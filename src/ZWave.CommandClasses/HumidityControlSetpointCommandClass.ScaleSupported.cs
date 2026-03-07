using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class HumidityControlSetpointCommandClass
{
    private Dictionary<HumidityControlSetpointType, IReadOnlySet<HumidityControlSetpointScale>?>? _supportedScales;

    /// <summary>
    /// Gets the supported scales per setpoint type, or <see langword="null"/> if not yet known.
    /// </summary>
    public IReadOnlyDictionary<HumidityControlSetpointType, IReadOnlySet<HumidityControlSetpointScale>?>? SupportedScales => _supportedScales;

    /// <summary>
    /// Request the supported scales for a given setpoint type.
    /// </summary>
    public async Task<IReadOnlySet<HumidityControlSetpointScale>> GetScaleSupportedAsync(
        HumidityControlSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointScaleSupportedGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<HumidityControlSetpointScaleSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<HumidityControlSetpointScale> supportedScales = HumidityControlSetpointScaleSupportedReportCommand.Parse(reportFrame, Logger);

        _supportedScales ??= new Dictionary<HumidityControlSetpointType, IReadOnlySet<HumidityControlSetpointScale>?>();
        _supportedScales[setpointType] = supportedScales;

        return supportedScales;
    }

    internal readonly struct HumidityControlSetpointScaleSupportedGetCommand : ICommand
    {
        public HumidityControlSetpointScaleSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.ScaleSupportedGet;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointScaleSupportedGetCommand Create(HumidityControlSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointScaleSupportedGetCommand(frame);
        }
    }

    internal readonly struct HumidityControlSetpointScaleSupportedReportCommand : ICommand
    {
        public HumidityControlSetpointScaleSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.ScaleSupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<HumidityControlSetpointScale> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Humidity Control Setpoint Scale Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Humidity Control Setpoint Scale Supported Report frame is too short");
            }

            HashSet<HumidityControlSetpointScale> supportedScales = [];

            // The Scale Bit Mask is in the lower 4 bits of the first byte
            byte scaleBitMask = (byte)(frame.CommandParameters.Span[0] & 0x0F);
            for (int bitNum = 0; bitNum < 4; bitNum++)
            {
                if ((scaleBitMask & (1 << bitNum)) != 0)
                {
                    supportedScales.Add((HumidityControlSetpointScale)bitNum);
                }
            }

            return supportedScales;
        }
    }
}
