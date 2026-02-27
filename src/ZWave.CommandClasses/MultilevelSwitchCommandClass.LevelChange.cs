namespace ZWave.CommandClasses;

public sealed partial class MultilevelSwitchCommandClass
{
    /// <summary>
    /// Initiate a transition to a new level.
    /// </summary>
    public async Task StartLevelChangeAsync(
        MultilevelSwitchChangeDirection direction,
        GenericValue? startLevel,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchStartLevelChangeCommand.Create(
            EffectiveVersion,
            direction,
            startLevel,
            duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop an ongoing transition.
    /// </summary>
    public async Task StopLevelChangeAsync(CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchStopLevelChangeCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct MultilevelSwitchStartLevelChangeCommand : ICommand
    {
        public MultilevelSwitchStartLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.StartLevelChange;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchStartLevelChangeCommand Create(
            byte version,
            MultilevelSwitchChangeDirection direction,
            GenericValue? startLevel,
            DurationSet? duration)
        {
            bool includeDuration = version >= 2 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[2 + (includeDuration ? 1 : 0)];

            commandParameters[0] = (byte)((byte)direction << 6);
            if (!startLevel.HasValue)
            {
                // Ignore Start Level bit (bit 5)
                commandParameters[0] |= 0b0010_0000;
            }

            commandParameters[1] = startLevel.GetValueOrDefault().Value;

            if (includeDuration)
            {
                commandParameters[2] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultilevelSwitchStartLevelChangeCommand(frame);
        }
    }

    internal readonly struct MultilevelSwitchStopLevelChangeCommand : ICommand
    {
        public MultilevelSwitchStopLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.StopLevelChange;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchStopLevelChangeCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultilevelSwitchStopLevelChangeCommand(frame);
        }
    }
}
