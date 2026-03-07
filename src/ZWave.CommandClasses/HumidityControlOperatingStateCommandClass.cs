using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The operating state of a humidity control device.
/// </summary>
public enum HumidityControlOperatingState : byte
{
    /// <summary>
    /// The humidity control system is idle.
    /// </summary>
    Idle = 0x00,

    /// <summary>
    /// The system is humidifying.
    /// </summary>
    Humidifying = 0x01,

    /// <summary>
    /// The system is de-humidifying.
    /// </summary>
    Dehumidifying = 0x02,
}

/// <summary>
/// Commands for the Humidity Control Operating State Command Class.
/// </summary>
public enum HumidityControlOperatingStateCommand : byte
{
    /// <summary>
    /// Request the operating state of the humidity control device.
    /// </summary>
    Get = 0x01,

    /// <summary>
    /// Report the operating state of the humidity control device.
    /// </summary>
    Report = 0x02,
}

/// <summary>
/// Implements the Humidity Control Operating State Command Class (V1).
/// </summary>
[CommandClass(CommandClassId.HumidityControlOperatingState)]
public sealed class HumidityControlOperatingStateCommandClass : CommandClass<HumidityControlOperatingStateCommand>
{
    internal HumidityControlOperatingStateCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last operating state received from the device.
    /// </summary>
    public HumidityControlOperatingState? OperatingState { get; private set; }

    /// <summary>
    /// Event raised when a Humidity Control Operating State Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<HumidityControlOperatingState>? OnOperatingStateReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(HumidityControlOperatingStateCommand command)
        => command switch
        {
            HumidityControlOperatingStateCommand.Get => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the operating state of the humidity control device.
    /// </summary>
    public async Task<HumidityControlOperatingState> GetAsync(CancellationToken cancellationToken)
    {
        HumidityControlOperatingStateGetCommand command = HumidityControlOperatingStateGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<HumidityControlOperatingStateReportCommand>(cancellationToken).ConfigureAwait(false);
        HumidityControlOperatingState state = HumidityControlOperatingStateReportCommand.Parse(reportFrame, Logger);
        OperatingState = state;
        OnOperatingStateReportReceived?.Invoke(state);
        return state;
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((HumidityControlOperatingStateCommand)frame.CommandId)
        {
            case HumidityControlOperatingStateCommand.Report:
            {
                HumidityControlOperatingState state = HumidityControlOperatingStateReportCommand.Parse(frame, Logger);
                OperatingState = state;
                OnOperatingStateReportReceived?.Invoke(state);
                break;
            }
        }
    }

    internal readonly struct HumidityControlOperatingStateGetCommand : ICommand
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

    internal readonly struct HumidityControlOperatingStateReportCommand : ICommand
    {
        public HumidityControlOperatingStateReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlOperatingState;

        public static byte CommandId => (byte)HumidityControlOperatingStateCommand.Report;

        public CommandClassFrame Frame { get; }

        public static HumidityControlOperatingState Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Humidity Control Operating State Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Humidity Control Operating State Report frame is too short");
            }

            return (HumidityControlOperatingState)(frame.CommandParameters.Span[0] & 0x0F);
        }
    }
}
