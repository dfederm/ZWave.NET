using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the state of a signaling subsystem on a barrier operator device.
/// </summary>
/// <param name="SubsystemType">The type of signaling subsystem.</param>
/// <param name="SubsystemState">The raw state byte value (0x00 = Off, 0xFF = On).</param>
public readonly record struct BarrierOperatorEventSignalReport(
    BarrierOperatorSignalingSubsystemType SubsystemType,
    byte SubsystemState)
{
    /// <summary>
    /// Gets whether the subsystem is on, off, or <see langword="null"/> for reserved values.
    /// </summary>
    public bool? IsOn => SubsystemState switch
    {
        0x00 => false,
        0xFF => true,
        _ => null,
    };
}

public sealed partial class BarrierOperatorCommandClass
{
    private readonly Dictionary<BarrierOperatorSignalingSubsystemType, BarrierOperatorEventSignalReport> _eventSignals = [];

    /// <summary>
    /// Occurs when a Barrier Operator Event Signaling Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<BarrierOperatorEventSignalReport>? OnEventSignalReportReceived;

    /// <summary>
    /// Gets the last known state for each signaling subsystem type that has been reported.
    /// </summary>
    public IReadOnlyDictionary<BarrierOperatorSignalingSubsystemType, BarrierOperatorEventSignalReport> EventSignals => _eventSignals;

    /// <summary>
    /// Gets the state of a signaling subsystem on the device.
    /// </summary>
    /// <param name="subsystemType">The type of signaling subsystem to query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<BarrierOperatorEventSignalReport> GetEventSignalAsync(
        BarrierOperatorSignalingSubsystemType subsystemType,
        CancellationToken cancellationToken)
    {
        EventSignalingGetCommand command = EventSignalingGetCommand.Create(subsystemType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<EventSignalingReportCommand>(
            predicate: frame => frame.CommandParameters.Length > 0
                && (BarrierOperatorSignalingSubsystemType)frame.CommandParameters.Span[0] == subsystemType,
            cancellationToken).ConfigureAwait(false);
        BarrierOperatorEventSignalReport report = EventSignalingReportCommand.Parse(reportFrame, Logger);
        _eventSignals[report.SubsystemType] = report;
        OnEventSignalReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Turns on or off an event signaling subsystem on the device.
    /// </summary>
    /// <param name="subsystemType">The type of signaling subsystem to control.</param>
    /// <param name="on"><see langword="true"/> to turn on the subsystem; <see langword="false"/> to turn it off.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SetEventSignalAsync(
        BarrierOperatorSignalingSubsystemType subsystemType,
        bool on,
        CancellationToken cancellationToken)
    {
        EventSignalSetCommand command = EventSignalSetCommand.Create(subsystemType, on);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct EventSignalSetCommand : ICommand
    {
        public EventSignalSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.EventSignalSet;

        public CommandClassFrame Frame { get; }

        public static EventSignalSetCommand Create(
            BarrierOperatorSignalingSubsystemType subsystemType,
            bool on)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)subsystemType, on ? (byte)0xFF : (byte)0x00];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new EventSignalSetCommand(frame);
        }
    }

    internal readonly struct EventSignalingGetCommand : ICommand
    {
        public EventSignalingGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.EventSignalingGet;

        public CommandClassFrame Frame { get; }

        public static EventSignalingGetCommand Create(BarrierOperatorSignalingSubsystemType subsystemType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)subsystemType];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new EventSignalingGetCommand(frame);
        }
    }

    internal readonly struct EventSignalingReportCommand : ICommand
    {
        public EventSignalingReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.EventSignalingReport;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorEventSignalReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning(
                    "Barrier Operator Event Signaling Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Barrier Operator Event Signaling Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            BarrierOperatorSignalingSubsystemType subsystemType = (BarrierOperatorSignalingSubsystemType)span[0];
            byte subsystemState = span[1];

            return new BarrierOperatorEventSignalReport(subsystemType, subsystemState);
        }
    }
}
