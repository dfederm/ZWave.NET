namespace ZWave.CommandClasses;

public enum WindowCoveringCommand : byte
{
    /// <summary>
    /// Request the supported parameter IDs from a node.
    /// </summary>
    SupportedGet = 0x01,

    /// <summary>
    /// Advertise the supported parameter IDs at the sending node.
    /// </summary>
    SupportedReport = 0x02,

    /// <summary>
    /// Request the current state of one or more parameters from a node.
    /// </summary>
    Get = 0x03,

    /// <summary>
    /// Advertise the current state of one or more parameters at the sending node.
    /// </summary>
    Report = 0x04,

    /// <summary>
    /// Set the value of one or more parameters at the receiving node.
    /// </summary>
    Set = 0x05,

    /// <summary>
    /// Initiate a transition for a parameter.
    /// </summary>
    StartLevelChange = 0x06,

    /// <summary>
    /// Stop an ongoing transition for a parameter.
    /// </summary>
    StopLevelChange = 0x07,
}

/// <summary>
/// Identifies a window covering parameter.
/// </summary>
public enum WindowCoveringParameterId : byte
{
    OutboundLeftPosition = 0x00,

    OutboundRightPosition = 0x02,

    InboundLeftPosition = 0x04,

    InboundRightPosition = 0x06,

    VerticalSlatsAngle = 0x0C,

    HorizontalSlatsAngle = 0x0E,
}

/// <summary>
/// Represents the state of a single window covering parameter.
/// </summary>
public readonly struct WindowCoveringParameterState
{
    public WindowCoveringParameterState(
        byte currentValue,
        byte targetValue,
        DurationReport duration)
    {
        CurrentValue = currentValue;
        TargetValue = targetValue;
        Duration = duration;
    }

    /// <summary>
    /// The current value of the parameter.
    /// </summary>
    public byte CurrentValue { get; }

    /// <summary>
    /// The target value of the parameter.
    /// </summary>
    public byte TargetValue { get; }

    /// <summary>
    /// The remaining duration to reach the target value.
    /// </summary>
    public DurationReport Duration { get; }
}

[CommandClass(CommandClassId.WindowCovering)]
public sealed class WindowCoveringCommandClass : CommandClass<WindowCoveringCommand>
{
    private Dictionary<WindowCoveringParameterId, WindowCoveringParameterState>? _parameterStates;

    internal WindowCoveringCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the supported parameter IDs.
    /// </summary>
    public IReadOnlySet<WindowCoveringParameterId>? SupportedParameters { get; private set; }

    /// <summary>
    /// Gets the state of each supported parameter.
    /// </summary>
    public IReadOnlyDictionary<WindowCoveringParameterId, WindowCoveringParameterState>? ParameterStates => _parameterStates;

    /// <inheritdoc />
    public override bool? IsCommandSupported(WindowCoveringCommand command)
        => command switch
        {
            WindowCoveringCommand.SupportedGet => true,
            WindowCoveringCommand.Get => true,
            WindowCoveringCommand.Set => true,
            WindowCoveringCommand.StartLevelChange => true,
            WindowCoveringCommand.StopLevelChange => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        IReadOnlySet<WindowCoveringParameterId> supportedParameters = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

        foreach (WindowCoveringParameterId parameterId in supportedParameters)
        {
            _ = await GetAsync(parameterId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request the supported parameter IDs from a node.
    /// </summary>
    public async Task<IReadOnlySet<WindowCoveringParameterId>> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = WindowCoveringSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<WindowCoveringSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedParameters!;
    }

    /// <summary>
    /// Request the current state of a parameter from a node.
    /// </summary>
    public async Task<WindowCoveringParameterState> GetAsync(
        WindowCoveringParameterId parameterId,
        CancellationToken cancellationToken)
    {
        var command = WindowCoveringGetCommand.Create(parameterId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<WindowCoveringReportCommand>(
            predicate: frame =>
            {
                WindowCoveringReportCommand report = new WindowCoveringReportCommand(frame);
                return report.ParameterId == parameterId;
            },
            cancellationToken).ConfigureAwait(false);
        return _parameterStates![parameterId];
    }

    /// <summary>
    /// Set the value of one or more parameters at the receiving node.
    /// </summary>
    public async Task SetAsync(
        (WindowCoveringParameterId ParameterId, byte Value)[] parameters,
        DurationSet duration,
        CancellationToken cancellationToken)
    {
        var command = WindowCoveringSetCommand.Create(parameters, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Initiate a transition for a parameter.
    /// </summary>
    public async Task StartLevelChangeAsync(
        bool up,
        WindowCoveringParameterId parameterId,
        DurationSet duration,
        CancellationToken cancellationToken)
    {
        var command = WindowCoveringStartLevelChangeCommand.Create(up, parameterId, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop an ongoing transition for a parameter.
    /// </summary>
    public async Task StopLevelChangeAsync(
        WindowCoveringParameterId parameterId,
        CancellationToken cancellationToken)
    {
        var command = WindowCoveringStopLevelChangeCommand.Create(parameterId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((WindowCoveringCommand)frame.CommandId)
        {
            case WindowCoveringCommand.SupportedGet:
            case WindowCoveringCommand.Get:
            case WindowCoveringCommand.Set:
            case WindowCoveringCommand.StartLevelChange:
            case WindowCoveringCommand.StopLevelChange:
            {
                // We don't expect to recieve these commands
                break;
            }
            case WindowCoveringCommand.SupportedReport:
            {
                var command = new WindowCoveringSupportedReportCommand(frame);
                SupportedParameters = command.SupportedParameterIds;

                var newStates = new Dictionary<WindowCoveringParameterId, WindowCoveringParameterState>();
                foreach (WindowCoveringParameterId parameterId in SupportedParameters)
                {
                    if (_parameterStates != null
                        && _parameterStates.TryGetValue(parameterId, out WindowCoveringParameterState existingState))
                    {
                        newStates.Add(parameterId, existingState);
                    }
                }

                _parameterStates = newStates;
                break;
            }
            case WindowCoveringCommand.Report:
            {
                var command = new WindowCoveringReportCommand(frame);

                _parameterStates ??= new Dictionary<WindowCoveringParameterId, WindowCoveringParameterState>();
                _parameterStates[command.ParameterId] = command.State;

                break;
            }
        }
    }

    private readonly struct WindowCoveringSupportedGetCommand : ICommand
    {
        public WindowCoveringSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WindowCoveringSupportedGetCommand(frame);
        }
    }

    private readonly struct WindowCoveringSupportedReportCommand : ICommand
    {
        public WindowCoveringSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public IReadOnlySet<WindowCoveringParameterId> SupportedParameterIds
        {
            get
            {
                var supportedIds = new HashSet<WindowCoveringParameterId>();

                int numBitmaskBytes = Frame.CommandParameters.Span[0] & 0x0F;
                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(
                    1,
                    Math.Min(numBitmaskBytes, Frame.CommandParameters.Length - 1));
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            WindowCoveringParameterId parameterId = (WindowCoveringParameterId)((byteNum * 8) + bitNum);
                            supportedIds.Add(parameterId);
                        }
                    }
                }

                return supportedIds;
            }
        }
    }

    private readonly struct WindowCoveringGetCommand : ICommand
    {
        public WindowCoveringGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.Get;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringGetCommand Create(WindowCoveringParameterId parameterId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)parameterId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringGetCommand(frame);
        }
    }

    private readonly struct WindowCoveringReportCommand : ICommand
    {
        public WindowCoveringReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The parameter ID reported.
        /// </summary>
        public WindowCoveringParameterId ParameterId
            => (WindowCoveringParameterId)Frame.CommandParameters.Span[0];

        /// <summary>
        /// The state of the reported parameter.
        /// </summary>
        public WindowCoveringParameterState State
        {
            get
            {
                ReadOnlySpan<byte> data = Frame.CommandParameters.Span;
                byte currentValue = data[1];
                byte targetValue = data[2];
                DurationReport duration = data[3];
                return new WindowCoveringParameterState(currentValue, targetValue, duration);
            }
        }
    }

    private readonly struct WindowCoveringSetCommand : ICommand
    {
        public WindowCoveringSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.Set;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringSetCommand Create(
            (WindowCoveringParameterId ParameterId, byte Value)[] parameters,
            DurationSet duration)
        {
            Span<byte> commandParameters = stackalloc byte[1 + (parameters.Length * 2) + 1];
            commandParameters[0] = (byte)parameters.Length;
            for (int i = 0; i < parameters.Length; i++)
            {
                commandParameters[1 + (i * 2)] = (byte)parameters[i].ParameterId;
                commandParameters[2 + (i * 2)] = parameters[i].Value;
            }

            commandParameters[1 + (parameters.Length * 2)] = duration.Value;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringSetCommand(frame);
        }
    }

    private readonly struct WindowCoveringStartLevelChangeCommand : ICommand
    {
        public WindowCoveringStartLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.StartLevelChange;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringStartLevelChangeCommand Create(
            bool up,
            WindowCoveringParameterId parameterId,
            DurationSet duration)
        {
            Span<byte> commandParameters = stackalloc byte[3];
            commandParameters[0] = up ? (byte)0x40 : (byte)0x00;
            commandParameters[1] = (byte)parameterId;
            commandParameters[2] = duration.Value;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringStartLevelChangeCommand(frame);
        }
    }

    private readonly struct WindowCoveringStopLevelChangeCommand : ICommand
    {
        public WindowCoveringStopLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.StopLevelChange;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringStopLevelChangeCommand Create(WindowCoveringParameterId parameterId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)parameterId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WindowCoveringStopLevelChangeCommand(frame);
        }
    }
}
