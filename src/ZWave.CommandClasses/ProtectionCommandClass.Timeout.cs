using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class ProtectionCommandClass
{
    /// <summary>
    /// Gets the last timeout received from the device, or <see langword="null"/> if not yet queried.
    /// </summary>
    /// <remarks>
    /// <see cref="TimeSpan.Zero"/> means no timer is active.
    /// <see cref="Timeout.InfiniteTimeSpan"/> means infinite RF protection.
    /// A <see langword="null"/> value from <see cref="GetTimeoutAsync"/> indicates a reserved encoding.
    /// </remarks>
    public TimeSpan? LastTimeout { get; private set; }

    /// <summary>
    /// Event raised when a Protection Timeout Report is received.
    /// </summary>
    public event Action<TimeSpan>? OnTimeoutReportReceived;

    /// <summary>
    /// Request the remaining RF protection timeout from a device.
    /// </summary>
    /// <returns>
    /// <see cref="TimeSpan.Zero"/> for no timer, <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> for infinite,
    /// a concrete duration for 1–60 seconds or 2–191 minutes, or <see langword="null"/> for reserved values.
    /// </returns>
    public async Task<TimeSpan?> GetTimeoutAsync(CancellationToken cancellationToken)
    {
        ProtectionTimeoutGetCommand command = ProtectionTimeoutGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ProtectionTimeoutReportCommand>(cancellationToken).ConfigureAwait(false);
        TimeSpan? timeout = ProtectionTimeoutReportCommand.Parse(reportFrame, Logger);
        if (timeout.HasValue)
        {
            LastTimeout = timeout;
            OnTimeoutReportReceived?.Invoke(timeout.Value);
        }

        return timeout;
    }

    /// <summary>
    /// Set the RF protection timeout for a device.
    /// </summary>
    /// <param name="timeout">
    /// The timeout value encoded per the Z-Wave specification:
    /// <c>0x00</c> = no timer, <c>0x01</c>–<c>0x3C</c> = 1–60 seconds,
    /// <c>0x41</c>–<c>0xFE</c> = 2–191 minutes, <c>0xFF</c> = infinite.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetTimeoutAsync(byte timeout, CancellationToken cancellationToken)
    {
        var command = ProtectionTimeoutSetCommand.Create(timeout);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ProtectionTimeoutSetCommand : ICommand
    {
        public ProtectionTimeoutSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.TimeoutSet;

        public CommandClassFrame Frame { get; }

        public static ProtectionTimeoutSetCommand Create(byte timeout)
        {
            ReadOnlySpan<byte> commandParameters = [timeout];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionTimeoutSetCommand(frame);
        }
    }

    internal readonly struct ProtectionTimeoutGetCommand : ICommand
    {
        public ProtectionTimeoutGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.TimeoutGet;

        public CommandClassFrame Frame { get; }

        public static ProtectionTimeoutGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ProtectionTimeoutGetCommand(frame);
        }
    }

    internal readonly struct ProtectionTimeoutReportCommand : ICommand
    {
        public ProtectionTimeoutReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.TimeoutReport;

        public CommandClassFrame Frame { get; }

        public static ProtectionTimeoutReportCommand Create(byte timeout)
        {
            ReadOnlySpan<byte> commandParameters = [timeout];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionTimeoutReportCommand(frame);
        }

        public static TimeSpan? Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Protection Timeout Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Protection Timeout Report frame is too short");
            }

            byte value = frame.CommandParameters.Span[0];
            return value switch
            {
                0x00 => TimeSpan.Zero,
                >= 0x01 and <= 0x3C => TimeSpan.FromSeconds(value),
                >= 0x41 and <= 0xFE => TimeSpan.FromMinutes(value - 0x3F),
                0xFF => Timeout.InfiniteTimeSpan,
                _ => null,
            };
        }
    }
}
