using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The type of data carried in an Entry Control notification.
/// </summary>
public enum EntryControlDataType : byte
{
    /// <summary>
    /// No data included.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// 1 to 32 bytes of arbitrary binary data.
    /// </summary>
    Raw = 0x01,

    /// <summary>
    /// 1 to 32 ASCII encoded characters (codes 0x00-0xF7), padded with 0xFF to 16-byte blocks.
    /// </summary>
    Ascii = 0x02,

    /// <summary>
    /// 16 bytes of MD5 hash data.
    /// </summary>
    Md5 = 0x03,
}

/// <summary>
/// The type of event reported by an Entry Control device.
/// </summary>
public enum EntryControlEventType : byte
{
    /// <summary>
    /// Indicates the user has started entering credentials and caching is initiated.
    /// </summary>
    Caching = 0x00,

    /// <summary>
    /// Sends cached user inputs when the cache is full, timed out, or a command button is pressed.
    /// </summary>
    CachedKeys = 0x01,

    /// <summary>
    /// The Enter command button was pressed.
    /// </summary>
    Enter = 0x02,

    /// <summary>
    /// The Disarm command button was pressed.
    /// </summary>
    DisarmAll = 0x03,

    /// <summary>
    /// The Arm command button was pressed.
    /// </summary>
    ArmAll = 0x04,

    /// <summary>
    /// The Arm Away command button was pressed.
    /// </summary>
    ArmAway = 0x05,

    /// <summary>
    /// The Arm Home command button was pressed.
    /// </summary>
    ArmHome = 0x06,

    /// <summary>
    /// The Exit Delay / Arm Delay command button was pressed.
    /// </summary>
    ExitDelay = 0x07,

    /// <summary>
    /// The Arm Zone 1 command button was pressed.
    /// </summary>
    Arm1 = 0x08,

    /// <summary>
    /// The Arm Zone 2 command button was pressed.
    /// </summary>
    Arm2 = 0x09,

    /// <summary>
    /// The Arm Zone 3 command button was pressed.
    /// </summary>
    Arm3 = 0x0A,

    /// <summary>
    /// The Arm Zone 4 command button was pressed.
    /// </summary>
    Arm4 = 0x0B,

    /// <summary>
    /// The Arm Zone 5 command button was pressed.
    /// </summary>
    Arm5 = 0x0C,

    /// <summary>
    /// The Arm Zone 6 command button was pressed.
    /// </summary>
    Arm6 = 0x0D,

    /// <summary>
    /// An RFID tag was presented.
    /// </summary>
    Rfid = 0x0E,

    /// <summary>
    /// The Bell button was pressed.
    /// </summary>
    Bell = 0x0F,

    /// <summary>
    /// The Fire button was pressed.
    /// </summary>
    Fire = 0x10,

    /// <summary>
    /// The Police button was pressed.
    /// </summary>
    Police = 0x11,

    /// <summary>
    /// A panic alert was triggered.
    /// </summary>
    AlertPanic = 0x12,

    /// <summary>
    /// A medical alert was triggered.
    /// </summary>
    AlertMedical = 0x13,

    /// <summary>
    /// The Gate Open command button was pressed.
    /// </summary>
    GateOpen = 0x14,

    /// <summary>
    /// The Gate Close command button was pressed.
    /// </summary>
    GateClose = 0x15,

    /// <summary>
    /// The Lock command was issued.
    /// </summary>
    Lock = 0x16,

    /// <summary>
    /// The Unlock command was issued.
    /// </summary>
    Unlock = 0x17,

    /// <summary>
    /// The Test button was pressed.
    /// </summary>
    Test = 0x18,

    /// <summary>
    /// The Cancel button was pressed.
    /// </summary>
    Cancel = 0x19,
}

/// <summary>
/// Commands for the Entry Control Command Class.
/// </summary>
public enum EntryControlCommand : byte
{
    /// <summary>
    /// Advertises user input from the Entry Control device.
    /// </summary>
    Notification = 0x01,

    /// <summary>
    /// Requests the supported keys for credential entry.
    /// </summary>
    KeySupportedGet = 0x02,

    /// <summary>
    /// Advertises the supported keys for credential entry.
    /// </summary>
    KeySupportedReport = 0x03,

    /// <summary>
    /// Requests the supported events, data types, and configuration ranges.
    /// </summary>
    EventSupportedGet = 0x04,

    /// <summary>
    /// Advertises the supported events, data types, and configuration ranges.
    /// </summary>
    EventSupportedReport = 0x05,

    /// <summary>
    /// Configures the key cache size and timeout.
    /// </summary>
    ConfigurationSet = 0x06,

    /// <summary>
    /// Requests the current configuration.
    /// </summary>
    ConfigurationGet = 0x07,

    /// <summary>
    /// Advertises the current configuration.
    /// </summary>
    ConfigurationReport = 0x08,
}

/// <summary>
/// Implementation of the Entry Control Command Class (version 1).
/// </summary>
/// <remarks>
/// The Entry Control Command Class defines a method for advertising user input to a central
/// Entry Control application and for discovery of capabilities. User input may be button presses,
/// RFID tags, or other means.
/// </remarks>
[CommandClass(CommandClassId.EntryControl)]
public sealed partial class EntryControlCommandClass : CommandClass<EntryControlCommand>
{
    private byte? _lastNotificationSequenceNumber;

    internal EntryControlCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(EntryControlCommand command)
        => command switch
        {
            EntryControlCommand.KeySupportedGet => true,
            EntryControlCommand.EventSupportedGet => true,
            EntryControlCommand.ConfigurationSet => true,
            EntryControlCommand.ConfigurationGet => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Per spec Figure 6.11:
        // 1. Key Supported Get
        // 2. Event Supported Get
        // 3. Configuration Get
        _ = await GetSupportedKeysAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetEventSupportedAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((EntryControlCommand)frame.CommandId)
        {
            case EntryControlCommand.Notification:
            {
                EntryControlNotification notification = EntryControlNotificationCommand.Parse(frame, Logger);

                // Per spec: "A receiving device MUST use the Sequence Number to detect and ignore duplicates."
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
        }
    }
}
