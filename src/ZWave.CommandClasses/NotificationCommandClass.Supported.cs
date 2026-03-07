using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the supported notification types of a device.
/// </summary>
public readonly record struct SupportedNotifications(
    /// <summary>
    /// Whether the device supports proprietary V1 Alarm types and levels.
    /// </summary>
    bool SupportsV1Alarm,

    /// <summary>
    /// The set of supported Notification Types.
    /// </summary>
    IReadOnlySet<NotificationType> SupportedNotificationTypes);

public sealed partial class NotificationCommandClass
{
    private Dictionary<NotificationType, SupportedNotificationEvents?>? _supportedNotificationEvents;

    /// <summary>
    /// Gets the supported notification types.
    /// </summary>
    public SupportedNotifications? SupportedNotifications { get; private set; }

    /// <summary>
    /// Gets the supported notification events for each notification type.
    /// </summary>
    public IReadOnlyDictionary<NotificationType, SupportedNotificationEvents?>? SupportedNotificationEvents => _supportedNotificationEvents;

    /// <summary>
    /// Request the supported notification types from the device.
    /// </summary>
    public async Task<SupportedNotifications> GetSupportedAsync(CancellationToken cancellationToken)
    {
        NotificationSupportedGetCommand command = NotificationSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NotificationSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        SupportedNotifications supportedNotifications = NotificationSupportedReportCommand.Parse(reportFrame, Logger);
        ApplySupportedNotifications(supportedNotifications);
        return supportedNotifications;
    }

    private void ApplySupportedNotifications(SupportedNotifications supportedNotifications)
    {
        SupportedNotifications = supportedNotifications;

        Dictionary<NotificationType, SupportedNotificationEvents?> newSupportedNotificationEvents =
            new Dictionary<NotificationType, SupportedNotificationEvents?>(supportedNotifications.SupportedNotificationTypes.Count);
        foreach (NotificationType notificationType in supportedNotifications.SupportedNotificationTypes)
        {
            // Preserve any existing event support data.
            if (_supportedNotificationEvents == null
                || !_supportedNotificationEvents.TryGetValue(notificationType, out SupportedNotificationEvents? existing))
            {
                existing = null;
            }

            newSupportedNotificationEvents.Add(notificationType, existing);
        }

        _supportedNotificationEvents = newSupportedNotificationEvents;
    }

    internal readonly struct NotificationSupportedGetCommand : ICommand
    {
        public NotificationSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static NotificationSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new NotificationSupportedGetCommand(frame);
        }
    }

    internal readonly struct NotificationSupportedReportCommand : ICommand
    {
        public NotificationSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static SupportedNotifications Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Notification Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Notification Supported Report frame is too short");
            }

            bool supportsV1Alarm = (frame.CommandParameters.Span[0] & 0b1000_0000) != 0;

            int numBitMasks = frame.CommandParameters.Span[0] & 0b0001_1111;

            if (frame.CommandParameters.Length < 1 + numBitMasks)
            {
                logger.LogWarning("Notification Supported Report bitmask is truncated (expected {Expected}, got {Actual} bytes)",
                    numBitMasks, frame.CommandParameters.Length - 1);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Notification Supported Report bitmask is truncated");
            }

            HashSet<NotificationType> supportedNotificationTypes = BitMaskHelper.ParseBitMask<NotificationType>(frame.CommandParameters.Span.Slice(1, numBitMasks));

            return new SupportedNotifications(supportsV1Alarm, supportedNotificationTypes);
        }
    }
}
