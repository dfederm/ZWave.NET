using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Binary Sensor Report received from a device.
/// </summary>
public readonly record struct BinarySensorReport(
    /// <summary>
    /// Indicates whether the sensor has detected an event.
    /// </summary>
    bool Value,

    /// <summary>
    /// The type of sensor that generated this report.
    /// Null for version 1 devices that do not include a sensor type field.
    /// </summary>
    BinarySensorType? SensorType);

public sealed partial class BinarySensorCommandClass
{
    private Dictionary<BinarySensorType, bool?> _sensorValues = [];

    /// <summary>
    /// Event raised when a Binary Sensor Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<BinarySensorReport>? OnBinarySensorReportReceived;

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public BinarySensorReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the values of each supported sensor type.
    /// </summary>
    public IReadOnlyDictionary<BinarySensorType, bool?> SensorValues => _sensorValues;

    /// <summary>
    /// Request the status of a specific sensor type.
    /// </summary>
    public async Task<BinarySensorReport> GetAsync(
        BinarySensorType? sensorType,
        CancellationToken cancellationToken)
    {
        BinarySensorGetCommand command = BinarySensorGetCommand.Create(EffectiveVersion, sensorType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<BinarySensorReportCommand>(
            predicate: frame =>
            {
                // If no sensor type was specified (or FirstSupported was requested), accept the next report.
                // We can't match by sensor type since we don't know the device's default.
                if (!sensorType.HasValue || sensorType.Value == BinarySensorType.FirstSupported)
                {
                    return true;
                }

                return frame.CommandParameters.Length > 1
                    && (BinarySensorType)frame.CommandParameters.Span[1] == sensorType.Value;
            },
            cancellationToken).ConfigureAwait(false);
        BinarySensorReport report = BinarySensorReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        BinarySensorType key = report.SensorType.GetValueOrDefault(BinarySensorType.FirstSupported);
        _sensorValues[key] = report.Value;
        OnBinarySensorReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct BinarySensorGetCommand : ICommand
    {
        public BinarySensorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BinarySensorGetCommand Create(byte version, BinarySensorType? sensorType)
        {
            if (version >= 2 && sensorType.HasValue)
            {
                ReadOnlySpan<byte> commandParameters = [(byte)sensorType.Value];
                CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
                return new BinarySensorGetCommand(frame);
            }
            else
            {
                CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
                return new BinarySensorGetCommand(frame);
            }
        }
    }

    internal readonly struct BinarySensorReportCommand : ICommand
    {
        public BinarySensorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.Report;

        public CommandClassFrame Frame { get; }

        public static BinarySensorReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Binary Sensor Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Binary Sensor Report frame is too short");
            }

            bool sensorValue = frame.CommandParameters.Span[0] == 0xff;
            BinarySensorType? sensorType = frame.CommandParameters.Length > 1
                ? (BinarySensorType)frame.CommandParameters.Span[1]
                : null;
            return new BinarySensorReport(sensorValue, sensorType);
        }
    }
}
