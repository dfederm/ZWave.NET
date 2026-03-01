using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the source of accurate local time.
/// </summary>
public enum TimeSource : byte
{
    /// <summary>
    /// Time is set via the Z-Wave network.
    /// </summary>
    /// <remarks>
    /// In Time CC versions 1 and 2, this field was reserved and set to 0,
    /// which should be interpreted as unknown but may be assumed to be Z-Wave.
    /// </remarks>
    ZWave = 0x00,

    /// <summary>
    /// Time is sourced from GPS or satellite navigation.
    /// </summary>
    GpsSatelliteNav = 0x01,

    /// <summary>
    /// Time is sourced from Wi-Fi or the internet.
    /// </summary>
    WiFiInternet = 0x02,
}

/// <summary>
/// Represents a Time Report received from a device.
/// </summary>
public readonly record struct TimeReport(
    /// <summary>
    /// Indicates if the Real-Time Clock has been stopped and the advertised time might be inaccurate.
    /// </summary>
    bool RtcFailure,

    /// <summary>
    /// Identifies the source of accurate local time.
    /// </summary>
    TimeSource TimeSource,

    /// <summary>
    /// The current local time.
    /// </summary>
    TimeOnly Time);

public sealed partial class TimeCommandClass
{
    /// <summary>
    /// Event raised when a Time Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<TimeReport>? OnTimeReportReceived;

    /// <summary>
    /// Gets the last time report received from the device.
    /// </summary>
    public TimeReport? LastTimeReport { get; private set; }

    /// <summary>
    /// Request the current time from a supporting device.
    /// </summary>
    public async Task<TimeReport> GetTimeAsync(CancellationToken cancellationToken)
    {
        TimeGetCommand command = TimeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<TimeReportCommand>(cancellationToken).ConfigureAwait(false);
        TimeReport report = TimeReportCommand.Parse(reportFrame, Logger);
        LastTimeReport = report;
        OnTimeReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the current time at a supporting device.
    /// </summary>
    public async Task SetTimeAsync(TimeOnly time, CancellationToken cancellationToken)
    {
        var command = TimeSetCommand.Create(time);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct TimeGetCommand : ICommand
    {
        public TimeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.TimeGet;

        public CommandClassFrame Frame { get; }

        public static TimeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new TimeGetCommand(frame);
        }
    }

    internal readonly struct TimeSetCommand : ICommand
    {
        public TimeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.TimeSet;

        public CommandClassFrame Frame { get; }

        public static TimeSetCommand Create(TimeOnly time)
        {
            // Byte 0: [Reserved (bits 7-5)] [Hour (bits 4-0)]
            ReadOnlySpan<byte> commandParameters =
            [
                (byte)(time.Hour & 0b0001_1111),
                (byte)time.Minute,
                (byte)time.Second,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new TimeSetCommand(frame);
        }
    }

    internal readonly struct TimeReportCommand : ICommand
    {
        public TimeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.TimeReport;

        public CommandClassFrame Frame { get; }

        public static TimeReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Time Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Time Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            bool rtcFailure = (span[0] & 0b1000_0000) != 0;
            TimeSource timeSource = (TimeSource)((span[0] & 0b0110_0000) >> 5);
            int hour = span[0] & 0b0001_1111;
            int minute = span[1];
            int second = span[2];

            try
            {
                return new TimeReport(rtcFailure, timeSource, new TimeOnly(hour, minute, second));
            }
            catch (ArgumentOutOfRangeException)
            {
                logger.LogWarning("Time Report has invalid time values (hour={Hour}, minute={Minute}, second={Second})", hour, minute, second);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Time Report has invalid time values");
            }
        }
    }
}
