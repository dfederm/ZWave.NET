using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Humidity Control Setpoint Report received from a device.
/// </summary>
public readonly record struct HumidityControlSetpointReport(
    /// <summary>
    /// The setpoint type.
    /// </summary>
    HumidityControlSetpointType SetpointType,

    /// <summary>
    /// The scale of the setpoint value.
    /// </summary>
    HumidityControlSetpointScale Scale,

    /// <summary>
    /// The setpoint value.
    /// </summary>
    double Value);

public sealed partial class HumidityControlSetpointCommandClass
{
    private Dictionary<HumidityControlSetpointType, HumidityControlSetpointReport?> _setpointValues = new();

    /// <summary>
    /// Gets the latest setpoint values per setpoint type.
    /// </summary>
    public IReadOnlyDictionary<HumidityControlSetpointType, HumidityControlSetpointReport?> SetpointValues => _setpointValues;

    /// <summary>
    /// Event raised when a Humidity Control Setpoint Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<HumidityControlSetpointReport>? OnSetpointReportReceived;

    /// <summary>
    /// Request the current setpoint value for a given setpoint type.
    /// </summary>
    public async Task<HumidityControlSetpointReport> GetAsync(
        HumidityControlSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<HumidityControlSetpointReportCommand>(
            predicate: frame =>
            {
                return frame.CommandParameters.Length > 0
                    && (HumidityControlSetpointType)(frame.CommandParameters.Span[0] & 0x0F) == setpointType;
            },
            cancellationToken).ConfigureAwait(false);
        HumidityControlSetpointReport report = HumidityControlSetpointReportCommand.Parse(reportFrame, Logger);
        _setpointValues[report.SetpointType] = report;
        OnSetpointReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the humidity control setpoint value.
    /// </summary>
    public async Task SetAsync(
        HumidityControlSetpointType setpointType,
        HumidityControlSetpointScale scale,
        double value,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointSetCommand.Create(setpointType, scale, value);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct HumidityControlSetpointSetCommand : ICommand
    {
        public HumidityControlSetpointSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.Set;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointSetCommand Create(
            HumidityControlSetpointType setpointType,
            HumidityControlSetpointScale scale,
            double value)
        {
            (int rawValue, int valueSize, int precision) = EncodeValue(value);

            Span<byte> commandParameters = stackalloc byte[2 + valueSize];
            commandParameters[0] = (byte)((byte)setpointType & 0x0F);
            commandParameters[1] = EncodePrecisionScaleSize(precision, scale, valueSize);
            rawValue.WriteSignedVariableSizeBE(commandParameters.Slice(2, valueSize));

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointSetCommand(frame);
        }
    }

    internal readonly struct HumidityControlSetpointGetCommand : ICommand
    {
        public HumidityControlSetpointGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.Get;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointGetCommand Create(HumidityControlSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointGetCommand(frame);
        }
    }

    internal readonly struct HumidityControlSetpointReportCommand : ICommand
    {
        public HumidityControlSetpointReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.Report;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Humidity Control Setpoint Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Humidity Control Setpoint Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            HumidityControlSetpointType setpointType = (HumidityControlSetpointType)(span[0] & 0x0F);
            (int precision, HumidityControlSetpointScale scale, int valueSize) = ParsePrecisionScaleSize(span[1]);

            if (frame.CommandParameters.Length < 2 + valueSize)
            {
                logger.LogWarning(
                    "Humidity Control Setpoint Report frame value size ({ValueSize}) exceeds remaining bytes ({Remaining})",
                    valueSize,
                    frame.CommandParameters.Length - 2);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Humidity Control Setpoint Report frame is too short for declared value size");
            }

            ReadOnlySpan<byte> valueBytes = span.Slice(2, valueSize);
            double value = ParseValue(valueBytes, precision);

            return new HumidityControlSetpointReport(setpointType, scale, value);
        }
    }
}
