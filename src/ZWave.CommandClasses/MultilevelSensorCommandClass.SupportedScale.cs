using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class MultilevelSensorCommandClass
{
    private Dictionary<MultilevelSensorType, IReadOnlySet<MultilevelSensorScale>?>? _supportedScales;

    /// <summary>
    /// Gets the supported scales per sensor type, or <see langword="null"/> if not yet known.
    /// </summary>
    public IReadOnlyDictionary<MultilevelSensorType, IReadOnlySet<MultilevelSensorScale>?>? SupportedScales => _supportedScales;

    /// <summary>
    /// Request the supported scales for a specific sensor type.
    /// </summary>
    /// <param name="sensorType">The sensor type to query scales for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The set of supported scales for the specified sensor type.</returns>
    public async Task<IReadOnlySet<MultilevelSensorScale>> GetSupportedScalesAsync(
        MultilevelSensorType sensorType,
        CancellationToken cancellationToken)
    {
        if (SupportedSensorTypes == null)
        {
            bool? isCommandSupported = IsCommandSupported(MultilevelSensorCommand.SupportedScaleGet);
            if (isCommandSupported == null)
            {
                ZWaveException.Throw(
                    ZWaveErrorCode.CommandNotReady,
                    "The supported sensor types are not yet known.");
            }
            else if (!isCommandSupported.Value)
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandNotSupported, "This command is not supported by this node");
            }
            else
            {
                throw new InvalidOperationException("Unexpected state. The interview is complete, the command is supported, but the supported sensor types are unknown.");
            }
        }

        if (!SupportedSensorTypes.Contains(sensorType))
        {
            ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, $"Sensor type '{sensorType}' is not supported.");
        }

        var command = MultilevelSensorSupportedScaleGetCommand.Create(sensorType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSensorSupportedScaleReportCommand>(
            predicate: frame =>
            {
                return frame.CommandParameters.Length > 0
                    && (MultilevelSensorType)frame.CommandParameters.Span[0] == sensorType;
            },
            cancellationToken).ConfigureAwait(false);
        (MultilevelSensorType reportedType, IReadOnlySet<MultilevelSensorScale> supportedScales) = MultilevelSensorSupportedScaleReportCommand.Parse(reportFrame, Logger);
        _supportedScales![reportedType] = supportedScales;
        return supportedScales;
    }

    internal readonly struct MultilevelSensorSupportedScaleGetCommand : ICommand
    {
        public MultilevelSensorSupportedScaleGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.SupportedScaleGet;

        public CommandClassFrame Frame { get; }

        public static MultilevelSensorSupportedScaleGetCommand Create(MultilevelSensorType sensorType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)sensorType];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultilevelSensorSupportedScaleGetCommand(frame);
        }
    }

    internal readonly struct MultilevelSensorSupportedScaleReportCommand : ICommand
    {
        public MultilevelSensorSupportedScaleReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.SupportedScaleReport;

        public CommandClassFrame Frame { get; }

        public static (MultilevelSensorType SensorType, IReadOnlySet<MultilevelSensorScale> SupportedScales) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Multilevel Sensor Supported Scale Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multilevel Sensor Supported Scale Report frame is too short");
            }

            MultilevelSensorType sensorType = (MultilevelSensorType)frame.CommandParameters.Span[0];

            HashSet<MultilevelSensorScale> supportedScales = new HashSet<MultilevelSensorScale>();
            byte bitMask = (byte)(frame.CommandParameters.Span[1] & 0b0000_1111);
            for (int bitNum = 0; bitNum < 4; bitNum++)
            {
                if ((bitMask & (1 << bitNum)) != 0)
                {
                    byte scaleId = (byte)bitNum;
                    MultilevelSensorScale scale = sensorType.GetScale(scaleId);
                    supportedScales.Add(scale);
                }
            }

            return (sensorType, supportedScales);
        }
    }
}
