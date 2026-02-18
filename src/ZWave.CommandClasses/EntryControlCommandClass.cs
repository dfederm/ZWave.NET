namespace ZWave.CommandClasses;

public enum EntryControlCommand : byte
{
    /// <summary>
    /// Advertise an entry control event from a device.
    /// </summary>
    Notification = 0x01,

    /// <summary>
    /// Request the supported keys from a device.
    /// </summary>
    KeySupportedGet = 0x02,

    /// <summary>
    /// Advertise the supported keys.
    /// </summary>
    KeySupportedReport = 0x03,

    /// <summary>
    /// Request the supported event types and data types from a device.
    /// </summary>
    EventSupportedGet = 0x04,

    /// <summary>
    /// Advertise the supported event types and data types.
    /// </summary>
    EventSupportedReport = 0x05,

    /// <summary>
    /// Set the entry control configuration at the receiving node.
    /// </summary>
    ConfigurationSet = 0x06,

    /// <summary>
    /// Request the entry control configuration from a node.
    /// </summary>
    ConfigurationGet = 0x07,

    /// <summary>
    /// Advertise the entry control configuration at the sending node.
    /// </summary>
    ConfigurationReport = 0x08,
}

/// <summary>
/// Identifies the type of entry control event.
/// </summary>
public enum EntryControlEventType : byte
{
    Caching = 0x00,
    CachedKeys = 0x01,
    Enter = 0x02,
    DisarmAll = 0x03,
    ArmAll = 0x04,
    ArmAway = 0x05,
    ArmHome = 0x06,
    ExitDelay = 0x07,
    Arm1 = 0x08,
    Arm2 = 0x09,
    Arm3 = 0x0A,
    Arm4 = 0x0B,
    Arm5 = 0x0C,
    Arm6 = 0x0D,
    Rfid = 0x0E,
    Bell = 0x0F,
    Fire = 0x10,
    Police = 0x11,
    AlertPanic = 0x12,
    AlertMedical = 0x13,
    GateOpen = 0x14,
    GateClose = 0x15,
    Lock = 0x16,
    Unlock = 0x17,
    Test = 0x18,
    Cancel = 0x19,
}

/// <summary>
/// Identifies the data type of entry control event data.
/// </summary>
public enum EntryControlDataType : byte
{
    None = 0x00,
    Raw = 0x01,
    Ascii = 0x02,
    Md5 = 0x03,
}

/// <summary>
/// Represents an entry control notification received from a device.
/// </summary>
public readonly struct EntryControlNotification
{
    public EntryControlNotification(
        byte sequenceNumber,
        EntryControlDataType dataType,
        EntryControlEventType eventType,
        ReadOnlyMemory<byte> eventData)
    {
        SequenceNumber = sequenceNumber;
        DataType = dataType;
        EventType = eventType;
        EventData = eventData;
    }

    /// <summary>
    /// Gets the sequence number of the notification.
    /// </summary>
    public byte SequenceNumber { get; }

    /// <summary>
    /// Gets the data type of the event data.
    /// </summary>
    public EntryControlDataType DataType { get; }

    /// <summary>
    /// Gets the type of entry control event.
    /// </summary>
    public EntryControlEventType EventType { get; }

    /// <summary>
    /// Gets the event data.
    /// </summary>
    public ReadOnlyMemory<byte> EventData { get; }
}

[CommandClass(CommandClassId.EntryControl)]
public sealed class EntryControlCommandClass : CommandClass<EntryControlCommand>
{
    internal EntryControlCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last received entry control notification.
    /// </summary>
    public EntryControlNotification? LastNotification { get; private set; }

    /// <summary>
    /// Gets the supported keys.
    /// </summary>
    public IReadOnlySet<byte>? SupportedKeys { get; private set; }

    /// <summary>
    /// Gets the supported event types.
    /// </summary>
    public IReadOnlySet<EntryControlEventType>? SupportedEventTypes { get; private set; }

    /// <summary>
    /// Gets the supported data types.
    /// </summary>
    public IReadOnlySet<EntryControlDataType>? SupportedDataTypes { get; private set; }

    /// <summary>
    /// Gets the key cache size.
    /// </summary>
    public byte? KeyCacheSize { get; private set; }

    /// <summary>
    /// Gets the key cache timeout.
    /// </summary>
    public byte? KeyCacheTimeout { get; private set; }

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

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetKeySupportedAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetEventSupportedAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported keys from a device.
    /// </summary>
    public async Task<IReadOnlySet<byte>> GetKeySupportedAsync(CancellationToken cancellationToken)
    {
        EntryControlKeySupportedGetCommand command = EntryControlKeySupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<EntryControlKeySupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedKeys!;
    }

    /// <summary>
    /// Request the supported event types and data types from a device.
    /// </summary>
    public async Task<IReadOnlySet<EntryControlEventType>> GetEventSupportedAsync(CancellationToken cancellationToken)
    {
        EntryControlEventSupportedGetCommand command = EntryControlEventSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<EntryControlEventSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedEventTypes!;
    }

    /// <summary>
    /// Set the entry control configuration at the receiving node.
    /// </summary>
    public async Task SetConfigurationAsync(byte keyCacheSize, byte keyCacheTimeout, CancellationToken cancellationToken)
    {
        EntryControlConfigurationSetCommand command = EntryControlConfigurationSetCommand.Create(keyCacheSize, keyCacheTimeout);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the entry control configuration from a node.
    /// </summary>
    public async Task<(byte KeyCacheSize, byte KeyCacheTimeout)> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        EntryControlConfigurationGetCommand command = EntryControlConfigurationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<EntryControlConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        return (KeyCacheSize!.Value, KeyCacheTimeout!.Value);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((EntryControlCommand)frame.CommandId)
        {
            case EntryControlCommand.KeySupportedGet:
            case EntryControlCommand.EventSupportedGet:
            case EntryControlCommand.ConfigurationSet:
            case EntryControlCommand.ConfigurationGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case EntryControlCommand.Notification:
            {
                EntryControlNotificationCommand command = new EntryControlNotificationCommand(frame);
                LastNotification = new EntryControlNotification(
                    command.SequenceNumber,
                    command.DataType,
                    command.EventType,
                    command.EventData);
                break;
            }
            case EntryControlCommand.KeySupportedReport:
            {
                EntryControlKeySupportedReportCommand command = new EntryControlKeySupportedReportCommand(frame);
                SupportedKeys = command.SupportedKeys;
                break;
            }
            case EntryControlCommand.EventSupportedReport:
            {
                EntryControlEventSupportedReportCommand command = new EntryControlEventSupportedReportCommand(frame);
                SupportedDataTypes = command.SupportedDataTypes;
                SupportedEventTypes = command.SupportedEventTypes;
                KeyCacheSize = command.MinKeyCacheSize;
                KeyCacheTimeout = command.MinKeyCacheTimeout;
                break;
            }
            case EntryControlCommand.ConfigurationReport:
            {
                EntryControlConfigurationReportCommand command = new EntryControlConfigurationReportCommand(frame);
                KeyCacheSize = command.KeyCacheSize;
                KeyCacheTimeout = command.KeyCacheTimeout;
                break;
            }
        }
    }

    private readonly struct EntryControlNotificationCommand : ICommand
    {
        public EntryControlNotificationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.Notification;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The sequence number of the notification.
        /// </summary>
        public byte SequenceNumber => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The data type of the event data.
        /// </summary>
        public EntryControlDataType DataType => (EntryControlDataType)(Frame.CommandParameters.Span[1] & 0x03);

        /// <summary>
        /// The type of entry control event.
        /// </summary>
        public EntryControlEventType EventType => (EntryControlEventType)Frame.CommandParameters.Span[2];

        /// <summary>
        /// The length of the event data.
        /// </summary>
        public byte EventDataLength => Frame.CommandParameters.Span[3];

        /// <summary>
        /// The event data.
        /// </summary>
        public ReadOnlyMemory<byte> EventData => Frame.CommandParameters.Slice(4, EventDataLength);
    }

    private readonly struct EntryControlKeySupportedGetCommand : ICommand
    {
        public EntryControlKeySupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.KeySupportedGet;

        public CommandClassFrame Frame { get; }

        public static EntryControlKeySupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new EntryControlKeySupportedGetCommand(frame);
        }
    }

    private readonly struct EntryControlKeySupportedReportCommand : ICommand
    {
        public EntryControlKeySupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.KeySupportedReport;

        public CommandClassFrame Frame { get; }

        public IReadOnlySet<byte> SupportedKeys
        {
            get
            {
                HashSet<byte> supportedKeys = new HashSet<byte>();

                int bitMaskLength = Frame.CommandParameters.Span[0];
                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(1, bitMaskLength);
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            byte key = (byte)((byteNum << 3) + bitNum);
                            supportedKeys.Add(key);
                        }
                    }
                }

                return supportedKeys;
            }
        }
    }

    private readonly struct EntryControlEventSupportedGetCommand : ICommand
    {
        public EntryControlEventSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.EventSupportedGet;

        public CommandClassFrame Frame { get; }

        public static EntryControlEventSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new EntryControlEventSupportedGetCommand(frame);
        }
    }

    private readonly struct EntryControlEventSupportedReportCommand : ICommand
    {
        public EntryControlEventSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.EventSupportedReport;

        public CommandClassFrame Frame { get; }

        private int DataTypeBitMaskLength => Frame.CommandParameters.Span[0] & 0x03;

        private int EventTypeBitMaskLength => (Frame.CommandParameters.Span[0] >> 2) & 0x1F;

        public IReadOnlySet<EntryControlDataType> SupportedDataTypes
        {
            get
            {
                HashSet<EntryControlDataType> supportedDataTypes = new HashSet<EntryControlDataType>();

                int length = DataTypeBitMaskLength;
                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(1, length);
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            EntryControlDataType dataType = (EntryControlDataType)((byteNum << 3) + bitNum);
                            supportedDataTypes.Add(dataType);
                        }
                    }
                }

                return supportedDataTypes;
            }
        }

        public IReadOnlySet<EntryControlEventType> SupportedEventTypes
        {
            get
            {
                HashSet<EntryControlEventType> supportedEventTypes = new HashSet<EntryControlEventType>();

                int dataTypeLength = DataTypeBitMaskLength;
                int eventTypeLength = EventTypeBitMaskLength;
                int offset = 1 + dataTypeLength;
                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(offset, eventTypeLength);
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            EntryControlEventType eventType = (EntryControlEventType)((byteNum << 3) + bitNum);
                            supportedEventTypes.Add(eventType);
                        }
                    }
                }

                return supportedEventTypes;
            }
        }

        private int CacheSizeOffset => 1 + DataTypeBitMaskLength + EventTypeBitMaskLength;

        public byte MinKeyCacheSize => Frame.CommandParameters.Span[CacheSizeOffset];

        public byte MaxKeyCacheSize => Frame.CommandParameters.Span[CacheSizeOffset + 1];

        public byte MinKeyCacheTimeout => Frame.CommandParameters.Span[CacheSizeOffset + 2];

        public byte MaxKeyCacheTimeout => Frame.CommandParameters.Span[CacheSizeOffset + 3];
    }

    private readonly struct EntryControlConfigurationSetCommand : ICommand
    {
        public EntryControlConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.ConfigurationSet;

        public CommandClassFrame Frame { get; }

        public static EntryControlConfigurationSetCommand Create(byte keyCacheSize, byte keyCacheTimeout)
        {
            ReadOnlySpan<byte> commandParameters = [keyCacheSize, keyCacheTimeout];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new EntryControlConfigurationSetCommand(frame);
        }
    }

    private readonly struct EntryControlConfigurationGetCommand : ICommand
    {
        public EntryControlConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.ConfigurationGet;

        public CommandClassFrame Frame { get; }

        public static EntryControlConfigurationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new EntryControlConfigurationGetCommand(frame);
        }
    }

    private readonly struct EntryControlConfigurationReportCommand : ICommand
    {
        public EntryControlConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.ConfigurationReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The key cache size.
        /// </summary>
        public byte KeyCacheSize => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The key cache timeout.
        /// </summary>
        public byte KeyCacheTimeout => Frame.CommandParameters.Span[1];
    }
}
