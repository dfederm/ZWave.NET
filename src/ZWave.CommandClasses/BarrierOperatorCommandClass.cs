namespace ZWave.CommandClasses;

public enum BarrierOperatorCommand : byte
{
    /// <summary>
    /// Set the target state of the barrier operator.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current state of the barrier operator.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current state of the barrier operator.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported signaling subsystem types.
    /// </summary>
    SignalSupportedGet = 0x04,

    /// <summary>
    /// Advertise the supported signaling subsystem types.
    /// </summary>
    SignalSupportedReport = 0x05,

    /// <summary>
    /// Set the state of a signaling subsystem.
    /// </summary>
    SignalSet = 0x06,

    /// <summary>
    /// Request the state of a signaling subsystem.
    /// </summary>
    SignalGet = 0x07,

    /// <summary>
    /// Advertise the state of a signaling subsystem.
    /// </summary>
    SignalReport = 0x08,
}

/// <summary>
/// Represents the state of the barrier operator.
/// </summary>
public enum BarrierOperatorState : byte
{
    Closed = 0x00,

    // Values 0x01-0x63 represent percentage open

    Closing = 0xFC,

    Stopped = 0xFD,

    Opening = 0xFE,

    Open = 0xFF,
}

/// <summary>
/// Identifies a signaling subsystem type for the barrier operator.
/// </summary>
public enum BarrierOperatorSubsystemType : byte
{
    Audible = 0x01,

    Visual = 0x02,
}

[CommandClass(CommandClassId.BarrierOperator)]
public sealed class BarrierOperatorCommandClass : CommandClass<BarrierOperatorCommand>
{
    private Dictionary<BarrierOperatorSubsystemType, byte?>? _signalStates;

    internal BarrierOperatorCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported barrier state.
    /// </summary>
    public BarrierOperatorState? BarrierState { get; private set; }

    /// <summary>
    /// Gets the supported signaling subsystem types.
    /// </summary>
    public IReadOnlySet<BarrierOperatorSubsystemType>? SupportedSignalTypes { get; private set; }

    /// <summary>
    /// Gets the state of each signaling subsystem (0x00=off, 0xFF=on).
    /// </summary>
    public IReadOnlyDictionary<BarrierOperatorSubsystemType, byte?>? SignalStates => _signalStates;

    /// <inheritdoc />
    public override bool? IsCommandSupported(BarrierOperatorCommand command)
        => command switch
        {
            BarrierOperatorCommand.Set => true,
            BarrierOperatorCommand.Get => true,
            BarrierOperatorCommand.SignalSupportedGet => true,
            BarrierOperatorCommand.SignalSet => true,
            BarrierOperatorCommand.SignalGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlySet<BarrierOperatorSubsystemType> supportedSignalTypes = await GetSignalSupportedAsync(cancellationToken).ConfigureAwait(false);

        foreach (BarrierOperatorSubsystemType subsystemType in supportedSignalTypes)
        {
            _ = await GetSignalAsync(subsystemType, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request the current state of the barrier operator.
    /// </summary>
    public async Task<BarrierOperatorState> GetAsync(CancellationToken cancellationToken)
    {
        var command = BarrierOperatorGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<BarrierOperatorReportCommand>(cancellationToken).ConfigureAwait(false);
        return BarrierState!.Value;
    }

    /// <summary>
    /// Set the target state of the barrier operator.
    /// </summary>
    public async Task SetAsync(byte targetValue, CancellationToken cancellationToken)
    {
        var command = BarrierOperatorSetCommand.Create(targetValue);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported signaling subsystem types.
    /// </summary>
    public async Task<IReadOnlySet<BarrierOperatorSubsystemType>> GetSignalSupportedAsync(CancellationToken cancellationToken)
    {
        var command = BarrierOperatorSignalSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<BarrierOperatorSignalSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedSignalTypes!;
    }

    /// <summary>
    /// Set the state of a signaling subsystem.
    /// </summary>
    public async Task SetSignalAsync(BarrierOperatorSubsystemType subsystemType, byte state, CancellationToken cancellationToken)
    {
        var command = BarrierOperatorSignalSetCommand.Create(subsystemType, state);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the state of a signaling subsystem.
    /// </summary>
    public async Task<byte> GetSignalAsync(BarrierOperatorSubsystemType subsystemType, CancellationToken cancellationToken)
    {
        var command = BarrierOperatorSignalGetCommand.Create(subsystemType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<BarrierOperatorSignalReportCommand>(cancellationToken).ConfigureAwait(false);
        return _signalStates![subsystemType]!.Value;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((BarrierOperatorCommand)frame.CommandId)
        {
            case BarrierOperatorCommand.Set:
            case BarrierOperatorCommand.Get:
            case BarrierOperatorCommand.SignalSupportedGet:
            case BarrierOperatorCommand.SignalSet:
            case BarrierOperatorCommand.SignalGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case BarrierOperatorCommand.Report:
            {
                var command = new BarrierOperatorReportCommand(frame);
                BarrierState = command.BarrierState;
                break;
            }
            case BarrierOperatorCommand.SignalSupportedReport:
            {
                var command = new BarrierOperatorSignalSupportedReportCommand(frame);
                SupportedSignalTypes = command.SupportedSubsystemTypes;

                var newSignalStates = new Dictionary<BarrierOperatorSubsystemType, byte?>();
                foreach (BarrierOperatorSubsystemType subsystemType in SupportedSignalTypes)
                {
                    // Persist any existing known state.
                    if (SignalStates == null
                        || !SignalStates.TryGetValue(subsystemType, out byte? signalState))
                    {
                        signalState = null;
                    }

                    newSignalStates.Add(subsystemType, signalState);
                }

                _signalStates = newSignalStates;
                break;
            }
            case BarrierOperatorCommand.SignalReport:
            {
                var command = new BarrierOperatorSignalReportCommand(frame);
                _signalStates![command.SubsystemType] = command.SubsystemState;
                break;
            }
        }
    }

    private readonly struct BarrierOperatorSetCommand : ICommand
    {
        public BarrierOperatorSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.Set;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorSetCommand Create(byte targetValue)
        {
            ReadOnlySpan<byte> commandParameters = [targetValue];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BarrierOperatorSetCommand(frame);
        }
    }

    private readonly struct BarrierOperatorGetCommand : ICommand
    {
        public BarrierOperatorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BarrierOperatorGetCommand(frame);
        }
    }

    private readonly struct BarrierOperatorReportCommand : ICommand
    {
        public BarrierOperatorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.Report;

        public CommandClassFrame Frame { get; }

        public BarrierOperatorState BarrierState => (BarrierOperatorState)Frame.CommandParameters.Span[0];
    }

    private readonly struct BarrierOperatorSignalSupportedGetCommand : ICommand
    {
        public BarrierOperatorSignalSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.SignalSupportedGet;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorSignalSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BarrierOperatorSignalSupportedGetCommand(frame);
        }
    }

    private readonly struct BarrierOperatorSignalSupportedReportCommand : ICommand
    {
        public BarrierOperatorSignalSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.SignalSupportedReport;

        public CommandClassFrame Frame { get; }

        public IReadOnlySet<BarrierOperatorSubsystemType> SupportedSubsystemTypes
        {
            get
            {
                var supportedTypes = new HashSet<BarrierOperatorSubsystemType>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            BarrierOperatorSubsystemType subsystemType = (BarrierOperatorSubsystemType)((byteNum << 3) + bitNum);
                            supportedTypes.Add(subsystemType);
                        }
                    }
                }

                return supportedTypes;
            }
        }
    }

    private readonly struct BarrierOperatorSignalSetCommand : ICommand
    {
        public BarrierOperatorSignalSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.SignalSet;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorSignalSetCommand Create(BarrierOperatorSubsystemType subsystemType, byte state)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)subsystemType, state];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BarrierOperatorSignalSetCommand(frame);
        }
    }

    private readonly struct BarrierOperatorSignalGetCommand : ICommand
    {
        public BarrierOperatorSignalGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.SignalGet;

        public CommandClassFrame Frame { get; }

        public static BarrierOperatorSignalGetCommand Create(BarrierOperatorSubsystemType subsystemType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)subsystemType];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BarrierOperatorSignalGetCommand(frame);
        }
    }

    private readonly struct BarrierOperatorSignalReportCommand : ICommand
    {
        public BarrierOperatorSignalReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.SignalReport;

        public CommandClassFrame Frame { get; }

        public BarrierOperatorSubsystemType SubsystemType => (BarrierOperatorSubsystemType)Frame.CommandParameters.Span[0];

        public byte SubsystemState => Frame.CommandParameters.Span[1];
    }
}
