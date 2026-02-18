namespace ZWave.CommandClasses;

/// <summary>
/// Defines the local protection state of a device.
/// </summary>
public enum LocalProtectionState : byte
{
    /// <summary>
    /// The device is not protected and can be operated normally.
    /// </summary>
    Unprotected = 0x00,

    /// <summary>
    /// The device is protected by a sequence that must be completed before operation.
    /// </summary>
    BySequence = 0x01,

    /// <summary>
    /// No local operation is possible.
    /// </summary>
    NoOperationPossible = 0x02,
}

/// <summary>
/// Defines the RF protection state of a device.
/// </summary>
public enum RfProtectionState : byte
{
    /// <summary>
    /// The device is not RF protected and can be operated normally via RF.
    /// </summary>
    Unprotected = 0x00,

    /// <summary>
    /// No RF control is possible. All runtime Commands are ignored.
    /// </summary>
    NoControl = 0x01,

    /// <summary>
    /// No RF response at all. The device does not respond to any Z-Wave commands.
    /// </summary>
    NoResponse = 0x02,
}

public enum ProtectionCommand : byte
{
    /// <summary>
    /// Set the protection state of a node.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the protection state of a node.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the protection state of the sending node.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported protection states from a node.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Advertise the supported protection states of a node.
    /// </summary>
    SupportedReport = 0x05,

    /// <summary>
    /// Set the exclusive control node for a device.
    /// </summary>
    ExclusiveControlSet = 0x06,

    /// <summary>
    /// Request the exclusive control node of a device.
    /// </summary>
    ExclusiveControlGet = 0x07,

    /// <summary>
    /// Advertise the exclusive control node of a device.
    /// </summary>
    ExclusiveControlReport = 0x08,

    /// <summary>
    /// Set the RF protection timeout at the receiving node.
    /// </summary>
    TimeoutSet = 0x09,

    /// <summary>
    /// Request the RF protection timeout from a node.
    /// </summary>
    TimeoutGet = 0x0A,

    /// <summary>
    /// Advertise the RF protection timeout of a node.
    /// </summary>
    TimeoutReport = 0x0B,
}

[CommandClass(CommandClassId.Protection)]
public sealed class ProtectionCommandClass : CommandClass<ProtectionCommand>
{
    internal ProtectionCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported local protection state.
    /// </summary>
    public LocalProtectionState? LocalProtectionState { get; private set; }

    /// <summary>
    /// Gets the last reported RF protection state.
    /// </summary>
    public RfProtectionState? RfProtectionState { get; private set; }

    /// <summary>
    /// Gets the supported local protection states.
    /// </summary>
    public IReadOnlySet<LocalProtectionState>? SupportedLocalStates { get; private set; }

    /// <summary>
    /// Gets the supported RF protection states.
    /// </summary>
    public IReadOnlySet<RfProtectionState>? SupportedRfStates { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ProtectionCommand command)
        => command switch
        {
            ProtectionCommand.Set => true,
            ProtectionCommand.Get => true,
            ProtectionCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.ExclusiveControlSet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.ExclusiveControlGet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.TimeoutSet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.TimeoutGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(ProtectionCommand.SupportedGet).GetValueOrDefault(false))
        {
            _ = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);
        }

        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the protection state of a node.
    /// </summary>
    public async Task<(LocalProtectionState Local, RfProtectionState? Rf)> GetAsync(CancellationToken cancellationToken)
    {
        var command = ProtectionGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ProtectionReportCommand>(cancellationToken).ConfigureAwait(false);
        return (LocalProtectionState!.Value, RfProtectionState);
    }

    /// <summary>
    /// Set the protection state of a node.
    /// </summary>
    public async Task SetAsync(
        LocalProtectionState localState,
        RfProtectionState? rfState,
        CancellationToken cancellationToken)
    {
        var command = ProtectionSetCommand.Create(EffectiveVersion, localState, rfState);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported protection states from a node.
    /// </summary>
    public async Task<(IReadOnlySet<LocalProtectionState> Local, IReadOnlySet<RfProtectionState> Rf)> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = ProtectionSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ProtectionSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return (SupportedLocalStates!, SupportedRfStates!);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ProtectionCommand)frame.CommandId)
        {
            case ProtectionCommand.Set:
            case ProtectionCommand.Get:
            case ProtectionCommand.SupportedGet:
            case ProtectionCommand.ExclusiveControlSet:
            case ProtectionCommand.ExclusiveControlGet:
            case ProtectionCommand.TimeoutSet:
            case ProtectionCommand.TimeoutGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ProtectionCommand.Report:
            {
                var command = new ProtectionReportCommand(frame, EffectiveVersion);
                LocalProtectionState = command.LocalProtectionState;
                RfProtectionState = command.RfProtectionState;
                break;
            }
            case ProtectionCommand.SupportedReport:
            {
                var command = new ProtectionSupportedReportCommand(frame);
                SupportedLocalStates = command.SupportedLocalStates;
                SupportedRfStates = command.SupportedRfStates;
                break;
            }
            case ProtectionCommand.ExclusiveControlReport:
            case ProtectionCommand.TimeoutReport:
            {
                // TODO: Implement exclusive control and timeout report handling
                break;
            }
        }
    }

    private readonly struct ProtectionSetCommand : ICommand
    {
        public ProtectionSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ProtectionSetCommand Create(
            byte version,
            LocalProtectionState localState,
            RfProtectionState? rfState)
        {
            bool includeRfState = version >= 2 && rfState.HasValue;
            Span<byte> commandParameters = stackalloc byte[1 + (includeRfState ? 1 : 0)];
            commandParameters[0] = (byte)localState;
            if (includeRfState)
            {
                commandParameters[1] = (byte)rfState!.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionSetCommand(frame);
        }
    }

    private readonly struct ProtectionGetCommand : ICommand
    {
        public ProtectionGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ProtectionGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ProtectionGetCommand(frame);
        }
    }

    private readonly struct ProtectionReportCommand : ICommand
    {
        private readonly byte _version;

        public ProtectionReportCommand(CommandClassFrame frame, byte version)
        {
            Frame = frame;
            _version = version;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The local protection state of the node.
        /// </summary>
        public LocalProtectionState LocalProtectionState => (LocalProtectionState)Frame.CommandParameters.Span[0];

        /// <summary>
        /// The RF protection state of the node.
        /// </summary>
        public RfProtectionState? RfProtectionState => _version >= 2 && Frame.CommandParameters.Length > 1
            ? (RfProtectionState)Frame.CommandParameters.Span[1]
            : null;
    }

    private readonly struct ProtectionSupportedGetCommand : ICommand
    {
        public ProtectionSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static ProtectionSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ProtectionSupportedGetCommand(frame);
        }
    }

    private readonly struct ProtectionSupportedReportCommand : ICommand
    {
        public ProtectionSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The supported local protection states.
        /// </summary>
        public IReadOnlySet<LocalProtectionState> SupportedLocalStates
        {
            get
            {
                var supportedStates = new HashSet<LocalProtectionState>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(0, 2);
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            LocalProtectionState state = (LocalProtectionState)((byteNum << 3) + bitNum);
                            supportedStates.Add(state);
                        }
                    }
                }

                return supportedStates;
            }
        }

        /// <summary>
        /// The supported RF protection states.
        /// </summary>
        public IReadOnlySet<RfProtectionState> SupportedRfStates
        {
            get
            {
                var supportedStates = new HashSet<RfProtectionState>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(2, 2);
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            RfProtectionState state = (RfProtectionState)((byteNum << 3) + bitNum);
                            supportedStates.Add(state);
                        }
                    }
                }

                return supportedStates;
            }
        }
    }
}
