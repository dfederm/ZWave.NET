namespace ZWave.CommandClasses;

public enum DoorLockCommand : byte
{
    /// <summary>
    /// Set the door lock mode at the receiving node.
    /// </summary>
    OperationSet = 0x01,

    /// <summary>
    /// Request the door lock operation state from a node.
    /// </summary>
    OperationGet = 0x02,

    /// <summary>
    /// Advertise the door lock operation state at the sending node.
    /// </summary>
    OperationReport = 0x03,

    /// <summary>
    /// Set the door lock configuration at the receiving node.
    /// </summary>
    ConfigurationSet = 0x04,

    /// <summary>
    /// Request the door lock configuration from a node.
    /// </summary>
    ConfigurationGet = 0x05,

    /// <summary>
    /// Advertise the door lock configuration at the sending node.
    /// </summary>
    ConfigurationReport = 0x06,

    /// <summary>
    /// Request the door lock capabilities from a node (v4+).
    /// </summary>
    CapabilitiesGet = 0x07,

    /// <summary>
    /// Advertise the door lock capabilities at the sending node (v4+).
    /// </summary>
    CapabilitiesReport = 0x08,
}

/// <summary>
/// Defines the lock mode of a door lock device.
/// </summary>
public enum DoorLockMode : byte
{
    Unsecured = 0x00,

    UnsecuredWithTimeout = 0x01,

    UnsecuredInsideDoorHandles = 0x10,

    UnsecuredInsideDoorHandlesWithTimeout = 0x11,

    UnsecuredOutsideDoorHandles = 0x20,

    UnsecuredOutsideDoorHandlesWithTimeout = 0x21,

    Unknown = 0xFE,

    Secured = 0xFF,
}

/// <summary>
/// Defines the operation type of a door lock device.
/// </summary>
public enum DoorLockOperationType : byte
{
    /// <summary>
    /// Constant operation (no timeout).
    /// </summary>
    Constant = 0x01,

    /// <summary>
    /// Timed operation (lock reverts after timeout).
    /// </summary>
    Timed = 0x02,
}

/// <summary>
/// Represents the operation state reported by a Door Lock Command Class device.
/// </summary>
public readonly struct DoorLockOperationState
{
    public DoorLockOperationState(
        DoorLockMode mode,
        byte outsideDoorHandlesMode,
        byte insideDoorHandlesMode,
        byte doorCondition,
        byte? lockTimeoutMinutes,
        byte? lockTimeoutSeconds,
        DoorLockMode? targetMode,
        byte? duration)
    {
        Mode = mode;
        OutsideDoorHandlesMode = outsideDoorHandlesMode;
        InsideDoorHandlesMode = insideDoorHandlesMode;
        DoorCondition = doorCondition;
        LockTimeoutMinutes = lockTimeoutMinutes;
        LockTimeoutSeconds = lockTimeoutSeconds;
        TargetMode = targetMode;
        Duration = duration;
    }

    /// <summary>
    /// The current door lock mode.
    /// </summary>
    public DoorLockMode Mode { get; }

    /// <summary>
    /// The outside door handles mode (upper nibble).
    /// </summary>
    public byte OutsideDoorHandlesMode { get; }

    /// <summary>
    /// The inside door handles mode (lower nibble).
    /// </summary>
    public byte InsideDoorHandlesMode { get; }

    /// <summary>
    /// The door condition bitmask.
    /// </summary>
    public byte DoorCondition { get; }

    /// <summary>
    /// The lock timeout in minutes (v2+).
    /// </summary>
    public byte? LockTimeoutMinutes { get; }

    /// <summary>
    /// The lock timeout in seconds (v2+).
    /// </summary>
    public byte? LockTimeoutSeconds { get; }

    /// <summary>
    /// The target door lock mode of an ongoing transition (v3+).
    /// </summary>
    public DoorLockMode? TargetMode { get; }

    /// <summary>
    /// The duration of an ongoing transition (v3+).
    /// </summary>
    public byte? Duration { get; }
}

/// <summary>
/// Represents the configuration state reported by a Door Lock Command Class device.
/// </summary>
public readonly struct DoorLockConfigurationState
{
    public DoorLockConfigurationState(
        DoorLockOperationType operationType,
        byte outsideHandlesState,
        byte insideHandlesState,
        byte lockTimeoutMinutes,
        byte lockTimeoutSeconds)
    {
        OperationType = operationType;
        OutsideHandlesState = outsideHandlesState;
        InsideHandlesState = insideHandlesState;
        LockTimeoutMinutes = lockTimeoutMinutes;
        LockTimeoutSeconds = lockTimeoutSeconds;
    }

    /// <summary>
    /// The operation type.
    /// </summary>
    public DoorLockOperationType OperationType { get; }

    /// <summary>
    /// The outside door handles state (upper nibble).
    /// </summary>
    public byte OutsideHandlesState { get; }

    /// <summary>
    /// The inside door handles state (lower nibble).
    /// </summary>
    public byte InsideHandlesState { get; }

    /// <summary>
    /// The lock timeout in minutes.
    /// </summary>
    public byte LockTimeoutMinutes { get; }

    /// <summary>
    /// The lock timeout in seconds.
    /// </summary>
    public byte LockTimeoutSeconds { get; }
}

/// <summary>
/// Represents the capabilities reported by a Door Lock Command Class device (v4+).
/// </summary>
public readonly struct DoorLockCapabilities
{
    public DoorLockCapabilities(
        IReadOnlySet<DoorLockOperationType> supportedOperationTypes,
        IReadOnlySet<DoorLockMode> supportedModes,
        byte supportedOutsideHandles,
        byte supportedInsideHandles)
    {
        SupportedOperationTypes = supportedOperationTypes;
        SupportedModes = supportedModes;
        SupportedOutsideHandles = supportedOutsideHandles;
        SupportedInsideHandles = supportedInsideHandles;
    }

    /// <summary>
    /// The set of supported door lock operation types.
    /// </summary>
    public IReadOnlySet<DoorLockOperationType> SupportedOperationTypes { get; }

    /// <summary>
    /// The set of supported door lock modes.
    /// </summary>
    public IReadOnlySet<DoorLockMode> SupportedModes { get; }

    /// <summary>
    /// The supported outside handles bitmask (4 bits).
    /// </summary>
    public byte SupportedOutsideHandles { get; }

    /// <summary>
    /// The supported inside handles bitmask (4 bits).
    /// </summary>
    public byte SupportedInsideHandles { get; }
}

[CommandClass(CommandClassId.DoorLock)]
public sealed class DoorLockCommandClass : CommandClass<DoorLockCommand>
{
    internal DoorLockCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported door lock operation state.
    /// </summary>
    public DoorLockOperationState? OperationState { get; private set; }

    /// <summary>
    /// Gets the last reported door lock configuration state.
    /// </summary>
    public DoorLockConfigurationState? ConfigurationState { get; private set; }

    /// <summary>
    /// Gets the last reported door lock capabilities.
    /// </summary>
    public DoorLockCapabilities? Capabilities { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(DoorLockCommand command)
        => command switch
        {
            DoorLockCommand.OperationSet => true,
            DoorLockCommand.OperationGet => true,
            DoorLockCommand.ConfigurationSet => true,
            DoorLockCommand.ConfigurationGet => true,
            DoorLockCommand.CapabilitiesGet => Version.HasValue ? Version >= 4 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetOperationAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Set the door lock mode at the receiving node.
    /// </summary>
    public async Task SetOperationAsync(DoorLockMode mode, CancellationToken cancellationToken)
    {
        var command = DoorLockOperationSetCommand.Create(mode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the door lock operation state from a node.
    /// </summary>
    public async Task<DoorLockOperationState> GetOperationAsync(CancellationToken cancellationToken)
    {
        var command = DoorLockOperationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<DoorLockOperationReportCommand>(cancellationToken).ConfigureAwait(false);
        return OperationState!.Value;
    }

    /// <summary>
    /// Set the door lock configuration at the receiving node.
    /// </summary>
    public async Task SetConfigurationAsync(
        DoorLockOperationType operationType,
        byte outsideHandlesState,
        byte insideHandlesState,
        byte lockTimeoutMinutes,
        byte lockTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var command = DoorLockConfigurationSetCommand.Create(
            operationType,
            outsideHandlesState,
            insideHandlesState,
            lockTimeoutMinutes,
            lockTimeoutSeconds);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the door lock configuration from a node.
    /// </summary>
    public async Task<DoorLockConfigurationState> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var command = DoorLockConfigurationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<DoorLockConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        return ConfigurationState!.Value;
    }

    /// <summary>
    /// Request the door lock capabilities from a node (v4+).
    /// </summary>
    public async Task<DoorLockCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var command = DoorLockCapabilitiesGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<DoorLockCapabilitiesReportCommand>(cancellationToken).ConfigureAwait(false);
        return Capabilities!.Value;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((DoorLockCommand)frame.CommandId)
        {
            case DoorLockCommand.OperationSet:
            case DoorLockCommand.OperationGet:
            case DoorLockCommand.ConfigurationSet:
            case DoorLockCommand.ConfigurationGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case DoorLockCommand.OperationReport:
            {
                var command = new DoorLockOperationReportCommand(frame, EffectiveVersion);
                OperationState = new DoorLockOperationState(
                    command.Mode,
                    command.OutsideDoorHandlesMode,
                    command.InsideDoorHandlesMode,
                    command.DoorCondition,
                    command.LockTimeoutMinutes,
                    command.LockTimeoutSeconds,
                    command.TargetMode,
                    command.Duration);
                break;
            }
            case DoorLockCommand.ConfigurationReport:
            {
                var command = new DoorLockConfigurationReportCommand(frame);
                ConfigurationState = new DoorLockConfigurationState(
                    command.OperationType,
                    command.OutsideHandlesState,
                    command.InsideHandlesState,
                    command.LockTimeoutMinutes,
                    command.LockTimeoutSeconds);
                break;
            }
            case DoorLockCommand.CapabilitiesReport:
            {
                var command = new DoorLockCapabilitiesReportCommand(frame);
                Capabilities = new DoorLockCapabilities(
                    command.SupportedOperationTypes,
                    command.SupportedModes,
                    command.SupportedOutsideHandles,
                    command.SupportedInsideHandles);
                break;
            }
        }
    }

    private readonly struct DoorLockOperationSetCommand : ICommand
    {
        public DoorLockOperationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.OperationSet;

        public CommandClassFrame Frame { get; }

        public static DoorLockOperationSetCommand Create(DoorLockMode mode)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)mode];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new DoorLockOperationSetCommand(frame);
        }
    }

    private readonly struct DoorLockOperationGetCommand : ICommand
    {
        public DoorLockOperationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.OperationGet;

        public CommandClassFrame Frame { get; }

        public static DoorLockOperationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new DoorLockOperationGetCommand(frame);
        }
    }

    private readonly struct DoorLockOperationReportCommand : ICommand
    {
        private readonly byte _version;

        public DoorLockOperationReportCommand(CommandClassFrame frame, byte version)
        {
            Frame = frame;
            _version = version;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.OperationReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current door lock mode.
        /// </summary>
        public DoorLockMode Mode => (DoorLockMode)Frame.CommandParameters.Span[0];

        /// <summary>
        /// The outside door handles mode (upper nibble of byte 2).
        /// </summary>
        public byte OutsideDoorHandlesMode => (byte)((Frame.CommandParameters.Span[1] >> 4) & 0x0F);

        /// <summary>
        /// The inside door handles mode (lower nibble of byte 2).
        /// </summary>
        public byte InsideDoorHandlesMode => (byte)(Frame.CommandParameters.Span[1] & 0x0F);

        /// <summary>
        /// The door condition bitmask.
        /// </summary>
        public byte DoorCondition => Frame.CommandParameters.Span[2];

        /// <summary>
        /// The lock timeout in minutes (v2+).
        /// </summary>
        public byte? LockTimeoutMinutes => _version >= 2 && Frame.CommandParameters.Length > 3
            ? Frame.CommandParameters.Span[3]
            : null;

        /// <summary>
        /// The lock timeout in seconds (v2+).
        /// </summary>
        public byte? LockTimeoutSeconds => _version >= 2 && Frame.CommandParameters.Length > 4
            ? Frame.CommandParameters.Span[4]
            : null;

        /// <summary>
        /// The target door lock mode of an ongoing transition (v3+).
        /// </summary>
        public DoorLockMode? TargetMode => _version >= 3 && Frame.CommandParameters.Length > 5
            ? (DoorLockMode)Frame.CommandParameters.Span[5]
            : null;

        /// <summary>
        /// The duration of an ongoing transition (v3+).
        /// </summary>
        public byte? Duration => _version >= 3 && Frame.CommandParameters.Length > 6
            ? Frame.CommandParameters.Span[6]
            : null;
    }

    private readonly struct DoorLockConfigurationSetCommand : ICommand
    {
        public DoorLockConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.ConfigurationSet;

        public CommandClassFrame Frame { get; }

        public static DoorLockConfigurationSetCommand Create(
            DoorLockOperationType operationType,
            byte outsideHandlesState,
            byte insideHandlesState,
            byte lockTimeoutMinutes,
            byte lockTimeoutSeconds)
        {
            Span<byte> commandParameters = stackalloc byte[4];
            commandParameters[0] = (byte)operationType;
            commandParameters[1] = (byte)(((outsideHandlesState & 0x0F) << 4) | (insideHandlesState & 0x0F));
            commandParameters[2] = lockTimeoutMinutes;
            commandParameters[3] = lockTimeoutSeconds;
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new DoorLockConfigurationSetCommand(frame);
        }
    }

    private readonly struct DoorLockConfigurationGetCommand : ICommand
    {
        public DoorLockConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.ConfigurationGet;

        public CommandClassFrame Frame { get; }

        public static DoorLockConfigurationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new DoorLockConfigurationGetCommand(frame);
        }
    }

    private readonly struct DoorLockConfigurationReportCommand : ICommand
    {
        public DoorLockConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.ConfigurationReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The operation type.
        /// </summary>
        public DoorLockOperationType OperationType => (DoorLockOperationType)Frame.CommandParameters.Span[0];

        /// <summary>
        /// The outside door handles state (upper nibble).
        /// </summary>
        public byte OutsideHandlesState => (byte)((Frame.CommandParameters.Span[1] >> 4) & 0x0F);

        /// <summary>
        /// The inside door handles state (lower nibble).
        /// </summary>
        public byte InsideHandlesState => (byte)(Frame.CommandParameters.Span[1] & 0x0F);

        /// <summary>
        /// The lock timeout in minutes.
        /// </summary>
        public byte LockTimeoutMinutes => Frame.CommandParameters.Span[2];

        /// <summary>
        /// The lock timeout in seconds.
        /// </summary>
        public byte LockTimeoutSeconds => Frame.CommandParameters.Span[3];
    }

    private readonly struct DoorLockCapabilitiesGetCommand : ICommand
    {
        public DoorLockCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.CapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static DoorLockCapabilitiesGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new DoorLockCapabilitiesGetCommand(frame);
        }
    }

    private readonly struct DoorLockCapabilitiesReportCommand : ICommand
    {
        public DoorLockCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DoorLock;

        public static byte CommandId => (byte)DoorLockCommand.CapabilitiesReport;

        public CommandClassFrame Frame { get; }

        private int OperationTypeBitmaskLength => Frame.CommandParameters.Span[0] & 0x1F;

        private int ModeListOffset => 1 + OperationTypeBitmaskLength;

        private int ModeListLength => Frame.CommandParameters.Span[ModeListOffset];

        private int HandlesOffset => ModeListOffset + 1 + ModeListLength;

        public HashSet<DoorLockOperationType> SupportedOperationTypes
        {
            get
            {
                HashSet<DoorLockOperationType> result = new();
                ReadOnlySpan<byte> span = Frame.CommandParameters.Span;
                int length = OperationTypeBitmaskLength;
                for (int i = 0; i < length; i++)
                {
                    byte b = span[1 + i];
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if ((b & (1 << bit)) != 0)
                        {
                            result.Add((DoorLockOperationType)((i * 8) + bit));
                        }
                    }
                }

                return result;
            }
        }

        public HashSet<DoorLockMode> SupportedModes
        {
            get
            {
                HashSet<DoorLockMode> result = new();
                ReadOnlySpan<byte> span = Frame.CommandParameters.Span;
                int offset = ModeListOffset + 1;
                int length = ModeListLength;
                for (int i = 0; i < length; i++)
                {
                    result.Add((DoorLockMode)span[offset + i]);
                }

                return result;
            }
        }

        public byte SupportedOutsideHandles => (byte)((Frame.CommandParameters.Span[HandlesOffset] >> 4) & 0x0F);

        public byte SupportedInsideHandles => (byte)(Frame.CommandParameters.Span[HandlesOffset] & 0x0F);
    }
}
