using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a recurring annual DST transition point (month, day, and hour).
/// </summary>
public readonly record struct DstTransition(
    /// <summary>
    /// The month of the transition (1-12), or 0 if DST is not used.
    /// </summary>
    byte Month,

    /// <summary>
    /// The day of the month of the transition (1-31), or 0 if DST is not used.
    /// </summary>
    byte Day,

    /// <summary>
    /// The hour of the transition (0-23), or 0 if DST is not used.
    /// </summary>
    byte Hour);

/// <summary>
/// Represents a Time Offset Report received from a device, containing Time Zone Offset and Daylight Savings Time parameters.
/// </summary>
public readonly record struct TimeOffsetReport(
    /// <summary>
    /// The Time Zone Offset from UTC (positive = east of UTC, negative = west of UTC).
    /// </summary>
    TimeSpan TimeZoneOffset,

    /// <summary>
    /// The DST offset to apply to the current time during Daylight Saving Time
    /// (positive = add time, negative = subtract time).
    /// </summary>
    TimeSpan DstOffset,

    /// <summary>
    /// When Daylight Saving Time starts each year.
    /// </summary>
    DstTransition DstStart,

    /// <summary>
    /// When Daylight Saving Time ends each year.
    /// </summary>
    DstTransition DstEnd);

public sealed partial class TimeCommandClass
{
    /// <summary>
    /// Event raised when a Time Offset Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<TimeOffsetReport>? OnTimeOffsetReportReceived;

    /// <summary>
    /// Gets the last time offset report received from the device.
    /// </summary>
    public TimeOffsetReport? LastTimeOffsetReport { get; private set; }

    /// <summary>
    /// Request the Time Zone Offset and Daylight Savings Time parameters from a supporting device.
    /// </summary>
    public async Task<TimeOffsetReport> GetTimeOffsetAsync(CancellationToken cancellationToken)
    {
        TimeOffsetGetCommand command = TimeOffsetGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<TimeOffsetReportCommand>(cancellationToken).ConfigureAwait(false);
        TimeOffsetReport report = TimeOffsetReportCommand.Parse(reportFrame, Logger);
        LastTimeOffsetReport = report;
        OnTimeOffsetReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the Time Zone Offset and Daylight Savings Time parameters at a supporting device.
    /// </summary>
    public async Task SetTimeOffsetAsync(TimeOffsetReport offset, CancellationToken cancellationToken)
    {
        var command = TimeOffsetSetCommand.Create(offset);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct TimeOffsetGetCommand : ICommand
    {
        public TimeOffsetGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.TimeOffsetGet;

        public CommandClassFrame Frame { get; }

        public static TimeOffsetGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new TimeOffsetGetCommand(frame);
        }
    }

    internal readonly struct TimeOffsetSetCommand : ICommand
    {
        public TimeOffsetSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.TimeOffsetSet;

        public CommandClassFrame Frame { get; }

        public static TimeOffsetSetCommand Create(TimeOffsetReport offset)
        {
            int tzoTotalMinutes = (int)offset.TimeZoneOffset.TotalMinutes;
            bool signTzo = tzoTotalMinutes < 0;
            int absTzoMinutes = Math.Abs(tzoTotalMinutes);
            byte hourTzo = (byte)(absTzoMinutes / 60);
            byte minuteTzo = (byte)(absTzoMinutes % 60);

            if (hourTzo > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "TimeZoneOffset exceeds the 7-bit hour field limit");
            }

            int dstTotalMinutes = (int)offset.DstOffset.TotalMinutes;
            bool signDst = dstTotalMinutes < 0;
            int absDstMinutes = Math.Abs(dstTotalMinutes);

            if (absDstMinutes > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "DstOffset exceeds the 7-bit minute field limit");
            }

            byte minuteDst = (byte)absDstMinutes;

            Span<byte> commandParameters =
            [
                (byte)((signTzo ? 0b1000_0000 : 0) | (hourTzo & 0b0111_1111)),
                minuteTzo,
                (byte)((signDst ? 0b1000_0000 : 0) | (minuteDst & 0b0111_1111)),
                offset.DstStart.Month,
                offset.DstStart.Day,
                offset.DstStart.Hour,
                offset.DstEnd.Month,
                offset.DstEnd.Day,
                offset.DstEnd.Hour,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new TimeOffsetSetCommand(frame);
        }
    }

    internal readonly struct TimeOffsetReportCommand : ICommand
    {
        public TimeOffsetReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.TimeOffsetReport;

        public CommandClassFrame Frame { get; }

        public static TimeOffsetReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 9)
            {
                logger.LogWarning("Time Offset Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Time Offset Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            bool signTzo = (span[0] & 0b1000_0000) != 0;
            byte hourTzo = (byte)(span[0] & 0b0111_1111);
            byte minuteTzo = span[1];
            int tzoMinutes = hourTzo * 60 + minuteTzo;
            if (signTzo)
            {
                tzoMinutes = -tzoMinutes;
            }

            bool signDst = (span[2] & 0b1000_0000) != 0;
            byte minuteDst = (byte)(span[2] & 0b0111_1111);
            int dstMinutes = signDst ? -minuteDst : minuteDst;

            DstTransition dstStart = new(span[3], span[4], span[5]);
            DstTransition dstEnd = new(span[6], span[7], span[8]);

            return new TimeOffsetReport(
                TimeSpan.FromMinutes(tzoMinutes),
                TimeSpan.FromMinutes(dstMinutes),
                dstStart,
                dstEnd);
        }
    }
}
