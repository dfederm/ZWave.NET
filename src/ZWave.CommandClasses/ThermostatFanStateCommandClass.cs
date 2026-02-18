namespace ZWave.CommandClasses;

public enum ThermostatFanStateCommand : byte
{
    /// <summary>
    /// Request the fan operating state from a node.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current fan operating state at the sending node.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Identifies the operating state of a thermostat fan.
/// </summary>
public enum ThermostatFanState : byte
{
    Idle = 0x00,

    Running = 0x01,

    RunningHigh = 0x02,

    RunningMedium = 0x03,

    Circulation = 0x04,

    HumidityCirculation = 0x05,

    RightLeftCirculation = 0x06,

    UpDownCirculation = 0x07,

    QuietCirculation = 0x08,
}

[CommandClass(CommandClassId.ThermostatFanState)]
public sealed class ThermostatFanStateCommandClass : CommandClass<ThermostatFanStateCommand>
{
    internal ThermostatFanStateCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the current fan operating state.
    /// </summary>
    public ThermostatFanState? FanState { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ThermostatFanStateCommand command)
        => command switch
        {
            ThermostatFanStateCommand.Get => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the fan operating state from a node.
    /// </summary>
    public async Task<ThermostatFanState> GetAsync(CancellationToken cancellationToken)
    {
        var command = ThermostatFanStateGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatFanStateReportCommand>(cancellationToken).ConfigureAwait(false);
        return FanState!.Value;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ThermostatFanStateCommand)frame.CommandId)
        {
            case ThermostatFanStateCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ThermostatFanStateCommand.Report:
            {
                var command = new ThermostatFanStateReportCommand(frame);
                FanState = command.FanState;
                break;
            }
        }
    }

    private readonly struct ThermostatFanStateGetCommand : ICommand
    {
        public ThermostatFanStateGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanState;

        public static byte CommandId => (byte)ThermostatFanStateCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ThermostatFanStateGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatFanStateGetCommand(frame);
        }
    }

    private readonly struct ThermostatFanStateReportCommand : ICommand
    {
        public ThermostatFanStateReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanState;

        public static byte CommandId => (byte)ThermostatFanStateCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current fan operating state.
        /// </summary>
        public ThermostatFanState FanState => (ThermostatFanState)(Frame.CommandParameters.Span[0] & 0x0F);
    }
}
