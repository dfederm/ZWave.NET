using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Color Switch Report received from a device.
/// </summary>
public readonly record struct ColorSwitchReport(
    /// <summary>
    /// The color component covered by this report.
    /// </summary>
    ColorSwitchColorComponent ColorComponent,

    /// <summary>
    /// The current value of the color component identified by the Color Component ID.
    /// </summary>
    byte CurrentValue,

    /// <summary>
    /// The target value of an ongoing transition or the most recent transition for the advertised Color Component ID.
    /// </summary>
    byte? TargetValue,

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate.
    /// </summary>
    DurationReport? Duration);

public sealed partial class ColorSwitchCommandClass
{
    private Dictionary<ColorSwitchColorComponent, ColorSwitchReport?> _colorComponents = new();

    /// <summary>
    /// Event raised when a Color Switch Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ColorSwitchReport>? OnColorSwitchReportReceived;

    /// <summary>
    /// Gets the state of each supported color component.
    /// </summary>
    public IReadOnlyDictionary<ColorSwitchColorComponent, ColorSwitchReport?> ColorComponents => _colorComponents;

    /// <summary>
    /// Request the status of a specified color component.
    /// </summary>
    public async Task<ColorSwitchReport> GetAsync(
        ColorSwitchColorComponent colorComponent,
        CancellationToken cancellationToken)
    {
        ColorSwitchGetCommand command = ColorSwitchGetCommand.Create(colorComponent);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ColorSwitchReportCommand>(
            predicate: frame => frame.CommandParameters.Length > 0
                && (ColorSwitchColorComponent)frame.CommandParameters.Span[0] == colorComponent,
            cancellationToken).ConfigureAwait(false);
        ColorSwitchReport report = ColorSwitchReportCommand.Parse(reportFrame, Logger);

        _colorComponents[report.ColorComponent] = report;

        OnColorSwitchReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the value of one or more color components.
    /// </summary>
    public async Task SetAsync(
        IReadOnlyDictionary<ColorSwitchColorComponent, byte> values,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = ColorSwitchSetCommand.Create(EffectiveVersion, values, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ColorSwitchGetCommand : ICommand
    {
        public ColorSwitchGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchGetCommand Create(ColorSwitchColorComponent colorComponent)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)colorComponent];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchGetCommand(frame);
        }
    }

    internal readonly struct ColorSwitchReportCommand : ICommand
    {
        public ColorSwitchReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Color Switch Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Color Switch Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            ColorSwitchColorComponent colorComponent = (ColorSwitchColorComponent)span[0];
            byte currentValue = span[1];
            byte? targetValue = span.Length > 2
                ? span[2]
                : null;
            DurationReport? duration = span.Length > 3
                ? span[3]
                : null;

            return new ColorSwitchReport(colorComponent, currentValue, targetValue, duration);
        }
    }

    internal readonly struct ColorSwitchSetCommand : ICommand
    {
        public ColorSwitchSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchSetCommand Create(
            byte version,
            IReadOnlyDictionary<ColorSwitchColorComponent, byte> values,
            DurationSet? duration)
        {
            bool includeDuration = version >= 2 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[1 + (2 * values.Count) + (includeDuration ? 1 : 0)];
            commandParameters[0] = (byte)(values.Count & 0b0001_1111);

            int idx = 1;
            foreach (KeyValuePair<ColorSwitchColorComponent, byte> pair in values)
            {
                commandParameters[idx++] = (byte)pair.Key;
                commandParameters[idx++] = pair.Value;
            }

            if (includeDuration)
            {
                commandParameters[idx] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchSetCommand(frame);
        }
    }
}
