using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the capabilities (min/max values) for a humidity control setpoint type.
/// </summary>
public readonly record struct HumidityControlSetpointCapabilities(
    /// <summary>
    /// The setpoint type.
    /// </summary>
    HumidityControlSetpointType SetpointType,

    /// <summary>
    /// The scale of the minimum value.
    /// </summary>
    HumidityControlSetpointScale MinimumScale,

    /// <summary>
    /// The minimum setpoint value.
    /// </summary>
    double MinimumValue,

    /// <summary>
    /// The scale of the maximum value.
    /// </summary>
    HumidityControlSetpointScale MaximumScale,

    /// <summary>
    /// The maximum setpoint value.
    /// </summary>
    double MaximumValue);

public sealed partial class HumidityControlSetpointCommandClass
{
    private Dictionary<HumidityControlSetpointType, HumidityControlSetpointCapabilities?>? _capabilities;

    /// <summary>
    /// Gets the capabilities (min/max) per setpoint type, or <see langword="null"/> if not yet known.
    /// </summary>
    public IReadOnlyDictionary<HumidityControlSetpointType, HumidityControlSetpointCapabilities?>? Capabilities => _capabilities;

    /// <summary>
    /// Request the minimum and maximum setpoint values for a given setpoint type.
    /// </summary>
    public async Task<HumidityControlSetpointCapabilities> GetCapabilitiesAsync(
        HumidityControlSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointCapabilitiesGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<HumidityControlSetpointCapabilitiesReportCommand>(
            predicate: frame =>
            {
                return frame.CommandParameters.Length > 0
                    && (HumidityControlSetpointType)(frame.CommandParameters.Span[0] & 0x0F) == setpointType;
            },
            cancellationToken).ConfigureAwait(false);
        HumidityControlSetpointCapabilities capabilities = HumidityControlSetpointCapabilitiesReportCommand.Parse(reportFrame, Logger);

        _capabilities ??= [];
        _capabilities[setpointType] = capabilities;

        return capabilities;
    }

    internal readonly struct HumidityControlSetpointCapabilitiesGetCommand : ICommand
    {
        public HumidityControlSetpointCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.CapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointCapabilitiesGetCommand Create(HumidityControlSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointCapabilitiesGetCommand(frame);
        }
    }

    internal readonly struct HumidityControlSetpointCapabilitiesReportCommand : ICommand
    {
        public HumidityControlSetpointCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.CapabilitiesReport;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointCapabilities Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum payload: type(1) + min PSS(1) + at least 1 min value byte + max PSS(1) + at least 1 max value byte = 5
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Humidity Control Setpoint Capabilities Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Humidity Control Setpoint Capabilities Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            HumidityControlSetpointType setpointType = (HumidityControlSetpointType)(span[0] & 0x0F);

            // Parse minimum value
            (int minPrecision, HumidityControlSetpointScale minScale, int minValueSize) = ParsePrecisionScaleSize(span[1]);

            if (frame.CommandParameters.Length < 2 + minValueSize + 1)
            {
                logger.LogWarning(
                    "Humidity Control Setpoint Capabilities Report frame is too short for minimum value ({Length} bytes, need {Needed})",
                    frame.CommandParameters.Length,
                    2 + minValueSize + 1);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Humidity Control Setpoint Capabilities Report frame is too short for minimum value");
            }

            ReadOnlySpan<byte> minValueBytes = span.Slice(2, minValueSize);
            double minValue = ParseValue(minValueBytes, minPrecision);

            // Parse maximum value
            int maxPssOffset = 2 + minValueSize;
            (int maxPrecision, HumidityControlSetpointScale maxScale, int maxValueSize) = ParsePrecisionScaleSize(span[maxPssOffset]);

            if (frame.CommandParameters.Length < maxPssOffset + 1 + maxValueSize)
            {
                logger.LogWarning(
                    "Humidity Control Setpoint Capabilities Report frame is too short for maximum value ({Length} bytes, need {Needed})",
                    frame.CommandParameters.Length,
                    maxPssOffset + 1 + maxValueSize);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Humidity Control Setpoint Capabilities Report frame is too short for maximum value");
            }

            ReadOnlySpan<byte> maxValueBytes = span.Slice(maxPssOffset + 1, maxValueSize);
            double maxValue = ParseValue(maxValueBytes, maxPrecision);

            return new HumidityControlSetpointCapabilities(setpointType, minScale, minValue, maxScale, maxValue);
        }
    }
}
