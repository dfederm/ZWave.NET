namespace ZWave.CommandClasses;

public sealed partial class WindowCoveringCommandClass
{
    /// <summary>
    /// Initiate a transition of one parameter to a new level.
    /// </summary>
    public async Task StartLevelChangeAsync(
        WindowCoveringChangeDirection direction,
        WindowCoveringParameterId parameterId,
        DurationSet duration,
        CancellationToken cancellationToken)
    {
        var command = WindowCoveringStartLevelChangeCommand.Create(direction, parameterId, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop an ongoing transition.
    /// </summary>
    public async Task StopLevelChangeAsync(WindowCoveringParameterId parameterId, CancellationToken cancellationToken)
    {
        var command = WindowCoveringStopLevelChangeCommand.Create(parameterId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct WindowCoveringStartLevelChangeCommand : ICommand
    {
        public WindowCoveringStartLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.StartLevelChange;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringStartLevelChangeCommand Create(
            WindowCoveringChangeDirection direction,
            WindowCoveringParameterId parameterId,
            DurationSet duration)
        {
            Span<byte> commandParameters =
            [
                // Byte 0: bit 7 = reserved, bit 6 = Up/Down, bits 5-0 = reserved
                (byte)((byte)direction << 6),
                (byte)parameterId,
                duration.Value,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringStartLevelChangeCommand(frame);
        }
    }

    internal readonly struct WindowCoveringStopLevelChangeCommand : ICommand
    {
        public WindowCoveringStopLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.StopLevelChange;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringStopLevelChangeCommand Create(WindowCoveringParameterId parameterId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)parameterId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringStopLevelChangeCommand(frame);
        }
    }
}
