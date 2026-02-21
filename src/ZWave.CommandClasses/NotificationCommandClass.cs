using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the type of notification.
/// </summary>
public enum NotificationType : byte
{
    SmokeAlarm = 0x01,
    COAlarm = 0x02,
    CO2Alarm = 0x03,
    HeatAlarm = 0x04,
    WaterAlarm = 0x05,
    AccessControl = 0x06,
    HomeSecurity = 0x07,
    PowerManagement = 0x08,
    System = 0x09,
    EmergencyAlarm = 0x0a,
    Clock = 0x0b,
    Appliance = 0x0c,
    HomeHealth = 0x0d,
    Siren = 0x0e,
    WaterValve = 0x0f,
    WeatherAlarm = 0x10,
    Irrigation = 0x11,
    GasAlarm = 0x12,
    PestControl = 0x13,
    LightSensor = 0x14,
    WaterQualityMonitoring = 0x15,
    HomeMonitoring = 0x16,
    RequestPendingNotification = 0xff,
}

public enum NotificationCommand : byte
{
    /// <summary>
    /// Request the supported Notifications for a specified Notification Type.
    /// </summary>
    EventSupportedGet = 0x01,

    /// <summary>
    /// Advertise supported events/states for a specified Notification Type
    /// </summary>
    EventSupportedReport = 0x02,

    /// <summary>
    /// Request if the unsolicited transmission of a specific Notification Type is enabled
    /// </summary>
    Get = 0x04,

    /// <summary>
    /// Advertises an event or state Notification
    /// </summary>
    Report = 0x05,

    /// <summary>
    /// Enable or disable the unsolicited transmission of a specific Notification Type
    /// </summary>
    Set = 0x06,

    /// <summary>
    /// Request the supported notification types.
    /// </summary>
    SupportedGet = 0x07,

    /// <summary>
    /// Advertise the supported notification types in the application.
    /// </summary>
    SupportedReport = 0x08,
}

/// <summary>
/// Represents a notification received from a device.
/// </summary>
public readonly record struct Notification(
    /// <summary>
    /// Gets the legacy V1 alarm type.
    /// </summary>
    byte? V1AlarmType,

    /// <summary>
    /// Gets the legacy V1 alarm level.
    /// </summary>
    byte? V1AlarmLevel,

    /// <summary>
    /// Zensor Net Source Node ID, which detected the alarm condition.
    /// </summary>
    byte? ZensorNetSourceNodeId,

    /// <summary>
    /// Gets the notification status.
    /// </summary>
    bool? NotificationStatus,

    /// <summary>
    /// Gets the notification type.
    /// </summary>
    byte? NotificationType,

    /// <summary>
    /// Gets the notification event.
    /// </summary>
    byte? NotificationEvent,

    /// <summary>
    /// Gets the event parameters.
    /// </summary>
    ReadOnlyMemory<byte>? EventParameters,

    /// <summary>
    /// Gets the sequence number.
    /// </summary>
    byte? SequenceNumber);

/// <summary>
/// Represents the supported notification types of a device.
/// </summary>
public readonly struct SupportedNotifications
{
    public SupportedNotifications(
        bool supportsV1Alarm,
        IReadOnlySet<NotificationType> supportedNotificationTypes)
    {
        SupportsV1Alarm = supportsV1Alarm;
        SupportedNotificationTypes = supportedNotificationTypes;
    }

    public bool SupportsV1Alarm { get; }

    public IReadOnlySet<NotificationType> SupportedNotificationTypes { get; }
}

/// <summary>
/// Represents the supported notification events for a given notification type.
/// </summary>
public readonly struct SupportedNotificationEvents
{
    public SupportedNotificationEvents(
        NotificationType notificationType,
        IReadOnlySet<byte> supportedNotificationEvents)
    {
        NotificationType = notificationType;
        SupportedEvents = supportedNotificationEvents;
    }

    public NotificationType NotificationType { get; }

    public IReadOnlySet<byte> SupportedEvents { get; }
}

// Note: Version < 3 is the Alarm Command Class
[CommandClass(CommandClassId.Notification)]
public sealed class NotificationCommandClass : CommandClass<NotificationCommand>
{
    private Dictionary<NotificationType, SupportedNotificationEvents?>? _supportedNotificationEvents;

    internal NotificationCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    // TODO: Should be an event. Although shouldn't all state changes?

    /// <summary>
    /// Gets the last received notification.
    /// </summary>
    public Notification? LastNotification { get; private set; }

    /// <summary>
    /// Gets the supported notification types.
    /// </summary>
    public SupportedNotifications? SupportedNotifications { get; private set; }

    /// <summary>
    /// Gets the supported notification events for each notification type.
    /// </summary>
    public IReadOnlyDictionary<NotificationType, SupportedNotificationEvents?>? SupportedNotificationEvents => _supportedNotificationEvents;

    /// <inheritdoc />
    public override bool? IsCommandSupported(NotificationCommand command)
        => command switch
        {
            NotificationCommand.Get => true,
            NotificationCommand.Set => Version.HasValue ? Version >= 2 : null,
            NotificationCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            NotificationCommand.EventSupportedGet => Version.HasValue ? Version >= 3 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(NotificationCommand.SupportedGet).GetValueOrDefault(false))
        {
            SupportedNotifications supportedNotifications = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

            if (IsCommandSupported(NotificationCommand.EventSupportedGet).GetValueOrDefault(false))
            {
                foreach (NotificationType notificationType in supportedNotifications.SupportedNotificationTypes)
                {
                    _ = await GetEventSupportedAsync(notificationType, cancellationToken);

                    // TODO: Determine whether this is the push or pull mode. Foe this we need to implement the AGI CC and
                    //       Some other semi-complicated logic. For now, assume push.

                    // Enable reports
                    await SetAsync(notificationType, true, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task<Notification> GetV1Async(byte alarmType, CancellationToken cancellationToken)
    {
        NotificationGetV1Command command = NotificationGetV1Command.Create(alarmType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NotificationReportCommand>(cancellationToken).ConfigureAwait(false);
        Notification notification = NotificationReportCommand.Parse(reportFrame, Logger);
        LastNotification = notification;
        return notification;
    }

    public async Task<Notification> GetAsync(NotificationType notificationType, byte? notificationEvent, CancellationToken cancellationToken)
    {
        NotificationGetCommand command = NotificationGetCommand.Create(EffectiveVersion, notificationType, notificationEvent);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NotificationReportCommand>(cancellationToken).ConfigureAwait(false);
        Notification notification = NotificationReportCommand.Parse(reportFrame, Logger);
        LastNotification = notification;
        return notification;
    }

    public async Task SetAsync(NotificationType notificationType, bool notificationStatus, CancellationToken cancellationToken)
    {
        NotificationSetCommand command = NotificationSetCommand.Create(notificationType, notificationStatus);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        // No report is expected from this command.
    }

    public async Task<SupportedNotifications> GetSupportedAsync(CancellationToken cancellationToken)
    {
        NotificationSupportedGetCommand command = NotificationSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NotificationSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        SupportedNotifications supportedNotifications = NotificationSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedNotifications = supportedNotifications;

        Dictionary<NotificationType, SupportedNotificationEvents?> newSupportedNotificationEvents =
            new Dictionary<NotificationType, SupportedNotificationEvents?>(supportedNotifications.SupportedNotificationTypes.Count);
        foreach (NotificationType notificationType in supportedNotifications.SupportedNotificationTypes)
        {
            if (SupportedNotificationEvents == null
                || !SupportedNotificationEvents.TryGetValue(notificationType, out SupportedNotificationEvents? existing))
            {
                existing = null;
            }

            newSupportedNotificationEvents.Add(notificationType, existing);
        }

        _supportedNotificationEvents = newSupportedNotificationEvents;

        return supportedNotifications;
    }

    public async Task<SupportedNotificationEvents> GetEventSupportedAsync(NotificationType notificationType, CancellationToken cancellationToken)
    {
        NotificationEventSupportedGetCommand command = NotificationEventSupportedGetCommand.Create(notificationType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NotificationEventSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        SupportedNotificationEvents supportedEvents = NotificationEventSupportedReportCommand.Parse(reportFrame, Logger);
        _supportedNotificationEvents![supportedEvents.NotificationType] = supportedEvents;
        return supportedEvents;
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((NotificationCommand)frame.CommandId)
        {
            case NotificationCommand.Get:
            case NotificationCommand.Set:
            case NotificationCommand.SupportedGet:
            case NotificationCommand.EventSupportedGet:
            {
                break;
            }
            case NotificationCommand.Report:
            {
                LastNotification = NotificationReportCommand.Parse(frame, Logger);
                break;
            }
            case NotificationCommand.SupportedReport:
            {
                SupportedNotifications supportedNotifications = NotificationSupportedReportCommand.Parse(frame, Logger);
                SupportedNotifications = supportedNotifications;

                Dictionary<NotificationType, SupportedNotificationEvents?> newSupportedNotificationEvents =
                    new Dictionary<NotificationType, SupportedNotificationEvents?>(supportedNotifications.SupportedNotificationTypes.Count);
                foreach (NotificationType notificationType in supportedNotifications.SupportedNotificationTypes)
                {
                    // Persist any existing known state.
                    if (SupportedNotificationEvents == null
                        || !SupportedNotificationEvents.TryGetValue(notificationType, out SupportedNotificationEvents? supportedNotificationEvents))
                    {
                        supportedNotificationEvents = null;
                    }

                    newSupportedNotificationEvents.Add(notificationType, supportedNotificationEvents);
                }

                _supportedNotificationEvents = newSupportedNotificationEvents;
                break;
            }
            case NotificationCommand.EventSupportedReport:
            {
                SupportedNotificationEvents supportedEvents = NotificationEventSupportedReportCommand.Parse(frame, Logger);
                _supportedNotificationEvents![supportedEvents.NotificationType] = supportedEvents;
                break;
            }
        }
    }

    private readonly struct NotificationGetV1Command : ICommand
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

    private readonly struct NotificationGetCommand : ICommand
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

    private readonly struct NotificationReportCommand : ICommand
    {
        public NotificationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Notification;

        public static byte CommandId => (byte)NotificationCommand.Report;

        public CommandClassFrame Frame { get; }

        public static Notification Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Notification Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Notification Report frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            int length = frame.CommandParameters.Length;

            // Determine notification event first (needed for V1 fallback logic)
            byte? notificationEvent = length > 5 ? parameters[5] : null;

            // V1 values are used when there's no notification event or event indicates V1 fallback (0xFE)
            bool shouldUseV1Values = !notificationEvent.HasValue || notificationEvent.Value == 0xfe;

            byte? v1AlarmType = null;
            byte? v1AlarmLevel = null;
            if (shouldUseV1Values)
            {
                if (length > 0 && parameters[0] != 0)
                {
                    v1AlarmType = parameters[0];
                }

                if (length > 1 && parameters[1] != 0)
                {
                    v1AlarmLevel = parameters[1];
                }
            }

            byte? zensorNetSourceNodeId = length > 2 ? parameters[2] : null;

            bool? notificationStatus = length > 3
                ? parameters[3] switch
                {
                    0x00 => false,
                    0xff => true,
                    _ => null,
                }
                : null;

            byte? notificationType = length > 4 ? parameters[4] : null;

            int numEventParameters = length > 6 ? (parameters[6] & 0b0001_1111) : 0;

            ReadOnlyMemory<byte>? eventParameters = numEventParameters > 0 && length >= 7 + numEventParameters
                ? frame.CommandParameters.Slice(7, numEventParameters)
                : null;

            byte? sequenceNumber = null;
            if (length > 6 && (parameters[6] & 0b1000_0000) != 0)
            {
                int seqIndex = 7 + numEventParameters;
                if (length > seqIndex)
                {
                    sequenceNumber = parameters[seqIndex];
                }
            }

            return new Notification(
                v1AlarmType,
                v1AlarmLevel,
                zensorNetSourceNodeId,
                notificationStatus,
                notificationType,
                notificationEvent,
                eventParameters,
                sequenceNumber);
        }
    }

    private readonly struct NotificationSetCommand : ICommand
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

    private readonly struct NotificationSupportedGetCommand : ICommand
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

    private readonly struct NotificationSupportedReportCommand : ICommand
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

            HashSet<NotificationType> supportedNotificationTypes = new HashSet<NotificationType>();
            int numBitMasks = (frame.CommandParameters.Span[0] & 0b0001_1111);
            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span.Slice(1, numBitMasks);
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        NotificationType notificationType = (NotificationType)((byteNum << 3) + bitNum);
                        supportedNotificationTypes.Add(notificationType);
                    }
                }
            }

            return new SupportedNotifications(supportsV1Alarm, supportedNotificationTypes);
        }
    }

    private readonly struct NotificationEventSupportedGetCommand : ICommand
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

    private readonly struct NotificationEventSupportedReportCommand : ICommand
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
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Notification Event Supported Report frame is too short");
            }

            NotificationType notificationType = (NotificationType)frame.CommandParameters.Span[0];

            HashSet<byte> supportedNotificationEvents = new HashSet<byte>();
            int numBitMasks = (frame.CommandParameters.Span[1] & 0b0001_1111);
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
