using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the current Wake Up interval configuration.
/// </summary>
public readonly record struct WakeUpInterval(
    /// <summary>
    /// The time in seconds between Wake Up periods at the sending node.
    /// </summary>
    uint WakeUpIntervalInSeconds,

    /// <summary>
    /// The Wake Up destination NodeID configured at the sending node.
    /// </summary>
    byte WakeUpDestinationNodeId);

public sealed partial class WakeUpCommandClass
{
    /// <summary>
    /// Raised when a Wake Up Interval Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<WakeUpInterval>? OnIntervalReportReceived;

    /// <summary>
    /// Gets the current wake up interval configuration.
    /// </summary>
    public WakeUpInterval? LastInterval { get; private set; }

    /// <summary>
    /// Request the Wake Up Interval and destination of a node.
    /// </summary>
    public async Task<WakeUpInterval> GetIntervalAsync(CancellationToken cancellationToken)
    {
        WakeUpIntervalGetCommand command = WakeUpIntervalGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<WakeUpIntervalReportCommand>(cancellationToken).ConfigureAwait(false);
        WakeUpInterval interval = WakeUpIntervalReportCommand.Parse(reportFrame, Logger);
        LastInterval = interval;
        OnIntervalReportReceived?.Invoke(interval);
        return interval;
    }

    /// <summary>
    /// Configure the Wake Up interval and destination of a node.
    /// </summary>
    public async Task SetIntervalAsync(uint wakeUpIntervalInSeconds, byte wakeUpDestinationNodeId, CancellationToken cancellationToken)
    {
        var command = WakeUpIntervalSetCommand.Create(wakeUpIntervalInSeconds, wakeUpDestinationNodeId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct WakeUpIntervalSetCommand : ICommand
    {
        public WakeUpIntervalSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalSet;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalSetCommand Create(uint wakeUpIntervalInSeconds, byte wakeUpDestinationNodeId)
        {
            const int int24MaxValue = (1 << 24) - 1;
            if (wakeUpIntervalInSeconds > int24MaxValue)
            {
                throw new ArgumentException($"Value must not be greater than {int24MaxValue}", nameof(wakeUpIntervalInSeconds));
            }

            Span<byte> commandParameters =
            [
                (byte)(wakeUpIntervalInSeconds >> 16),
                (byte)(wakeUpIntervalInSeconds >> 8),
                (byte)wakeUpIntervalInSeconds,
                wakeUpDestinationNodeId,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WakeUpIntervalSetCommand(frame);
        }
    }

    internal readonly struct WakeUpIntervalGetCommand : ICommand
    {
        public WakeUpIntervalGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalGet;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WakeUpIntervalGetCommand(frame);
        }
    }

    internal readonly struct WakeUpIntervalReportCommand : ICommand
    {
        public WakeUpIntervalReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalReport;

        public CommandClassFrame Frame { get; }

        public static WakeUpInterval Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Wake Up Interval Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Wake Up Interval Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            uint wakeUpIntervalInSeconds = (uint)(span[0] << 16) | (uint)(span[1] << 8) | span[2];
            byte wakeUpDestinationNodeId = span[3];
            return new WakeUpInterval(wakeUpIntervalInSeconds, wakeUpDestinationNodeId);
        }
    }
}
