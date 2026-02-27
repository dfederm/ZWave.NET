using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Multilevel Switch Report received from a device.
/// </summary>
public readonly record struct MultilevelSwitchReport(
    /// <summary>
    /// The current value at the sending node.
    /// </summary>
    GenericValue CurrentValue,

    /// <summary>
    /// The target value of an ongoing transition or the most recent transition (V4+).
    /// </summary>
    GenericValue? TargetValue,

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate (V4+).
    /// </summary>
    DurationReport? Duration);

public sealed partial class MultilevelSwitchCommandClass
{
    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public MultilevelSwitchReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Multilevel Switch Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<MultilevelSwitchReport>? OnMultilevelSwitchReportReceived;

    /// <summary>
    /// Set a multilevel value in a supporting device.
    /// </summary>
    public async Task SetAsync(
        GenericValue value,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchSetCommand.Create(EffectiveVersion, value, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the status of a multilevel device.
    /// </summary>
    public async Task<MultilevelSwitchReport> GetAsync(CancellationToken cancellationToken)
    {
        MultilevelSwitchGetCommand command = MultilevelSwitchGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSwitchReportCommand>(cancellationToken).ConfigureAwait(false);
        MultilevelSwitchReport report = MultilevelSwitchReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnMultilevelSwitchReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct MultilevelSwitchSetCommand : ICommand
    {
        public MultilevelSwitchSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.Set;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchSetCommand Create(byte version, GenericValue value, DurationSet? duration)
        {
            bool includeDuration = version >= 2 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[1 + (includeDuration ? 1 : 0)];
            commandParameters[0] = value.Value;
            if (includeDuration)
            {
                commandParameters[1] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultilevelSwitchSetCommand(frame);
        }
    }

    internal readonly struct MultilevelSwitchGetCommand : ICommand
    {
        public MultilevelSwitchGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultilevelSwitchGetCommand(frame);
        }
    }

    internal readonly struct MultilevelSwitchReportCommand : ICommand
    {
        public MultilevelSwitchReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.Report;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Multilevel Switch Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multilevel Switch Report frame is too short");
            }

            GenericValue currentValue = frame.CommandParameters.Span[0];
            GenericValue? targetValue = frame.CommandParameters.Length > 1
                ? frame.CommandParameters.Span[1]
                : null;
            DurationReport? duration = frame.CommandParameters.Length > 2
                ? frame.CommandParameters.Span[2]
                : null;
            return new MultilevelSwitchReport(currentValue, targetValue, duration);
        }
    }
}
