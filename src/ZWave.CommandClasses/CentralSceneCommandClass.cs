using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Key attribute values for the Central Scene Notification.
/// </summary>
public enum CentralSceneKeyAttribute : byte
{
    /// <summary>
    /// Key Pressed 1 time.
    /// </summary>
    KeyPressed = 0x00,

    /// <summary>
    /// Key Released.
    /// </summary>
    KeyReleased = 0x01,

    /// <summary>
    /// Key Held Down.
    /// </summary>
    KeyHeldDown = 0x02,

    /// <summary>
    /// Key Pressed 2 times.
    /// </summary>
    KeyPressed2Times = 0x03,

    /// <summary>
    /// Key Pressed 3 times.
    /// </summary>
    KeyPressed3Times = 0x04,

    /// <summary>
    /// Key Pressed 4 times.
    /// </summary>
    KeyPressed4Times = 0x05,

    /// <summary>
    /// Key Pressed 5 times.
    /// </summary>
    KeyPressed5Times = 0x06,
}

/// <summary>
/// Commands for the Central Scene Command Class.
/// </summary>
public enum CentralSceneCommand : byte
{
    /// <summary>
    /// Request the supported scenes and key attributes.
    /// </summary>
    SupportedGet = 0x01,

    /// <summary>
    /// Advertise the supported scenes and key attributes.
    /// </summary>
    SupportedReport = 0x02,

    /// <summary>
    /// Advertise a scene activation event.
    /// </summary>
    Notification = 0x03,

    /// <summary>
    /// Configure optional node capabilities for scene notifications (version 3).
    /// </summary>
    ConfigurationSet = 0x04,

    /// <summary>
    /// Request the configuration of optional node capabilities (version 3).
    /// </summary>
    ConfigurationGet = 0x05,

    /// <summary>
    /// Advertise the configuration of optional node capabilities (version 3).
    /// </summary>
    ConfigurationReport = 0x06,
}

/// <summary>
/// Implementation of the Central Scene Command Class (versions 1–3).
/// </summary>
/// <remarks>
/// <para>The Central Scene Command Class is used to communicate central scene activations
/// to a central controller using the lifeline concept. A scene is typically activated via
/// a push button on the device.</para>
/// <para>Version 2 extends version 1 by adding per-scene key attribute bitmasks and additional
/// key attributes (multi-tap).</para>
/// <para>Version 3 adds the Slow Refresh capability and Configuration commands.</para>
/// </remarks>
[CommandClass(CommandClassId.CentralScene)]
public sealed partial class CentralSceneCommandClass : CommandClass<CentralSceneCommand>
{
    private byte? _lastNotificationSequenceNumber;

    internal CentralSceneCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(CentralSceneCommand command)
        => command switch
        {
            CentralSceneCommand.SupportedGet => true,
            CentralSceneCommand.ConfigurationSet => Version.HasValue ? Version >= 3 : null,
            CentralSceneCommand.ConfigurationGet => Version.HasValue ? Version >= 3 : null,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(CentralSceneCommand.ConfigurationGet).GetValueOrDefault())
        {
            _ = await GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((CentralSceneCommand)frame.CommandId)
        {
            case CentralSceneCommand.Notification:
            {
                CentralSceneNotification notification = CentralSceneNotificationCommand.Parse(frame, Logger);

                // Per spec: "The receiving device uses the sequence number to ignore duplicates."
                if (_lastNotificationSequenceNumber.HasValue
                    && notification.SequenceNumber == _lastNotificationSequenceNumber.Value)
                {
                    return;
                }

                _lastNotificationSequenceNumber = notification.SequenceNumber;
                LastNotification = notification;
                OnNotificationReceived?.Invoke(notification);
                break;
            }
            case CentralSceneCommand.ConfigurationReport:
            {
                CentralSceneConfigurationReport report = CentralSceneConfigurationReportCommand.Parse(frame, Logger);
                LastConfiguration = report;
                OnConfigurationReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
