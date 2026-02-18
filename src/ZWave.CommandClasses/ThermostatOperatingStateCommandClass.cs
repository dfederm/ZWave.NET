namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the operating state of a thermostat.
/// </summary>
public enum ThermostatOperatingState : byte
{
    /// <summary>
    /// The thermostat is idle.
    /// </summary>
    Idle = 0x00,

    /// <summary>
    /// The thermostat is heating.
    /// </summary>
    Heating = 0x01,

    /// <summary>
    /// The thermostat is cooling.
    /// </summary>
    Cooling = 0x02,

    /// <summary>
    /// The thermostat fan is running.
    /// </summary>
    FanOnly = 0x03,

    /// <summary>
    /// The thermostat is pending heat.
    /// </summary>
    PendingHeat = 0x04,

    /// <summary>
    /// The thermostat is pending cool.
    /// </summary>
    PendingCool = 0x05,

    /// <summary>
    /// The thermostat is running the vent economizer.
    /// </summary>
    VentEconomizer = 0x06,

    /// <summary>
    /// The thermostat is running auxiliary heating.
    /// </summary>
    AuxHeating = 0x07,

    /// <summary>
    /// The thermostat is running second stage heating.
    /// </summary>
    SecondStageHeating = 0x08,

    /// <summary>
    /// The thermostat is running second stage cooling.
    /// </summary>
    SecondStageCooling = 0x09,

    /// <summary>
    /// The thermostat is running second stage auxiliary heat.
    /// </summary>
    SecondStageAuxHeat = 0x0A,

    /// <summary>
    /// The thermostat is running third stage auxiliary heat.
    /// </summary>
    ThirdStageAuxHeat = 0x0B,
}

public enum ThermostatOperatingStateCommand : byte
{
    /// <summary>
    /// Request the operating state of a thermostat.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the operating state of a thermostat.
    /// </summary>
    Report = 0x03,
}

[CommandClass(CommandClassId.ThermostatOperatingState)]
public sealed class ThermostatOperatingStateCommandClass : CommandClass<ThermostatOperatingStateCommand>
{
    public ThermostatOperatingStateCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported thermostat operating state.
    /// </summary>
    public ThermostatOperatingState? OperatingState { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ThermostatOperatingStateCommand command)
        => command switch
        {
            ThermostatOperatingStateCommand.Get => true,
            _ => false,
        };

    /// <summary>
    /// Request the current operating state of the thermostat.
    /// </summary>
    public async Task<ThermostatOperatingState> GetAsync(CancellationToken cancellationToken)
    {
        var command = ThermostatOperatingStateGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatOperatingStateReportCommand>(cancellationToken).ConfigureAwait(false);
        return OperatingState!.Value;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ThermostatOperatingStateCommand)frame.CommandId)
        {
            case ThermostatOperatingStateCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ThermostatOperatingStateCommand.Report:
            {
                var command = new ThermostatOperatingStateReportCommand(frame);
                OperatingState = command.OperatingState;
                break;
            }
        }
    }

    private readonly struct ThermostatOperatingStateGetCommand : ICommand
    {
        public ThermostatOperatingStateGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ThermostatOperatingStateGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatOperatingStateGetCommand(frame);
        }
    }

    private readonly struct ThermostatOperatingStateReportCommand : ICommand
    {
        public ThermostatOperatingStateReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current operating state of the thermostat.
        /// </summary>
        public ThermostatOperatingState OperatingState
            => (ThermostatOperatingState)(Frame.CommandParameters.Span[0] & 0x0F);
    }
}
