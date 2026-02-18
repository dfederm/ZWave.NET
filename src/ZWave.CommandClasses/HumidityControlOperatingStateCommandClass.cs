namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the operating state of a humidity control device.
/// </summary>
public enum HumidityControlOperatingState : byte
{
    /// <summary>
    /// The humidity control device is idle.
    /// </summary>
    Idle = 0x00,

    /// <summary>
    /// The humidity control device is humidifying.
    /// </summary>
    Humidifying = 0x01,

    /// <summary>
    /// The humidity control device is dehumidifying.
    /// </summary>
    Dehumidifying = 0x02,
}

public enum HumidityControlOperatingStateCommand : byte
{
    /// <summary>
    /// Request the operating state of a humidity control device.
    /// </summary>
    Get = 0x01,

    /// <summary>
    /// Advertise the operating state of a humidity control device.
    /// </summary>
    Report = 0x02,
}

[CommandClass(CommandClassId.HumidityControlOperatingState)]
public sealed class HumidityControlOperatingStateCommandClass : CommandClass<HumidityControlOperatingStateCommand>
{
    internal HumidityControlOperatingStateCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported humidity control operating state.
    /// </summary>
    public HumidityControlOperatingState? OperatingState { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(HumidityControlOperatingStateCommand command)
        => command switch
        {
            HumidityControlOperatingStateCommand.Get => true,
            _ => false,
        };

    /// <summary>
    /// Request the current operating state of the humidity control device.
    /// </summary>
    public async Task<HumidityControlOperatingState> GetAsync(CancellationToken cancellationToken)
    {
        var command = HumidityControlOperatingStateGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<HumidityControlOperatingStateReportCommand>(cancellationToken).ConfigureAwait(false);
        return OperatingState!.Value;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((HumidityControlOperatingStateCommand)frame.CommandId)
        {
            case HumidityControlOperatingStateCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case HumidityControlOperatingStateCommand.Report:
            {
                var command = new HumidityControlOperatingStateReportCommand(frame);
                OperatingState = command.OperatingState;
                break;
            }
        }
    }

    private readonly struct HumidityControlOperatingStateGetCommand : ICommand
    {
        public HumidityControlOperatingStateGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlOperatingState;

        public static byte CommandId => (byte)HumidityControlOperatingStateCommand.Get;

        public CommandClassFrame Frame { get; }

        public static HumidityControlOperatingStateGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new HumidityControlOperatingStateGetCommand(frame);
        }
    }

    private readonly struct HumidityControlOperatingStateReportCommand : ICommand
    {
        public HumidityControlOperatingStateReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlOperatingState;

        public static byte CommandId => (byte)HumidityControlOperatingStateCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current operating state of the humidity control device.
        /// </summary>
        public HumidityControlOperatingState OperatingState
            => (HumidityControlOperatingState)(Frame.CommandParameters.Span[0] & 0x0F);
    }
}
