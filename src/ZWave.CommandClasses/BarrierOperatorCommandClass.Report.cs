using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the target value for a Barrier Operator Set command.
/// </summary>
public enum BarrierOperatorTargetValue : byte
{
    /// <summary>
    /// Initiate unattended close.
    /// </summary>
    Close = 0x00,

    /// <summary>
    /// Initiate unattended open.
    /// </summary>
    Open = 0xFF,
}

/// <summary>
/// Represents a Barrier Operator Report received from a device.
/// </summary>
/// <param name="StateValue">The raw state byte value from the device.</param>
public readonly record struct BarrierOperatorReport(byte StateValue)
{
    /// <summary>
    /// Gets the interpreted barrier state, or <see langword="null"/> for reserved values.
    /// </summary>
    public BarrierOperatorState? State => StateValue switch
    {
        0x00 => BarrierOperatorState.Closed,
        >= 0x01 and <= 0x63 => BarrierOperatorState.Stopped,
        0xFC => BarrierOperatorState.Closing,
        0xFD => BarrierOperatorState.Stopped,
        0xFE => BarrierOperatorState.Opening,
        0xFF => BarrierOperatorState.Open,
        _ => null,
    };

    /// <summary>
    /// Gets the exact position percentage (0-100) when the barrier is at a known position,
    /// or <see langword="null"/> otherwise.
    /// </summary>
    public byte? Position => StateValue switch
    {
        <= 0x63 => StateValue,
        0xFF => 100,
        _ => null,
    };
}

public sealed partial class BarrierOperatorCommandClass
{
    /// <summary>
    /// Occurs when a Barrier Operator Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<BarrierOperatorReport>? OnBarrierOperatorReportReceived;

    /// <summary>
    /// Gets the last Barrier Operator Report received from the device.
    /// </summary>
    public BarrierOperatorReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the current state of the barrier operator device.
    /// </summary>
    public async Task<BarrierOperatorReport> GetAsync(CancellationToken cancellationToken)
    {
        BarrierOperatorGetCommand command = BarrierOperatorGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BarrierOperatorReportCommand>(cancellationToken).ConfigureAwait(false);
        BarrierOperatorReport report = BarrierOperatorReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnBarrierOperatorReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Initiates an unattended change in state of the barrier.
    /// </summary>
    /// <param name="targetValue">The intended state of the barrier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SetAsync(BarrierOperatorTargetValue targetValue, CancellationToken cancellationToken)
    {
        BarrierOperatorSetCommand command = BarrierOperatorSetCommand.Create(targetValue);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct BarrierOperatorSetCommand : ICommand
    {
        public BarrierOperatorSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.Set;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorSetCommand Create(BarrierOperatorTargetValue targetValue)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)targetValue];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BarrierOperatorSetCommand(frame);
        }
    }

    internal readonly struct BarrierOperatorGetCommand : ICommand
    {
        public BarrierOperatorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BarrierOperatorGetCommand(frame);
        }
    }

    internal readonly struct BarrierOperatorReportCommand : ICommand
    {
        public BarrierOperatorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.Report;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Barrier Operator Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Barrier Operator Report frame is too short");
            }

            byte stateValue = frame.CommandParameters.Span[0];
            return new BarrierOperatorReport(stateValue);
        }
    }
}
