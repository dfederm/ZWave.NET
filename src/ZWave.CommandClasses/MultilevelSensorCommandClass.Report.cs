using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a multilevel sensor reading.
/// </summary>
public readonly record struct MultilevelSensorReport(
    /// <summary>
    /// The sensor type of the actual sensor reading.
    /// </summary>
    MultilevelSensorType SensorType,

    /// <summary>
    /// Indicates what scale is used for the actual sensor reading.
    /// </summary>
    MultilevelSensorScale Scale,

    /// <summary>
    /// The value of the actual sensor reading.
    /// </summary>
    double Value);

public sealed partial class MultilevelSensorCommandClass
{
    private Dictionary<MultilevelSensorType, MultilevelSensorReport?> _sensorValues = new();

    /// <summary>
    /// Gets the latest sensor readings per sensor type, or <see langword="null"/> if no readings have been received.
    /// </summary>
    /// <summary>
    /// Gets the latest sensor readings per sensor type.
    /// </summary>
    public IReadOnlyDictionary<MultilevelSensorType, MultilevelSensorReport?> SensorValues => _sensorValues;

    /// <summary>
    /// Occurs when a Multilevel Sensor Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<MultilevelSensorReport>? OnMultilevelSensorReportReceived;

    /// <summary>
    /// Request the current reading from a multilevel sensor.
    /// </summary>
    /// <param name="sensorType">
    /// The sensor type to request a reading for. If <see langword="null"/>, the device's default sensor type is used (V1-4 behavior).
    /// </param>
    /// <param name="scale">
    /// The scale to request. If <see langword="null"/>, an arbitrary supported scale is used.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sensor reading.</returns>
    public async Task<MultilevelSensorReport> GetAsync(
        MultilevelSensorType? sensorType,
        MultilevelSensorScale? scale,
        CancellationToken cancellationToken)
    {
        byte? scaleId = null;
        if (sensorType.HasValue)
        {
            if (SupportedSensorTypes == null)
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandNotReady, "The supported sensor types are not yet known.");
            }

            if (!SupportedSensorTypes.Contains(sensorType.Value))
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, $"Sensor type '{sensorType.Value}' is not supported for this node.");
            }

            if (SupportedScales == null)
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandNotReady, "The supported scales are not yet known.");
            }

            if (scale != null)
            {
                if (!SupportedScales[sensorType.Value]!.Contains(scale))
                {
                    ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, $"Scale '{scale.Label}' is not supported for this sensor.");
                }

                scaleId = scale.Id;
            }
            else
            {
                // Pick an arbitrary supported scale if one isn't provided
                scaleId = 0;
            }
        }

        var command = MultilevelSensorGetCommand.Create(EffectiveVersion, sensorType, scaleId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSensorReportCommand>(
            predicate: frame =>
            {
                // Ensure the sensor type matches. If one wasn't provided, we don't know the default
                // sensor type, so just return the next report.
                return !sensorType.HasValue
                    || (frame.CommandParameters.Length > 0
                        && (MultilevelSensorType)frame.CommandParameters.Span[0] == sensorType.Value);
            },
            cancellationToken).ConfigureAwait(false);
        MultilevelSensorReport report = MultilevelSensorReportCommand.Parse(reportFrame, Logger);
        _sensorValues[report.SensorType] = report;
        OnMultilevelSensorReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct MultilevelSensorGetCommand : ICommand
    {
        public MultilevelSensorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MultilevelSensorGetCommand Create(
            byte version,
            MultilevelSensorType? sensorType,
            byte? scaleId)
        {
            CommandClassFrame frame;
            if (version >= 5
                && sensorType.HasValue
                && scaleId.HasValue)
            {
                ReadOnlySpan<byte> commandParameters = [(byte)sensorType.Value, (byte)((scaleId & 0b0000_0011) << 3)];
                frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            }
            else
            {
                frame = CommandClassFrame.Create(CommandClassId, CommandId);
            }

            return new MultilevelSensorGetCommand(frame);
        }
    }

    internal readonly struct MultilevelSensorReportCommand : ICommand
    {
        public MultilevelSensorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.Report;

        public CommandClassFrame Frame { get; }

        public static MultilevelSensorReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Multilevel Sensor Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multilevel Sensor Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            MultilevelSensorType sensorType = (MultilevelSensorType)span[0];
            byte scaleId = (byte)((span[1] & 0b0001_1000) >> 3);
            MultilevelSensorScale scale = sensorType.GetScale(scaleId);

            int precision = (span[1] & 0b1110_0000) >> 5;
            int valueSize = span[1] & 0b0000_0111;

            if (frame.CommandParameters.Length < 2 + valueSize)
            {
                logger.LogWarning(
                    "Multilevel Sensor Report frame value size ({ValueSize}) exceeds remaining bytes ({Remaining})",
                    valueSize,
                    frame.CommandParameters.Length - 2);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multilevel Sensor Report frame is too short for declared value size");
            }

            ReadOnlySpan<byte> valueBytes = span.Slice(2, valueSize);

            int rawValue = valueBytes.ReadSignedVariableSizeBE();
            double value = rawValue / BinaryExtensions.PowersOfTen[precision];

            return new MultilevelSensorReport(sensorType, scale, value);
        }
    }
}
