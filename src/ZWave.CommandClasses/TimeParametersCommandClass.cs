using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the commands in the Time Parameters Command Class.
/// </summary>
public enum TimeParametersCommand : byte
{
    /// <summary>
    /// Set the current date and time in UTC at a supporting node.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current date and time parameters from a supporting node.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current date and time in UTC.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// The Time Parameters Command Class is used to set date and time in a device.
/// </summary>
/// <remarks>
/// It is recommended to use Time Command Class, version 3 or higher instead.
/// The data formats are based on the International Standard ISO 8601.
/// Values are in Universal Time (UTC).
/// </remarks>
[CommandClass(CommandClassId.TimeParameters)]
public sealed class TimeParametersCommandClass : CommandClass<TimeParametersCommand>
{
    internal TimeParametersCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last date/time report received from the device.
    /// </summary>
    public DateTime? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Time Parameters Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<DateTime>? OnTimeParametersReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(TimeParametersCommand command)
        => command switch
        {
            TimeParametersCommand.Set => true,
            TimeParametersCommand.Get => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current date and time in UTC from a supporting device.
    /// </summary>
    public async Task<DateTime> GetAsync(CancellationToken cancellationToken)
    {
        TimeParametersGetCommand command = TimeParametersGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<TimeParametersReportCommand>(cancellationToken).ConfigureAwait(false);
        DateTime report = TimeParametersReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnTimeParametersReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the current date and time in UTC at a supporting device.
    /// </summary>
    public async Task SetAsync(DateTime dateTimeUtc, CancellationToken cancellationToken)
    {
        var command = TimeParametersSetCommand.Create(dateTimeUtc);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((TimeParametersCommand)frame.CommandId)
        {
            case TimeParametersCommand.Report:
            {
                DateTime report = TimeParametersReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnTimeParametersReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct TimeParametersSetCommand : ICommand
    {
        public TimeParametersSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.TimeParameters;

        public static byte CommandId => (byte)TimeParametersCommand.Set;

        public CommandClassFrame Frame { get; }

        public static TimeParametersSetCommand Create(DateTime dateTimeUtc)
        {
            if (dateTimeUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be in UTC.", nameof(dateTimeUtc));
            }

            Span<byte> commandParameters = stackalloc byte[7];
            ((ushort)dateTimeUtc.Year).WriteBytesBE(commandParameters[0..2]);
            commandParameters[2] = (byte)dateTimeUtc.Month;
            commandParameters[3] = (byte)dateTimeUtc.Day;
            commandParameters[4] = (byte)dateTimeUtc.Hour;
            commandParameters[5] = (byte)dateTimeUtc.Minute;
            commandParameters[6] = (byte)dateTimeUtc.Second;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new TimeParametersSetCommand(frame);
        }
    }

    internal readonly struct TimeParametersGetCommand : ICommand
    {
        public TimeParametersGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.TimeParameters;

        public static byte CommandId => (byte)TimeParametersCommand.Get;

        public CommandClassFrame Frame { get; }

        public static TimeParametersGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new TimeParametersGetCommand(frame);
        }
    }

    internal readonly struct TimeParametersReportCommand : ICommand
    {
        public TimeParametersReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.TimeParameters;

        public static byte CommandId => (byte)TimeParametersCommand.Report;

        public CommandClassFrame Frame { get; }

        public static DateTime Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 7)
            {
                logger.LogWarning("Time Parameters Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Time Parameters Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            int year = span[0..2].ToUInt16BE();
            int month = span[2];
            int day = span[3];
            int hour = span[4];
            int minute = span[5];
            int second = span[6];

            try
            {
                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                logger.LogWarning(
                    "Time Parameters Report has invalid date/time values (year={Year}, month={Month}, day={Day}, hour={Hour}, minute={Minute}, second={Second})",
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Time Parameters Report has invalid date/time values");
            }
        }
    }
}
