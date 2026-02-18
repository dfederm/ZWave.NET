namespace ZWave.CommandClasses;

/// <summary>
/// Identifies a key attribute for a central scene notification.
/// </summary>
public enum CentralSceneKeyAttribute : byte
{
    KeyPressed1x = 0x00,
    KeyReleased = 0x01,
    KeyHeldDown = 0x02,
    KeyPressed2x = 0x03,
    KeyPressed3x = 0x04,
    KeyPressed4x = 0x05,
    KeyPressed5x = 0x06,
}

public enum CentralSceneCommand : byte
{
    /// <summary>
    /// Request the supported scenes and key attributes from a device.
    /// </summary>
    SupportedGet = 0x01,

    /// <summary>
    /// Advertise the supported scenes and key attributes.
    /// </summary>
    SupportedReport = 0x02,

    /// <summary>
    /// Advertise a scene activation from a device.
    /// </summary>
    Notification = 0x03,

    /// <summary>
    /// Configure the slow refresh setting.
    /// </summary>
    ConfigurationSet = 0x04,

    /// <summary>
    /// Request the slow refresh configuration.
    /// </summary>
    ConfigurationGet = 0x05,

    /// <summary>
    /// Advertise the slow refresh configuration.
    /// </summary>
    ConfigurationReport = 0x06,
}

/// <summary>
/// Represents a central scene notification received from a device.
/// </summary>
public readonly struct CentralSceneNotification
{
    public CentralSceneNotification(
        byte sequenceNumber,
        CentralSceneKeyAttribute keyAttribute,
        byte sceneNumber,
        bool slowRefresh)
    {
        SequenceNumber = sequenceNumber;
        KeyAttribute = keyAttribute;
        SceneNumber = sceneNumber;
        SlowRefresh = slowRefresh;
    }

    /// <summary>
    /// Gets the sequence number of the notification.
    /// </summary>
    public byte SequenceNumber { get; }

    /// <summary>
    /// Gets the key attribute indicating the type of key event.
    /// </summary>
    public CentralSceneKeyAttribute KeyAttribute { get; }

    /// <summary>
    /// Gets the scene number that was activated.
    /// </summary>
    public byte SceneNumber { get; }

    /// <summary>
    /// Gets whether the node is using slow refresh.
    /// </summary>
    public bool SlowRefresh { get; }
}

/// <summary>
/// Represents the supported scenes and key attributes of a central scene device.
/// </summary>
public readonly struct CentralSceneSupportedReport
{
    public CentralSceneSupportedReport(
        byte supportedScenes,
        bool identical,
        bool supportsSlowRefresh,
        IReadOnlyList<IReadOnlySet<CentralSceneKeyAttribute>> supportedKeyAttributes)
    {
        SupportedScenes = supportedScenes;
        Identical = identical;
        SupportsSlowRefresh = supportsSlowRefresh;
        SupportedKeyAttributes = supportedKeyAttributes;
    }

    /// <summary>
    /// Gets the number of scenes supported by the device.
    /// </summary>
    public byte SupportedScenes { get; }

    /// <summary>
    /// Gets whether all scenes support the same key attributes.
    /// </summary>
    public bool Identical { get; }

    /// <summary>
    /// Gets whether the device supports the slow refresh capability.
    /// </summary>
    public bool SupportsSlowRefresh { get; }

    /// <summary>
    /// Gets the supported key attributes for each scene.
    /// </summary>
    public IReadOnlyList<IReadOnlySet<CentralSceneKeyAttribute>> SupportedKeyAttributes { get; }
}

[CommandClass(CommandClassId.CentralScene)]
public sealed class CentralSceneCommandClass : CommandClass<CentralSceneCommand>
{
    internal CentralSceneCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last received central scene notification.
    /// </summary>
    public CentralSceneNotification? LastNotification { get; private set; }

    /// <summary>
    /// Gets the supported scenes report.
    /// </summary>
    public CentralSceneSupportedReport? SupportedReport { get; private set; }

    /// <summary>
    /// Gets the current slow refresh configuration.
    /// </summary>
    public bool? SlowRefresh { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(CentralSceneCommand command)
        => command switch
        {
            CentralSceneCommand.SupportedGet => true,
            CentralSceneCommand.ConfigurationSet => Version.HasValue ? Version >= 3 : null,
            CentralSceneCommand.ConfigurationGet => Version.HasValue ? Version >= 3 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(CentralSceneCommand.ConfigurationGet).GetValueOrDefault(false))
        {
            _ = await GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request the supported scenes and key attributes from a device.
    /// </summary>
    public async Task<CentralSceneSupportedReport> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = CentralSceneSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<CentralSceneSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedReport!.Value;
    }

    /// <summary>
    /// Request the slow refresh configuration.
    /// </summary>
    public async Task<bool> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var command = CentralSceneConfigurationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<CentralSceneConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        return SlowRefresh!.Value;
    }

    /// <summary>
    /// Configure the slow refresh setting.
    /// </summary>
    public async Task SetConfigurationAsync(bool slowRefresh, CancellationToken cancellationToken)
    {
        var command = CentralSceneConfigurationSetCommand.Create(slowRefresh);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((CentralSceneCommand)frame.CommandId)
        {
            case CentralSceneCommand.SupportedGet:
            case CentralSceneCommand.ConfigurationSet:
            case CentralSceneCommand.ConfigurationGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case CentralSceneCommand.SupportedReport:
            {
                var command = new CentralSceneSupportedReportCommand(frame, EffectiveVersion);
                SupportedReport = new CentralSceneSupportedReport(
                    command.SupportedScenes,
                    command.Identical,
                    command.SupportsSlowRefresh,
                    command.SupportedKeyAttributes);
                break;
            }
            case CentralSceneCommand.Notification:
            {
                var command = new CentralSceneNotificationCommand(frame);
                LastNotification = new CentralSceneNotification(
                    command.SequenceNumber,
                    command.KeyAttribute,
                    command.SceneNumber,
                    command.SlowRefresh);
                break;
            }
            case CentralSceneCommand.ConfigurationReport:
            {
                var command = new CentralSceneConfigurationReportCommand(frame);
                SlowRefresh = command.SlowRefresh;
                break;
            }
        }
    }

    private readonly struct CentralSceneSupportedGetCommand : ICommand
    {
        public CentralSceneSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static CentralSceneSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new CentralSceneSupportedGetCommand(frame);
        }
    }

    private readonly struct CentralSceneSupportedReportCommand : ICommand
    {
        private readonly byte _version;

        public CentralSceneSupportedReportCommand(CommandClassFrame frame, byte version)
        {
            Frame = frame;
            _version = version;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The number of scenes supported by the device.
        /// </summary>
        public byte SupportedScenes => Frame.CommandParameters.Span[0];

        /// <summary>
        /// Whether all scenes support the same key attributes.
        /// </summary>
        public bool Identical => _version >= 2 && Frame.CommandParameters.Length > 1
            && (Frame.CommandParameters.Span[1] & 0b0000_0001) != 0;

        /// <summary>
        /// Whether the device supports slow refresh.
        /// </summary>
        public bool SupportsSlowRefresh => _version >= 3 && Frame.CommandParameters.Length > 1
            && (Frame.CommandParameters.Span[1] & 0b1000_0000) != 0;

        /// <summary>
        /// The supported key attributes for each scene.
        /// </summary>
        public IReadOnlyList<IReadOnlySet<CentralSceneKeyAttribute>> SupportedKeyAttributes
        {
            get
            {
                byte supportedScenes = SupportedScenes;
                bool identical = Identical;

                if (supportedScenes == 0 || Frame.CommandParameters.Length < 2)
                {
                    return [];
                }

                int numBitMaskBytes = _version >= 3
                    ? (Frame.CommandParameters.Span[1] & 0b0000_0110) >> 1
                    : 1;

                if (numBitMaskBytes == 0)
                {
                    return [];
                }

                int bitMaskCount = identical ? 1 : supportedScenes;
                var result = new List<IReadOnlySet<CentralSceneKeyAttribute>>(supportedScenes);

                for (int i = 0; i < bitMaskCount; i++)
                {
                    int offset = 2 + (i * numBitMaskBytes);
                    if (offset + numBitMaskBytes > Frame.CommandParameters.Length)
                    {
                        break;
                    }

                    HashSet<CentralSceneKeyAttribute> keyAttributes = ParseKeyAttributeBitMask(
                        Frame.CommandParameters.Span.Slice(offset, numBitMaskBytes));
                    result.Add(keyAttributes);
                }

                // If identical, replicate the single bitmask for all scenes
                if (identical && result.Count == 1)
                {
                    IReadOnlySet<CentralSceneKeyAttribute> shared = result[0];
                    for (int i = 1; i < supportedScenes; i++)
                    {
                        result.Add(shared);
                    }
                }

                return result;
            }
        }

        private static HashSet<CentralSceneKeyAttribute> ParseKeyAttributeBitMask(ReadOnlySpan<byte> bitMask)
        {
            HashSet<CentralSceneKeyAttribute> keyAttributes = new HashSet<CentralSceneKeyAttribute>();
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        CentralSceneKeyAttribute keyAttribute = (CentralSceneKeyAttribute)((byteNum << 3) + bitNum);
                        keyAttributes.Add(keyAttribute);
                    }
                }
            }

            return keyAttributes;
        }
    }

    private readonly struct CentralSceneNotificationCommand : ICommand
    {
        public CentralSceneNotificationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.Notification;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The sequence number of the notification.
        /// </summary>
        public byte SequenceNumber => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The key attribute indicating the type of key event.
        /// </summary>
        public CentralSceneKeyAttribute KeyAttribute
            => (CentralSceneKeyAttribute)(Frame.CommandParameters.Span[1] & 0b0000_0111);

        /// <summary>
        /// Whether the node is using slow refresh.
        /// </summary>
        public bool SlowRefresh => (Frame.CommandParameters.Span[1] & 0b1000_0000) != 0;

        /// <summary>
        /// The scene number that was activated.
        /// </summary>
        public byte SceneNumber => Frame.CommandParameters.Span[2];
    }

    private readonly struct CentralSceneConfigurationSetCommand : ICommand
    {
        public CentralSceneConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.ConfigurationSet;

        public CommandClassFrame Frame { get; }

        public static CentralSceneConfigurationSetCommand Create(bool slowRefresh)
        {
            ReadOnlySpan<byte> commandParameters = [slowRefresh ? (byte)0b1000_0000 : (byte)0x00];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new CentralSceneConfigurationSetCommand(frame);
        }
    }

    private readonly struct CentralSceneConfigurationGetCommand : ICommand
    {
        public CentralSceneConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.ConfigurationGet;

        public CommandClassFrame Frame { get; }

        public static CentralSceneConfigurationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new CentralSceneConfigurationGetCommand(frame);
        }
    }

    private readonly struct CentralSceneConfigurationReportCommand : ICommand
    {
        public CentralSceneConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.ConfigurationReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Whether slow refresh is enabled.
        /// </summary>
        public bool SlowRefresh => (Frame.CommandParameters.Span[0] & 0b1000_0000) != 0;
    }
}
