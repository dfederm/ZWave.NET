using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the commands in the Time Command Class.
/// </summary>
public enum TimeCommand : byte
{
    /// <summary>
    /// Request the current time from a supporting node.
    /// </summary>
    TimeGet = 0x01,

    /// <summary>
    /// Report the current time.
    /// </summary>
    TimeReport = 0x02,

    /// <summary>
    /// Request the current date from a supporting node.
    /// </summary>
    DateGet = 0x03,

    /// <summary>
    /// Report the current date.
    /// </summary>
    DateReport = 0x04,

    /// <summary>
    /// Set the Time Zone Offset and Daylight Savings Time parameters at a supporting node.
    /// </summary>
    TimeOffsetSet = 0x05,

    /// <summary>
    /// Request the Time Zone Offset and Daylight Savings Time parameters from a supporting node.
    /// </summary>
    TimeOffsetGet = 0x06,

    /// <summary>
    /// Report the Time Zone Offset and Daylight Savings Time parameters.
    /// </summary>
    TimeOffsetReport = 0x07,

    /// <summary>
    /// Set the current date at a supporting node.
    /// </summary>
    DateSet = 0x08,

    /// <summary>
    /// Set the current time at a supporting node.
    /// </summary>
    TimeSet = 0x09,
}

/// <summary>
/// The Time Command Class is used to set and read the current date and time from a device.
/// </summary>
[CommandClass(CommandClassId.Time)]
public sealed partial class TimeCommandClass : CommandClass<TimeCommand>
{
    internal TimeCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(TimeCommand command)
        => command switch
        {
            TimeCommand.TimeGet => true,
            TimeCommand.DateGet => true,
            TimeCommand.TimeOffsetGet => Version.HasValue ? Version >= 2 : null,
            TimeCommand.TimeOffsetSet => Version.HasValue ? Version >= 2 : null,
            TimeCommand.TimeSet => Version.HasValue ? Version >= 3 : null,
            TimeCommand.DateSet => Version.HasValue ? Version >= 3 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetTimeAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetDateAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(TimeCommand.TimeOffsetGet).GetValueOrDefault())
        {
            _ = await GetTimeOffsetAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((TimeCommand)frame.CommandId)
        {
            case TimeCommand.TimeReport:
            {
                TimeReport report = TimeReportCommand.Parse(frame, Logger);
                LastTimeReport = report;
                OnTimeReportReceived?.Invoke(report);
                break;
            }
            case TimeCommand.DateReport:
            {
                DateOnly date = DateReportCommand.Parse(frame, Logger);
                LastDateReport = date;
                OnDateReportReceived?.Invoke(date);
                break;
            }
            case TimeCommand.TimeOffsetReport:
            {
                TimeOffsetReport report = TimeOffsetReportCommand.Parse(frame, Logger);
                LastTimeOffsetReport = report;
                OnTimeOffsetReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
