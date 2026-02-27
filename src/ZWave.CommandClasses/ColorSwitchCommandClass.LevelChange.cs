namespace ZWave.CommandClasses;

public sealed partial class ColorSwitchCommandClass
{
    /// <summary>
    /// Initiate a transition of one color component to a new level.
    /// </summary>
    public async Task StartLevelChangeAsync(
        ColorSwitchChangeDirection direction,
        ColorSwitchColorComponent colorComponent,
        byte? startLevel,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = ColorSwitchStartLevelChangeCommand.Create(
            EffectiveVersion,
            direction,
            colorComponent,
            startLevel,
            duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop an ongoing color component level change.
    /// </summary>
    public async Task StopLevelChangeAsync(ColorSwitchColorComponent colorComponent, CancellationToken cancellationToken)
    {
        var command = ColorSwitchStopLevelChangeCommand.Create(colorComponent);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ColorSwitchStartLevelChangeCommand : ICommand
    {
        public ColorSwitchStartLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.StartLevelChange;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchStartLevelChangeCommand Create(
            byte version,
            ColorSwitchChangeDirection direction,
            ColorSwitchColorComponent colorComponent,
            byte? startLevel,
            DurationSet? duration)
        {
            bool includeDuration = version >= 3 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[3 + (includeDuration ? 1 : 0)];

            commandParameters[0] = (byte)((byte)direction << 6);
            if (!startLevel.HasValue)
            {
                // Ignore Start Level bit (bit 5)
                commandParameters[0] |= 0b0010_0000;
            }

            commandParameters[1] = (byte)colorComponent;
            commandParameters[2] = startLevel.GetValueOrDefault();

            if (includeDuration)
            {
                commandParameters[3] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchStartLevelChangeCommand(frame);
        }
    }

    internal readonly struct ColorSwitchStopLevelChangeCommand : ICommand
    {
        public ColorSwitchStopLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.StopLevelChange;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchStopLevelChangeCommand Create(ColorSwitchColorComponent colorComponent)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)colorComponent];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchStopLevelChangeCommand(frame);
        }
    }
}
