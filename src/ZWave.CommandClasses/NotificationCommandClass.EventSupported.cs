using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the supported notification events for a given notification type.
/// </summary>
public readonly record struct SupportedNotificationEvents(
    /// <summary>
    /// The notification type these events belong to.
    /// </summary>
    NotificationType NotificationType,

    /// <summary>
    /// The set of supported event/state values for this notification type.
    /// </summary>
    IReadOnlySet<byte> SupportedEvents);

public sealed partial class NotificationCommandClass
{
    /// <summary>
    /// Request the supported events for a specific notification type.
    /// </summary>
    public async Task<SupportedNotificationEvents> GetEventSupportedAsync(
        NotificationType notificationType,
        CancellationToken cancellationToken)
    {
        NotificationEventSupportedGetCommand command = NotificationEventSupportedGetCommand.Create(notificationType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NotificationEventSupportedReportCommand>(
            predicate: frame =>
            {
                return frame.CommandParameters.Length > 0
                    && (NotificationType)frame.CommandParameters.Span[0] == notificationType;
            },
            cancellationToken).ConfigureAwait(false);
        SupportedNotificationEvents supportedEvents = NotificationEventSupportedReportCommand.Parse(reportFrame, Logger);
        _supportedNotificationEvents ??= new();
        _supportedNotificationEvents[supportedEvents.NotificationType] = supportedEvents;
        return supportedEvents;
    }

    internal readonly struct NotificationEventSupportedGetCommand : ICommand
    {
        public NotificationEventSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.EventSupportedGet;

        public CommandClassFrame Frame { get; }

        public static NotificationEventSupportedGetCommand Create(NotificationType notificationType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)notificationType];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new NotificationEventSupportedGetCommand(frame);
        }
    }

    internal readonly struct NotificationEventSupportedReportCommand : ICommand
    {
        public NotificationEventSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.EventSupportedReport;

        public CommandClassFrame Frame { get; }

        public static SupportedNotificationEvents Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Notification Event Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Notification Event Supported Report frame is too short");
            }

            NotificationType notificationType = (NotificationType)frame.CommandParameters.Span[0];

            int numBitMasks = frame.CommandParameters.Span[1] & 0b0001_1111;

            if (frame.CommandParameters.Length < 2 + numBitMasks)
            {
                logger.LogWarning("Notification Event Supported Report bitmask is truncated (expected {Expected}, got {Actual} bytes)",
                    numBitMasks, frame.CommandParameters.Length - 2);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Notification Event Supported Report bitmask is truncated");
            }

            HashSet<byte> supportedNotificationEvents = [];
            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span.Slice(2, numBitMasks);
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        byte notificationEvent = (byte)((byteNum << 3) + bitNum);
                        supportedNotificationEvents.Add(notificationEvent);
                    }
                }
            }

            return new SupportedNotificationEvents(notificationType, supportedNotificationEvents);
        }
    }
}
