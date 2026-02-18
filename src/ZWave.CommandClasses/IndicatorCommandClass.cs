namespace ZWave.CommandClasses;

public enum IndicatorCommand : byte
{
    /// <summary>
    /// Set the indicator value at the receiving node.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current indicator value from a node.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current indicator value at the sending node.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported property IDs for a given indicator ID (V2+).
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Advertise the supported property IDs for a given indicator ID (V2+).
    /// </summary>
    SupportedReport = 0x05,
}

/// <summary>
/// Identifies an indicator on a Z-Wave device (SDS13781).
/// </summary>
public enum IndicatorId : byte
{
    Armed = 0x01,
    NotArmed = 0x02,
    Ready = 0x03,
    Fault = 0x04,
    Busy = 0x05,
    EnterID = 0x06,
    EnterPIN = 0x07,
    CodeAccepted = 0x08,
    CodeNotAccepted = 0x09,
    ArmedStay = 0x0A,
    ArmedAway = 0x0B,
    Alarming = 0x0C,
    AlarmingBurglar = 0x0D,
    AlarmingSmokeFire = 0x0E,
    AlarmingCarbonMonoxide = 0x0F,
    BypassChallenge = 0x10,
    EntryDelay = 0x11,
    ExitDelay = 0x12,
    AlarmingMedical = 0x13,
    AlarmingFreezeWarning = 0x14,
    AlarmingWaterLeak = 0x15,
    AlarmingPanic = 0x16,
    Zone1Armed = 0x20,
    Zone2Armed = 0x21,
    Zone3Armed = 0x22,
    Zone4Armed = 0x23,
    Zone5Armed = 0x24,
    Zone6Armed = 0x25,
    Zone7Armed = 0x26,
    Zone8Armed = 0x27,
    LcdBacklight = 0x30,
    ButtonBacklightLetters = 0x40,
    ButtonBacklightDigits = 0x41,
    ButtonBacklightCommand = 0x42,
    Button1Indication = 0x43,
    Button2Indication = 0x44,
    Button3Indication = 0x45,
    Button4Indication = 0x46,
    Button5Indication = 0x47,
    Button6Indication = 0x48,
    Button7Indication = 0x49,
    Button8Indication = 0x4A,
    Button9Indication = 0x4B,
    Button10Indication = 0x4C,
    Button11Indication = 0x4D,
    Button12Indication = 0x4E,
    NodeIdentify = 0x50,
    GenericEventSoundNotification1 = 0x60,
    GenericEventSoundNotification2 = 0x61,
    GenericEventSoundNotification3 = 0x62,
    GenericEventSoundNotification4 = 0x63,
    GenericEventSoundNotification5 = 0x64,
    GenericEventSoundNotification6 = 0x65,
    GenericEventSoundNotification7 = 0x66,
    GenericEventSoundNotification8 = 0x67,
    GenericEventSoundNotification9 = 0x68,
    GenericEventSoundNotification10 = 0x69,
    GenericEventSoundNotification11 = 0x6A,
    GenericEventSoundNotification12 = 0x6B,
    GenericEventSoundNotification13 = 0x6C,
    GenericEventSoundNotification14 = 0x6D,
    GenericEventSoundNotification15 = 0x6E,
    GenericEventSoundNotification16 = 0x6F,
    GenericEventSoundNotification17 = 0x70,
    GenericEventSoundNotification18 = 0x71,
    GenericEventSoundNotification19 = 0x72,
    GenericEventSoundNotification20 = 0x73,
    GenericEventSoundNotification21 = 0x74,
    GenericEventSoundNotification22 = 0x75,
    GenericEventSoundNotification23 = 0x76,
    GenericEventSoundNotification24 = 0x77,
    GenericEventSoundNotification25 = 0x78,
    GenericEventSoundNotification26 = 0x79,
    GenericEventSoundNotification27 = 0x7A,
    GenericEventSoundNotification28 = 0x7B,
    GenericEventSoundNotification29 = 0x7C,
    GenericEventSoundNotification30 = 0x7D,
    GenericEventSoundNotification31 = 0x7E,
    GenericEventSoundNotification32 = 0x7F,
    Buzzer = 0xF0,
}

/// <summary>
/// Identifies an indicator property (SDS13781).
/// </summary>
public enum IndicatorPropertyId : byte
{
    /// <summary>
    /// Multilevel value (0-100).
    /// </summary>
    Multilevel = 0x01,

    /// <summary>
    /// Binary on/off value.
    /// </summary>
    Binary = 0x02,

    /// <summary>
    /// On/off period duration in 1/10th seconds.
    /// </summary>
    OnOffPeriodDuration = 0x03,

    /// <summary>
    /// Number of on/off cycles. 0xFF means infinite.
    /// </summary>
    OnOffCycleCount = 0x04,

    /// <summary>
    /// On time during an on/off period. 0x00 means symmetric (equal on and off time).
    /// </summary>
    OnOffPeriodOnTime = 0x05,

    /// <summary>
    /// Timeout in minutes.
    /// </summary>
    TimeoutMinutes = 0x06,

    /// <summary>
    /// Timeout in seconds.
    /// </summary>
    TimeoutSeconds = 0x07,

    /// <summary>
    /// Timeout in 1/100th seconds.
    /// </summary>
    TimeoutHundredths = 0x08,

    /// <summary>
    /// Sound level (0-100). 0 means off/mute.
    /// </summary>
    SoundLevel = 0x09,

    /// <summary>
    /// Timeout in hours.
    /// </summary>
    TimeoutHours = 0x0A,

    /// <summary>
    /// Whether the indicator can continue working in sleep mode (read-only).
    /// </summary>
    LowPower = 0x10,
}

/// <summary>
/// Represents a single indicator object in a V2+ Indicator Set/Get/Report.
/// Each object specifies a property value for a particular indicator.
/// </summary>
public readonly record struct IndicatorObject(
    IndicatorId IndicatorId,
    IndicatorPropertyId PropertyId,
    byte Value);

[CommandClass(CommandClassId.Indicator)]
public sealed class IndicatorCommandClass : CommandClass<IndicatorCommand>
{
    private Dictionary<IndicatorId, IReadOnlySet<IndicatorPropertyId>>? _supportedIndicators;
    private IndicatorId _lastNextIndicatorId;

    internal IndicatorCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported V1 indicator value (0x00=off, 0x01-0x63=percentage, 0xFF=on).
    /// </summary>
    public byte? IndicatorValue { get; private set; }

    /// <summary>
    /// Gets the last reported V2+ indicator objects.
    /// </summary>
    public IReadOnlyList<IndicatorObject>? IndicatorObjects { get; private set; }

    /// <summary>
    /// Gets the supported indicators and their supported property IDs (V2+).
    /// Populated by <see cref="GetSupportedAsync"/>.
    /// </summary>
    public IReadOnlyDictionary<IndicatorId, IReadOnlySet<IndicatorPropertyId>>? SupportedIndicators => _supportedIndicators;

    /// <inheritdoc />
    public override bool? IsCommandSupported(IndicatorCommand command)
        => command switch
        {
            IndicatorCommand.Set => true,
            IndicatorCommand.Get => true,
            IndicatorCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(IndicatorCommand.SupportedGet).GetValueOrDefault(false))
        {
            // Discover all supported indicators and their properties
            IndicatorId nextIndicatorId = 0;
            do
            {
                _ = await GetSupportedAsync(nextIndicatorId, cancellationToken).ConfigureAwait(false);
                nextIndicatorId = _lastNextIndicatorId;
            }
            while (nextIndicatorId != 0);
        }
        else
        {
            _ = await GetAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request the current V1 indicator value from a node.
    /// </summary>
    public async Task<byte> GetAsync(CancellationToken cancellationToken)
    {
        IndicatorGetCommand command = IndicatorGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<IndicatorReportCommand>(cancellationToken).ConfigureAwait(false);
        return IndicatorValue!.Value;
    }

    /// <summary>
    /// Request the current indicator property values for a specific indicator (V2+).
    /// </summary>
    public async Task<IReadOnlyList<IndicatorObject>> GetAsync(IndicatorId indicatorId, CancellationToken cancellationToken)
    {
        IndicatorGetCommand command = IndicatorGetCommand.Create(indicatorId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<IndicatorReportCommand>(cancellationToken).ConfigureAwait(false);
        return IndicatorObjects ?? [];
    }

    /// <summary>
    /// Set the V1 indicator value at the receiving node.
    /// </summary>
    public async Task SetAsync(byte value, CancellationToken cancellationToken)
    {
        IndicatorSetCommand command = IndicatorSetCommand.Create(value);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Set indicator property values using V2+ indicator objects (up to 31).
    /// </summary>
    public Task SetAsync(ReadOnlySpan<IndicatorObject> objects, CancellationToken cancellationToken)
    {
        IndicatorSetCommand command = IndicatorSetCommand.Create(objects);
        return SendCommandAsync(command, cancellationToken);
    }

    /// <summary>
    /// Request the supported property IDs for the specified indicator (V2+).
    /// Pass <see cref="IndicatorId"/> value 0 to start enumeration from the first supported indicator.
    /// </summary>
    /// <returns>The supported property IDs for the queried indicator.</returns>
    public async Task<IReadOnlySet<IndicatorPropertyId>> GetSupportedAsync(IndicatorId indicatorId, CancellationToken cancellationToken)
    {
        IndicatorSupportedGetCommand command = IndicatorSupportedGetCommand.Create(indicatorId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<IndicatorSupportedReportCommand>(cancellationToken).ConfigureAwait(false);

        // Return the property IDs that were just stored for the reported indicator
        IndicatorId reportedId = _lastReportedIndicatorId;
        if (_supportedIndicators != null
            && reportedId != 0
            && _supportedIndicators.TryGetValue(reportedId, out IReadOnlySet<IndicatorPropertyId>? properties))
        {
            return properties;
        }

        return new HashSet<IndicatorPropertyId>();
    }

    private IndicatorId _lastReportedIndicatorId;

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((IndicatorCommand)frame.CommandId)
        {
            case IndicatorCommand.Set:
            case IndicatorCommand.Get:
            case IndicatorCommand.SupportedGet:
            {
                // We don't expect to receive these commands
                break;
            }
            case IndicatorCommand.Report:
            {
                var command = new IndicatorReportCommand(frame);
                IndicatorValue = command.Indicator0Value;

                IReadOnlyList<IndicatorObject> objects = command.Objects;
                if (objects.Count > 0)
                {
                    IndicatorObjects = objects;
                }

                break;
            }
            case IndicatorCommand.SupportedReport:
            {
                var command = new IndicatorSupportedReportCommand(frame);
                IndicatorId indicatorId = command.IndicatorId;
                _lastNextIndicatorId = command.NextIndicatorId;
                _lastReportedIndicatorId = indicatorId;

                if (indicatorId != 0)
                {
                    _supportedIndicators ??= new Dictionary<IndicatorId, IReadOnlySet<IndicatorPropertyId>>();
                    _supportedIndicators[indicatorId] = command.SupportedPropertyIds;
                }

                break;
            }
        }
    }

    private readonly struct IndicatorSetCommand : ICommand
    {
        public IndicatorSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.Set;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Create a V1 Set command with a single indicator value.
        /// </summary>
        public static IndicatorSetCommand Create(byte value)
        {
            ReadOnlySpan<byte> commandParameters = [value];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorSetCommand(frame);
        }

        /// <summary>
        /// Create a V2+ Set command with indicator objects.
        /// </summary>
        public static IndicatorSetCommand Create(ReadOnlySpan<IndicatorObject> objects)
        {
            int objectCount = Math.Min(objects.Length, 31);
            Span<byte> commandParameters = stackalloc byte[2 + (objectCount * 3)];
            commandParameters[0] = 0x00; // Indicator 0 Value (unused in V2 mode)
            commandParameters[1] = (byte)(objectCount & 0b0001_1111);
            for (int i = 0; i < objectCount; i++)
            {
                int offset = 2 + (i * 3);
                commandParameters[offset] = (byte)objects[i].IndicatorId;
                commandParameters[offset + 1] = (byte)objects[i].PropertyId;
                commandParameters[offset + 2] = objects[i].Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorSetCommand(frame);
        }
    }

    private readonly struct IndicatorGetCommand : ICommand
    {
        public IndicatorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.Get;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Create a V1 Get command (no indicator ID).
        /// </summary>
        public static IndicatorGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new IndicatorGetCommand(frame);
        }

        /// <summary>
        /// Create a V2+ Get command for a specific indicator.
        /// </summary>
        public static IndicatorGetCommand Create(IndicatorId indicatorId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)indicatorId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorGetCommand(frame);
        }
    }

    private readonly struct IndicatorReportCommand : ICommand
    {
        public IndicatorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The V1 indicator value (0x00=off, 0x01-0x63=percentage, 0xFF=on).
        /// Always present as the first byte.
        /// </summary>
        public byte Indicator0Value => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The V2+ indicator objects. Empty if this is a V1-only report.
        /// </summary>
        public IReadOnlyList<IndicatorObject> Objects
        {
            get
            {
                if (Frame.CommandParameters.Length < 2)
                {
                    return [];
                }

                int objectCount = Frame.CommandParameters.Span[1] & 0b0001_1111;
                if (objectCount == 0)
                {
                    return [];
                }

                List<IndicatorObject> objects = new(objectCount);
                for (int i = 0; i < objectCount; i++)
                {
                    int offset = 2 + (i * 3);
                    if (offset + 2 >= Frame.CommandParameters.Length)
                    {
                        break;
                    }

                    IndicatorId indicatorId = (IndicatorId)Frame.CommandParameters.Span[offset];
                    IndicatorPropertyId propertyId = (IndicatorPropertyId)Frame.CommandParameters.Span[offset + 1];
                    byte value = Frame.CommandParameters.Span[offset + 2];
                    objects.Add(new IndicatorObject(indicatorId, propertyId, value));
                }

                return objects;
            }
        }
    }

    private readonly struct IndicatorSupportedGetCommand : ICommand
    {
        public IndicatorSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static IndicatorSupportedGetCommand Create(IndicatorId indicatorId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)indicatorId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorSupportedGetCommand(frame);
        }
    }

    private readonly struct IndicatorSupportedReportCommand : ICommand
    {
        public IndicatorSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The indicator ID this report describes.
        /// </summary>
        public IndicatorId IndicatorId => (IndicatorId)Frame.CommandParameters.Span[0];

        /// <summary>
        /// The next indicator ID to query, or 0 if no more.
        /// </summary>
        public IndicatorId NextIndicatorId => (IndicatorId)Frame.CommandParameters.Span[1];

        /// <summary>
        /// The supported property IDs for this indicator, parsed from the bitmask.
        /// </summary>
        public IReadOnlySet<IndicatorPropertyId> SupportedPropertyIds
        {
            get
            {
                HashSet<IndicatorPropertyId> supportedPropertyIds = new();

                int numBitMasks = Frame.CommandParameters.Span[2] & 0b0001_1111;
                for (int byteNum = 0; byteNum < numBitMasks && (3 + byteNum) < Frame.CommandParameters.Length; byteNum++)
                {
                    byte mask = Frame.CommandParameters.Span[3 + byteNum];
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((mask & (1 << bitNum)) != 0)
                        {
                            IndicatorPropertyId propertyId = (IndicatorPropertyId)((byteNum * 8) + bitNum);
                            supportedPropertyIds.Add(propertyId);
                        }
                    }
                }

                return supportedPropertyIds;
            }
        }
    }
}
