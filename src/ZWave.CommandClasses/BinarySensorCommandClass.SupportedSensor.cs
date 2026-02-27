using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class BinarySensorCommandClass
{
    /// <summary>
    /// Gets the supported sensor types reported by the device.
    /// </summary>
    public IReadOnlySet<BinarySensorType>? SupportedSensorTypes { get; private set; }

    /// <summary>
    /// Request the supported sensor types from the binary sensor device.
    /// </summary>
    public async Task<IReadOnlySet<BinarySensorType>> GetSupportedSensorTypesAsync(CancellationToken cancellationToken)
    {
        BinarySensorSupportedGetCommand command = BinarySensorSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BinarySensorSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<BinarySensorType> supportedTypes = BinarySensorSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedSensorTypes = supportedTypes;

        Dictionary<BinarySensorType, bool?> newSensorValues = [];
        foreach (BinarySensorType type in supportedTypes)
        {
            // Persist any existing known state.
            if (!_sensorValues.TryGetValue(type, out bool? existing))
            {
                existing = null;
            }

            newSensorValues.Add(type, existing);
        }

        _sensorValues = newSensorValues;

        return supportedTypes;
    }

    internal readonly struct BinarySensorSupportedGetCommand : ICommand
    {
        public BinarySensorSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static BinarySensorSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BinarySensorSupportedGetCommand(frame);
        }
    }

    internal readonly struct BinarySensorSupportedReportCommand : ICommand
    {
        public BinarySensorSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<BinarySensorType> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Binary Sensor Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Binary Sensor Supported Report frame is too short");
            }

            HashSet<BinarySensorType> supportedSensorTypes = new HashSet<BinarySensorType>();

            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span;
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        BinarySensorType sensorType = (BinarySensorType)((byteNum << 3) + bitNum);
                        supportedSensorTypes.Add(sensorType);
                    }
                }
            }

            return supportedSensorTypes;
        }
    }
}
