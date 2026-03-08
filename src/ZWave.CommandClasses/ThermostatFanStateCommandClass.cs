using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The fan operating state of a thermostat device.
/// </summary>
public enum ThermostatFanOperatingState : byte
{
    /// <summary>
    /// The fan is idle or off.
    /// </summary>
    Idle = 0x00,

    /// <summary>
    /// The fan is running. For single-speed devices, this indicates the fan is running.
    /// For multi-speed devices, this indicates the fan is running at low speed.
    /// </summary>
    RunningLow = 0x01,

    /// <summary>
    /// The fan is running at high speed.
    /// </summary>
    RunningHigh = 0x02,

    /// <summary>
    /// The fan is running at medium speed.
    /// </summary>
    RunningMedium = 0x03,

    /// <summary>
    /// The fan is in circulation mode.
    /// </summary>
    CirculationMode = 0x04,

    /// <summary>
    /// The fan is in humidity circulation mode.
    /// </summary>
    HumidityCirculationMode = 0x05,

    /// <summary>
    /// The fan is in right-left circulation mode.
    /// </summary>
    RightLeftCirculationMode = 0x06,

    /// <summary>
    /// The fan is in up-down circulation mode.
    /// </summary>
    UpDownCirculationMode = 0x07,

    /// <summary>
    /// The fan is in quiet circulation mode.
    /// </summary>
    QuietCirculationMode = 0x08,
}

public enum ThermostatFanStateCommand : byte
{
    /// <summary>
    /// Request the fan operating state from the device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the fan operating state of the device.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents a Thermostat Fan State Report received from a device.
/// </summary>
public readonly record struct ThermostatFanStateReport(
    /// <summary>
    /// The current fan operating state.
    /// </summary>
    ThermostatFanOperatingState FanOperatingState);

/// <summary>
/// The Thermostat Fan State Command Class is used to obtain the fan operating state of the thermostat.
/// </summary>
[CommandClass(CommandClassId.ThermostatFanState)]
public sealed class ThermostatFanStateCommandClass : CommandClass<ThermostatFanStateCommand>
{
    internal ThermostatFanStateCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public ThermostatFanStateReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Thermostat Fan State Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ThermostatFanStateReport>? OnThermostatFanStateReportReceived;

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
    /// Request the fan operating state from the device.
    /// </summary>
    public async Task<ThermostatFanStateReport> GetAsync(CancellationToken cancellationToken)
    {
        ThermostatFanStateGetCommand command = ThermostatFanStateGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ThermostatFanStateReportCommand>(cancellationToken).ConfigureAwait(false);
        ThermostatFanStateReport report = ThermostatFanStateReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnThermostatFanStateReportReceived?.Invoke(report);
        return report;
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ThermostatFanStateCommand)frame.CommandId)
        {
            case ThermostatFanStateCommand.Report:
            {
                ThermostatFanStateReport report = ThermostatFanStateReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnThermostatFanStateReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct ThermostatFanStateGetCommand : ICommand
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

    internal readonly struct ThermostatFanStateReportCommand : ICommand
    {
        public ThermostatFanStateReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanState;

        public static byte CommandId => (byte)ThermostatFanStateCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ThermostatFanStateReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Thermostat Fan State Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Thermostat Fan State Report frame is too short");
            }

            // V1: 4-bit field (bits 0-3), upper 4 bits reserved
            // V2: full 8-bit field (adds states 3-8)
            // Per forward-compatibility rules, do NOT mask reserved bits — parse all 8 bits.
            ThermostatFanOperatingState fanOperatingState = (ThermostatFanOperatingState)frame.CommandParameters.Span[0];
            return new ThermostatFanStateReport(fanOperatingState);
        }
    }
}
