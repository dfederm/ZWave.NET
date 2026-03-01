using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the day of the week for the Clock Command Class.
/// </summary>
public enum ClockWeekday : byte
{
    /// <summary>
    /// The weekday is not used or not known.
    /// </summary>
    Unknown = 0x00,

    /// <summary>
    /// Monday
    /// </summary>
    Monday = 0x01,

    /// <summary>
    /// Tuesday
    /// </summary>
    Tuesday = 0x02,

    /// <summary>
    /// Wednesday
    /// </summary>
    Wednesday = 0x03,

    /// <summary>
    /// Thursday
    /// </summary>
    Thursday = 0x04,

    /// <summary>
    /// Friday
    /// </summary>
    Friday = 0x05,

    /// <summary>
    /// Saturday
    /// </summary>
    Saturday = 0x06,

    /// <summary>
    /// Sunday
    /// </summary>
    Sunday = 0x07,
}

/// <summary>
/// Represents the commands in the Clock Command Class.
/// </summary>
public enum ClockCommand : byte
{
    /// <summary>
    /// Set the current time in a supporting node.
    /// </summary>
    Set = 0x04,

    /// <summary>
    /// Request the current time set at a supporting node.
    /// </summary>
    Get = 0x05,

    /// <summary>
    /// Advertise the current time set at the sending node.
    /// </summary>
    Report = 0x06,
}

/// <summary>
/// Represents a Clock Report received from a device.
/// </summary>
public readonly record struct ClockReport(
    /// <summary>
    /// The current weekday.
    /// </summary>
    ClockWeekday Weekday,

    /// <summary>
    /// The current time (hour and minute; seconds are not supported by the Clock CC).
    /// </summary>
    TimeOnly Time);

/// <summary>
/// The Clock Command Class is used to set and read the current time of a device.
/// </summary>
/// <remarks>
/// This command class is deprecated. It is recommended to use the Time Command Class, version 3 or higher instead.
/// </remarks>
[CommandClass(CommandClassId.Clock)]
public sealed class ClockCommandClass : CommandClass<ClockCommand>
{
    internal ClockCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public ClockReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Clock Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ClockReport>? OnClockReportReceived;

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
    /// Request the current time set at a supporting device.
    /// </summary>
    public async Task<ClockReport> GetAsync(CancellationToken cancellationToken)
    {
        ClockGetCommand command = ClockGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ClockReportCommand>(cancellationToken).ConfigureAwait(false);
        ClockReport report = ClockReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnClockReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the current time in a supporting device.
    /// </summary>
    public async Task SetAsync(ClockWeekday weekday, TimeOnly time, CancellationToken cancellationToken)
    {
        var command = ClockSetCommand.Create(weekday, time);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ClockCommand)frame.CommandId)
        {
            case ClockCommand.Report:
            {
                ClockReport report = ClockReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnClockReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct ClockSetCommand : ICommand
    {
        public ClockSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Clock;

        public static byte CommandId => (byte)ClockCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ClockSetCommand Create(ClockWeekday weekday, TimeOnly time)
        {
            ReadOnlySpan<byte> commandParameters =
            [
                (byte)(((byte)weekday << 5) | (time.Hour & 0b0001_1111)),
                (byte)time.Minute,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ClockSetCommand(frame);
        }
    }

    internal readonly struct ClockGetCommand : ICommand
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

    internal readonly struct ClockReportCommand : ICommand
    {
        public ClockReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Clock;

        public static byte CommandId => (byte)ClockCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ClockReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Clock Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Clock Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            ClockWeekday weekday = (ClockWeekday)(span[0] >> 5);
            int hour = span[0] & 0b0001_1111;
            int minute = span[1];

            try
            {
                return new ClockReport(weekday, new TimeOnly(hour, minute));
            }
            catch (ArgumentOutOfRangeException)
            {
                logger.LogWarning("Clock Report has invalid time values (hour={Hour}, minute={Minute})", hour, minute);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Clock Report has invalid time values");
            }
        }
    }
}
