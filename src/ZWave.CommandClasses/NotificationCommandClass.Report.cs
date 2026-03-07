using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Notification Report received from a device.
/// </summary>
public readonly record struct NotificationReport(
    /// <summary>
    /// The legacy V1 alarm type. Set to 0x00 if V1 alarms are not supported.
    /// </summary>
    byte V1AlarmType,

    /// <summary>
    /// The legacy V1 alarm level. Set to 0x00 if V1 alarms are not supported.
    /// </summary>
    byte V1AlarmLevel,

    /// <summary>
    /// The notification status. For push mode: true = enabled, false = disabled.
    /// </summary>
    bool? NotificationStatus,

    /// <summary>
    /// The notification type.
    /// </summary>
    NotificationType? NotificationType,

    /// <summary>
    /// The notification event or state. Values are defined per Notification Type in the spec.
    /// </summary>
    byte? NotificationEvent,

    /// <summary>
    /// The event/state parameters, if any.
    /// </summary>
    ReadOnlyMemory<byte>? EventParameters,

    /// <summary>
    /// The sequence number for the notification, if present.
    /// </summary>
    byte? SequenceNumber);

public sealed partial class NotificationCommandClass
{
    /// <summary>
    /// Gets the last received notification report.
    /// </summary>
    public NotificationReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Notification Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<NotificationReport>? OnNotificationReportReceived;

    /// <summary>
    /// Request a notification using the V1 Alarm Get format (1-byte command).
    /// </summary>
    public async Task<NotificationReport> GetV1Async(byte alarmType, CancellationToken cancellationToken)
    {
        NotificationGetV1Command command = NotificationGetV1Command.Create(alarmType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NotificationReportCommand>(cancellationToken).ConfigureAwait(false);
        NotificationReport report = NotificationReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnNotificationReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Request the notification status or current state for a Notification Type.
    /// </summary>
    public async Task<NotificationReport> GetAsync(
        NotificationType notificationType,
        byte? notificationEvent,
        CancellationToken cancellationToken)
    {
        NotificationGetCommand command = NotificationGetCommand.Create(EffectiveVersion, notificationType, notificationEvent);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame;
        if (notificationType != NotificationType.RequestPendingNotification)
        {
            reportFrame = await AwaitNextReportAsync<NotificationReportCommand>(
                predicate: frame =>
                {
                    return frame.CommandParameters.Length > 4
                        && (NotificationType)frame.CommandParameters.Span[4] == notificationType;
                },
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            reportFrame = await AwaitNextReportAsync<NotificationReportCommand>(cancellationToken).ConfigureAwait(false);
        }

        NotificationReport report = NotificationReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnNotificationReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Enable or disable unsolicited transmission of a specific Notification Type.
    /// </summary>
    public async Task SetAsync(
        NotificationType notificationType,
        bool notificationStatus,
        CancellationToken cancellationToken)
    {
        NotificationSetCommand command = NotificationSetCommand.Create(notificationType, notificationStatus);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct NotificationGetV1Command : ICommand
    {
        public NotificationGetV1Command(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static NotificationGetV1Command Create(byte alarmType)
        {
            ReadOnlySpan<byte> commandParameters = [alarmType];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new NotificationGetV1Command(frame);
        }
    }

    internal readonly struct NotificationGetCommand : ICommand
    {
        public NotificationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static NotificationGetCommand Create(
            byte version,
            NotificationType notificationType,
            byte? notificationEvent)
        {
            bool includeNotificationEvent = version >= 3;
            Span<byte> commandParameters = stackalloc byte[2 + (includeNotificationEvent ? 1 : 0)];
            commandParameters[0] = 0;
            commandParameters[1] = (byte)notificationType;
            if (includeNotificationEvent)
            {
                commandParameters[2] = notificationType == NotificationType.RequestPendingNotification
                    ? (byte)0x00
                    : notificationEvent.GetValueOrDefault(0);
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new NotificationGetCommand(frame);
        }
    }

    internal readonly struct NotificationReportCommand : ICommand
    {
        public NotificationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.Report;

        public CommandClassFrame Frame { get; }

        public static NotificationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Notification Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Notification Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            int length = span.Length;

            byte v1AlarmType = span[0];
            byte v1AlarmLevel = span[1];

            // span[2] is reserved (was Zensor Net Source Node ID, deprecated V4+)

            bool? notificationStatus = length > 3
                ? span[3] switch
                {
                    0x00 => false,
                    0xff => true,
                    _ => null,
                }
                : null;

            NotificationType? notificationType = length > 4
                ? (NotificationType)span[4]
                : null;

            byte? notificationEvent = length > 5 ? span[5] : null;

            int numEventParameters = length > 6 ? (span[6] & 0b0001_1111) : 0;
            bool hasSequence = length > 6 && (span[6] & 0b1000_0000) != 0;

            ReadOnlyMemory<byte>? eventParameters = null;
            if (numEventParameters > 0 && length >= 7 + numEventParameters)
            {
                eventParameters = frame.CommandParameters.Slice(7, numEventParameters);
            }

            byte? sequenceNumber = null;
            if (hasSequence)
            {
                int seqIndex = 7 + numEventParameters;
                if (length > seqIndex)
                {
                    sequenceNumber = span[seqIndex];
                }
            }

            return new NotificationReport(
                v1AlarmType,
                v1AlarmLevel,
                notificationStatus,
                notificationType,
                notificationEvent,
                eventParameters,
                sequenceNumber);
        }
    }

    internal readonly struct NotificationSetCommand : ICommand
    {
        public NotificationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static NotificationSetCommand Create(NotificationType notificationType, bool notificationStatus)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)notificationType, notificationStatus ? (byte)0xff : (byte)0x00];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new NotificationSetCommand(frame);
        }
    }
}
