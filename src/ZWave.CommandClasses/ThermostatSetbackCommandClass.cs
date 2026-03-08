using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The setback type for a thermostat.
/// </summary>
public enum ThermostatSetbackType : byte
{
    /// <summary>
    /// No override.
    /// </summary>
    NoOverride = 0x00,

    /// <summary>
    /// Temporary override. If a timer is implemented in the device, the override will be terminated
    /// by the timer. If no timer is implemented, this acts as a permanent override.
    /// </summary>
    TemporaryOverride = 0x01,

    /// <summary>
    /// Permanent override.
    /// </summary>
    PermanentOverride = 0x02,
}

/// <summary>
/// The setback state for a thermostat, representing a temperature offset in 1/10 degree Kelvin steps,
/// or a special state such as frost protection or energy saving mode.
/// </summary>
public readonly record struct ThermostatSetbackState
{
    /// <summary>
    /// The raw setback state value as a signed byte.
    /// Values -128 to 120 represent temperature setback in 1/10 degree Kelvin steps.
    /// Value 121 (0x79) represents Frost Protection.
    /// Value 122 (0x7A) represents Energy Saving Mode.
    /// Value 127 (0x7F) represents Unused State.
    /// </summary>
    public sbyte RawValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThermostatSetbackState"/> struct with a raw value.
    /// </summary>
    public ThermostatSetbackState(sbyte rawValue)
    {
        RawValue = rawValue;
    }

    /// <summary>
    /// Gets the Frost Protection setback state (0x79 = 121).
    /// </summary>
    public static ThermostatSetbackState FrostProtection => new(121);

    /// <summary>
    /// Gets the Energy Saving Mode setback state (0x7A = 122).
    /// </summary>
    public static ThermostatSetbackState EnergySavingMode => new(122);

    /// <summary>
    /// Gets the Unused State setback state (0x7F = 127).
    /// </summary>
    public static ThermostatSetbackState UnusedState => new(127);

    /// <summary>
    /// Gets whether this state represents a temperature setback value (as opposed to a special state).
    /// </summary>
    public bool IsTemperatureSetback => RawValue >= -128 && RawValue <= 120;

    /// <summary>
    /// Gets the temperature setback in degrees Kelvin, or <c>null</c> if this is a special state.
    /// </summary>
    public decimal? TemperatureSetbackKelvin => IsTemperatureSetback ? RawValue / 10m : null;
}

public enum ThermostatSetbackCommand : byte
{
    /// <summary>
    /// Set the setback state of the thermostat.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current setback state of the thermostat.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the current setback state of the thermostat.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents a Thermostat Setback Report received from a device.
/// </summary>
public readonly record struct ThermostatSetbackReport(
    /// <summary>
    /// The current setback type.
    /// </summary>
    ThermostatSetbackType SetbackType,

    /// <summary>
    /// The current setback state.
    /// </summary>
    ThermostatSetbackState SetbackState);

/// <summary>
/// The Thermostat Setback Command Class is used to change the current state of a non-schedule
/// setback thermostat.
/// </summary>
[CommandClass(CommandClassId.ThermostatSetback)]
public sealed class ThermostatSetbackCommandClass : CommandClass<ThermostatSetbackCommand>
{
    internal ThermostatSetbackCommandClass(
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
    public ThermostatSetbackReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Thermostat Setback Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ThermostatSetbackReport>? OnThermostatSetbackReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(ThermostatSetbackCommand command)
        => command switch
        {
            ThermostatSetbackCommand.Set => true,
            ThermostatSetbackCommand.Get => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current setback state from the device.
    /// </summary>
    public async Task<ThermostatSetbackReport> GetAsync(CancellationToken cancellationToken)
    {
        ThermostatSetbackGetCommand command = ThermostatSetbackGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ThermostatSetbackReportCommand>(cancellationToken).ConfigureAwait(false);
        ThermostatSetbackReport report = ThermostatSetbackReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnThermostatSetbackReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the setback state of the thermostat.
    /// </summary>
    public async Task SetAsync(
        ThermostatSetbackType setbackType,
        ThermostatSetbackState setbackState,
        CancellationToken cancellationToken)
    {
        var command = ThermostatSetbackSetCommand.Create(setbackType, setbackState);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ThermostatSetbackCommand)frame.CommandId)
        {
            case ThermostatSetbackCommand.Report:
            {
                ThermostatSetbackReport report = ThermostatSetbackReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnThermostatSetbackReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct ThermostatSetbackSetCommand : ICommand
    {
        public ThermostatSetbackSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetback;

        public static byte CommandId => (byte)ThermostatSetbackCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ThermostatSetbackSetCommand Create(
            ThermostatSetbackType setbackType,
            ThermostatSetbackState setbackState)
        {
            // Byte 0: bits 7-2 = reserved (0), bits 1-0 = Setback Type
            // Byte 1: Setback State (signed byte)
            ReadOnlySpan<byte> commandParameters =
            [
                (byte)((byte)setbackType & 0b0000_0011),
                (byte)setbackState.RawValue,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ThermostatSetbackSetCommand(frame);
        }
    }

    internal readonly struct ThermostatSetbackGetCommand : ICommand
    {
        public ThermostatSetbackGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetback;

        public static byte CommandId => (byte)ThermostatSetbackCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ThermostatSetbackGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatSetbackGetCommand(frame);
        }
    }

    internal readonly struct ThermostatSetbackReportCommand : ICommand
    {
        public ThermostatSetbackReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetback;

        public static byte CommandId => (byte)ThermostatSetbackCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ThermostatSetbackReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Thermostat Setback Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Thermostat Setback Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            // Byte 0: bits 7-2 = reserved, bits 1-0 = Setback Type
            ThermostatSetbackType setbackType = (ThermostatSetbackType)(span[0] & 0b0000_0011);

            // Byte 1: Setback State (signed byte, -128 to 127)
            ThermostatSetbackState setbackState = new((sbyte)span[1]);

            return new ThermostatSetbackReport(setbackType, setbackState);
        }
    }
}
