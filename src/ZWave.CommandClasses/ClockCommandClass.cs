namespace ZWave.CommandClasses;

public enum ClockCommand : byte
{
    /// <summary>
    /// Set the current time at the receiving node.
    /// </summary>
    Set = 0x04,

    /// <summary>
    /// Request the current time from a node.
    /// </summary>
    Get = 0x05,

    /// <summary>
    /// Advertise the current time at the sending node.
    /// </summary>
    Report = 0x06,
}

/// <summary>
/// Represents the day of the week for the Clock Command Class.
/// </summary>
public enum ClockDayOfWeek : byte
{
    Unknown = 0x00,
    Monday = 0x01,
    Tuesday = 0x02,
    Wednesday = 0x03,
    Thursday = 0x04,
    Friday = 0x05,
    Saturday = 0x06,
    Sunday = 0x07,
}

/// <summary>
/// Represents the state reported by a Clock Command Class device.
/// </summary>
public readonly struct ClockState
{
    public ClockState(ClockDayOfWeek dayOfWeek, byte hour, byte minute)
    {
        DayOfWeek = dayOfWeek;
        Hour = hour;
        Minute = minute;
    }

    /// <summary>
    /// The day of the week.
    /// </summary>
    public ClockDayOfWeek DayOfWeek { get; }

    /// <summary>
    /// The hour of the day (0-23).
    /// </summary>
    public byte Hour { get; }

    /// <summary>
    /// The minute of the hour (0-59).
    /// </summary>
    public byte Minute { get; }
}

[CommandClass(CommandClassId.Clock)]
public sealed class ClockCommandClass : CommandClass<ClockCommand>
{
    internal ClockCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported clock state.
    /// </summary>
    public ClockState? State { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ClockCommand command)
        => command switch
        {
            ClockCommand.Set => true,
            ClockCommand.Get => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current time from a node.
    /// </summary>
    public async Task<ClockState> GetAsync(CancellationToken cancellationToken)
    {
        var command = ClockGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ClockReportCommand>(cancellationToken).ConfigureAwait(false);
        return State!.Value;
    }

    /// <summary>
    /// Set the current time at the receiving node.
    /// </summary>
    public async Task SetAsync(
        ClockDayOfWeek dayOfWeek,
        byte hour,
        byte minute,
        CancellationToken cancellationToken)
    {
        var command = ClockSetCommand.Create(dayOfWeek, hour, minute);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ClockCommand)frame.CommandId)
        {
            case ClockCommand.Set:
            case ClockCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ClockCommand.Report:
            {
                var command = new ClockReportCommand(frame);
                State = new ClockState(
                    command.DayOfWeek,
                    command.Hour,
                    command.Minute);
                break;
            }
        }
    }

    private readonly struct ClockSetCommand : ICommand
    {
        public ClockSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Clock;

        public static byte CommandId => (byte)ClockCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ClockSetCommand Create(ClockDayOfWeek dayOfWeek, byte hour, byte minute)
        {
            Span<byte> commandParameters = stackalloc byte[2];
            commandParameters[0] = (byte)(((byte)dayOfWeek << 5) | (hour & 0x1F));
            commandParameters[1] = minute;
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ClockSetCommand(frame);
        }
    }

    private readonly struct ClockGetCommand : ICommand
    {
        public ClockGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Clock;

        public static byte CommandId => (byte)ClockCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ClockGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ClockGetCommand(frame);
        }
    }

    private readonly struct ClockReportCommand : ICommand
    {
        public ClockReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Clock;

        public static byte CommandId => (byte)ClockCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The day of the week.
        /// </summary>
        public ClockDayOfWeek DayOfWeek => (ClockDayOfWeek)((Frame.CommandParameters.Span[0] & 0xE0) >> 5);

        /// <summary>
        /// The hour of the day (0-23).
        /// </summary>
        public byte Hour => (byte)(Frame.CommandParameters.Span[0] & 0x1F);

        /// <summary>
        /// The minute of the hour (0-59).
        /// </summary>
        public byte Minute => Frame.CommandParameters.Span[1];
    }
}
