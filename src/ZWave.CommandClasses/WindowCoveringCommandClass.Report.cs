using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Window Covering Report received from a device.
/// </summary>
public readonly record struct WindowCoveringReport(
    /// <summary>
    /// The parameter covered by this report.
    /// </summary>
    WindowCoveringParameterId ParameterId,

    /// <summary>
    /// The current value of the parameter.
    /// </summary>
    byte CurrentValue,

    /// <summary>
    /// The target value of an ongoing transition or the most recent transition for the parameter.
    /// </summary>
    byte TargetValue,

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate.
    /// </summary>
    DurationReport Duration);

public sealed partial class WindowCoveringCommandClass
{
    private Dictionary<WindowCoveringParameterId, WindowCoveringReport?> _parameterValues = new();

    /// <summary>
    /// Event raised when a Window Covering Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<WindowCoveringReport>? OnWindowCoveringReportReceived;

    /// <summary>
    /// Gets the state of each supported parameter.
    /// </summary>
    public IReadOnlyDictionary<WindowCoveringParameterId, WindowCoveringReport?> ParameterValues => _parameterValues;

    /// <summary>
    /// Request the status of a specified covering parameter.
    /// </summary>
    public async Task<WindowCoveringReport> GetAsync(
        WindowCoveringParameterId parameterId,
        CancellationToken cancellationToken)
    {
        WindowCoveringGetCommand command = WindowCoveringGetCommand.Create(parameterId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<WindowCoveringReportCommand>(
            predicate: frame => frame.CommandParameters.Length > 0
                && (WindowCoveringParameterId)frame.CommandParameters.Span[0] == parameterId,
            cancellationToken).ConfigureAwait(false);
        WindowCoveringReport report = WindowCoveringReportCommand.Parse(reportFrame, Logger);

        _parameterValues[report.ParameterId] = report;

        OnWindowCoveringReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the value of one or more covering parameters.
    /// </summary>
    public async Task SetAsync(
        IReadOnlyDictionary<WindowCoveringParameterId, byte> values,
        DurationSet duration,
        CancellationToken cancellationToken)
    {
        var command = WindowCoveringSetCommand.Create(values, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct WindowCoveringGetCommand : ICommand
    {
        public WindowCoveringGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.Get;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringGetCommand Create(WindowCoveringParameterId parameterId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)parameterId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringGetCommand(frame);
        }
    }

    internal readonly struct WindowCoveringReportCommand : ICommand
    {
        public WindowCoveringReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.Report;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Window Covering Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Window Covering Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            WindowCoveringParameterId parameterId = (WindowCoveringParameterId)span[0];
            byte currentValue = span[1];
            byte targetValue = span[2];
            DurationReport duration = span[3];

            return new WindowCoveringReport(parameterId, currentValue, targetValue, duration);
        }
    }

    internal readonly struct WindowCoveringSetCommand : ICommand
    {
        public WindowCoveringSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.Set;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringSetCommand Create(
            IReadOnlyDictionary<WindowCoveringParameterId, byte> values,
            DurationSet duration)
        {
            // 1 byte (reserved+count) + 2 bytes per parameter (ID + value) + 1 byte duration
            Span<byte> commandParameters = stackalloc byte[1 + (2 * values.Count) + 1];
            commandParameters[0] = (byte)(values.Count & 0b0001_1111);

            int idx = 1;
            foreach (KeyValuePair<WindowCoveringParameterId, byte> pair in values)
            {
                commandParameters[idx++] = (byte)pair.Key;
                commandParameters[idx++] = pair.Value;
            }

            commandParameters[idx] = duration.Value;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringSetCommand(frame);
        }
    }
}
