using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the type of notification.
/// </summary>
public enum NotificationType : byte
{
    /// <summary>
    /// Smoke alarm notification.
    /// </summary>
    SmokeAlarm = 0x01,

    /// <summary>
    /// CO alarm notification.
    /// </summary>
    COAlarm = 0x02,

    /// <summary>
    /// CO2 alarm notification.
    /// </summary>
    CO2Alarm = 0x03,

    /// <summary>
    /// Heat alarm notification.
    /// </summary>
    HeatAlarm = 0x04,

    /// <summary>
    /// Water alarm notification.
    /// </summary>
    WaterAlarm = 0x05,

    /// <summary>
    /// Access control notification (e.g. door lock).
    /// </summary>
    AccessControl = 0x06,

    /// <summary>
    /// Home security notification (e.g. motion detection, intrusion).
    /// </summary>
    HomeSecurity = 0x07,

    /// <summary>
    /// Power management notification.
    /// </summary>
    PowerManagement = 0x08,

    /// <summary>
    /// System notification.
    /// </summary>
    System = 0x09,

    /// <summary>
    /// Emergency alarm notification.
    /// </summary>
    EmergencyAlarm = 0x0a,

    /// <summary>
    /// Clock notification.
    /// </summary>
    Clock = 0x0b,

    /// <summary>
    /// Appliance notification.
    /// </summary>
    Appliance = 0x0c,

    /// <summary>
    /// Home health notification.
    /// </summary>
    HomeHealth = 0x0d,

    /// <summary>
    /// Siren notification.
    /// </summary>
    Siren = 0x0e,

    /// <summary>
    /// Water valve notification.
    /// </summary>
    WaterValve = 0x0f,

    /// <summary>
    /// Weather alarm notification.
    /// </summary>
    WeatherAlarm = 0x10,

    /// <summary>
    /// Irrigation notification.
    /// </summary>
    Irrigation = 0x11,

    /// <summary>
    /// Gas alarm notification.
    /// </summary>
    GasAlarm = 0x12,

    /// <summary>
    /// Pest control notification.
    /// </summary>
    PestControl = 0x13,

    /// <summary>
    /// Light sensor notification.
    /// </summary>
    LightSensor = 0x14,

    /// <summary>
    /// Water quality monitoring notification.
    /// </summary>
    WaterQualityMonitoring = 0x15,

    /// <summary>
    /// Home monitoring notification.
    /// </summary>
    HomeMonitoring = 0x16,

    /// <summary>
    /// Special-purpose value used in Get commands to request the first available notification.
    /// </summary>
    RequestPendingNotification = 0xff,
}

/// <summary>
/// Defines the commands for the Notification Command Class (V3-V8).
/// </summary>
public enum NotificationCommand : byte
{
    /// <summary>
    /// Request the supported notifications for a specified Notification Type.
    /// </summary>
    EventSupportedGet = 0x01,

    /// <summary>
    /// Advertise supported events/states for a specified Notification Type.
    /// </summary>
    EventSupportedReport = 0x02,

    /// <summary>
    /// Request the notification status or retrieve a notification from the queue.
    /// </summary>
    Get = 0x04,

    /// <summary>
    /// Advertise an event or state notification.
    /// </summary>
    Report = 0x05,

    /// <summary>
    /// Enable or disable unsolicited transmission of a specific Notification Type, or clear a persistent notification.
    /// </summary>
    Set = 0x06,

    /// <summary>
    /// Request the supported notification types.
    /// </summary>
    SupportedGet = 0x07,

    /// <summary>
    /// Advertise the supported notification types.
    /// </summary>
    SupportedReport = 0x08,
}

/// <summary>
/// Implements the Notification Command Class (V3-V8), used to advertise events or states
/// such as movement detection, door open/close, or system failure.
/// </summary>
/// <remarks>
/// This CC supersedes the Alarm Command Class (V1-V2). V1 Alarm fields are preserved in reports
/// for backwards compatibility but the primary interface uses Notification Types and Events.
/// </remarks>
[CommandClass(CommandClassId.Notification)]
public sealed partial class NotificationCommandClass : CommandClass<NotificationCommand>
{
    internal NotificationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

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
                    _ = await GetEventSupportedAsync(notificationType, cancellationToken).ConfigureAwait(false);

                    // TODO: Determine whether this is the push or pull mode. For this we need to implement the AGI CC and
                    //       some other semi-complicated logic. For now, assume push.

                    // Enable reports
                    await SetAsync(notificationType, true, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((NotificationCommand)frame.CommandId)
        {
            case NotificationCommand.Report:
            {
                NotificationReport report = NotificationReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnNotificationReportReceived?.Invoke(report);
                break;
            }
            case NotificationCommand.SupportedReport:
            {
                SupportedNotifications supportedNotifications = NotificationSupportedReportCommand.Parse(frame, Logger);
                ApplySupportedNotifications(supportedNotifications);
                break;
            }
            case NotificationCommand.EventSupportedReport:
            {
                SupportedNotificationEvents supportedEvents = NotificationEventSupportedReportCommand.Parse(frame, Logger);
                _supportedNotificationEvents ??= new();
                _supportedNotificationEvents[supportedEvents.NotificationType] = supportedEvents;
                break;
            }
        }
    }
}
