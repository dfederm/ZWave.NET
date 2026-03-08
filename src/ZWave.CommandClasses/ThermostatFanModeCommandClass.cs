using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The fan mode of a thermostat device.
/// </summary>
public enum ThermostatFanMode : byte
{
    /// <summary>
    /// Auto Low — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// "auto low" algorithms.
    /// </summary>
    AutoLow = 0x00,

    /// <summary>
    /// Low — turns the manual fan operation on at low speed.
    /// </summary>
    Low = 0x01,

    /// <summary>
    /// Auto High — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// "auto high" algorithms.
    /// </summary>
    AutoHigh = 0x02,

    /// <summary>
    /// High — turns the manual fan operation on at high speed.
    /// </summary>
    High = 0x03,

    /// <summary>
    /// Auto Medium — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// "auto medium" algorithms.
    /// </summary>
    AutoMedium = 0x04,

    /// <summary>
    /// Medium — turns the manual fan operation on at medium speed.
    /// </summary>
    Medium = 0x05,

    /// <summary>
    /// Circulation — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// circulation algorithms.
    /// </summary>
    Circulation = 0x06,

    /// <summary>
    /// Humidity Circulation — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// "humidity circulation" algorithms.
    /// </summary>
    HumidityCirculation = 0x07,

    /// <summary>
    /// Left &amp; Right — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// "left &amp; right" circulation algorithms.
    /// </summary>
    LeftRight = 0x08,

    /// <summary>
    /// Up &amp; Down — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// "up &amp; down" circulation algorithms.
    /// </summary>
    UpDown = 0x09,

    /// <summary>
    /// Quiet — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// "quiet" algorithms.
    /// </summary>
    Quiet = 0x0A,

    /// <summary>
    /// External Circulation — turns the manual fan operation off unless turned on by the manufacturer-specific
    /// circulation algorithms. This mode will circulate fresh air from the outside.
    /// </summary>
    ExternalCirculation = 0x0B,
}

public enum ThermostatFanModeCommand : byte
{
    /// <summary>
    /// Set the fan mode in the device.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the fan mode in the device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the fan mode in a device.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported fan modes from the device.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Report the supported fan modes from the device.
    /// </summary>
    SupportedReport = 0x05,
}

/// <summary>
/// Represents a Thermostat Fan Mode Report received from a device.
/// </summary>
public readonly record struct ThermostatFanModeReport(
    /// <summary>
    /// The current fan mode.
    /// </summary>
    ThermostatFanMode FanMode,

    /// <summary>
    /// Indicates whether the fan is fully off. When <c>true</c>, the fan is off regardless of the fan mode.
    /// Added in version 2; version 1 devices always report <c>false</c> (reserved bit).
    /// </summary>
    bool Off);

/// <summary>
/// The Thermostat Fan Mode Command Class is used to control the fan mode of HVAC systems.
/// </summary>
[CommandClass(CommandClassId.ThermostatFanMode)]
public sealed class ThermostatFanModeCommandClass : CommandClass<ThermostatFanModeCommand>
{
    internal ThermostatFanModeCommandClass(
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
    public ThermostatFanModeReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the set of fan modes supported by the device, or <c>null</c> if not yet queried.
    /// </summary>
    public IReadOnlySet<ThermostatFanMode>? SupportedFanModes { get; private set; }

    /// <summary>
    /// Event raised when a Thermostat Fan Mode Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ThermostatFanModeReport>? OnThermostatFanModeReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(ThermostatFanModeCommand command)
        => command switch
        {
            ThermostatFanModeCommand.Set => true,
            ThermostatFanModeCommand.Get => true,
            ThermostatFanModeCommand.SupportedGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current fan mode from the device.
    /// </summary>
    public async Task<ThermostatFanModeReport> GetAsync(CancellationToken cancellationToken)
    {
        ThermostatFanModeGetCommand command = ThermostatFanModeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ThermostatFanModeReportCommand>(cancellationToken).ConfigureAwait(false);
        ThermostatFanModeReport report = ThermostatFanModeReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnThermostatFanModeReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the fan mode in the device.
    /// </summary>
    /// <param name="fanMode">The desired fan mode.</param>
    /// <param name="off">
    /// When <c>true</c>, the fan is switched fully off regardless of the fan mode.
    /// Requires version 2 or later; ignored on version 1 devices.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(
        ThermostatFanMode fanMode,
        bool off,
        CancellationToken cancellationToken)
    {
        var command = ThermostatFanModeSetCommand.Create(EffectiveVersion, fanMode, off);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported fan modes from the device.
    /// </summary>
    public async Task<IReadOnlySet<ThermostatFanMode>> GetSupportedAsync(CancellationToken cancellationToken)
    {
        ThermostatFanModeSupportedGetCommand command = ThermostatFanModeSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ThermostatFanModeSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        HashSet<ThermostatFanMode> supportedModes = ThermostatFanModeSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedFanModes = supportedModes;
        return supportedModes;
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ThermostatFanModeCommand)frame.CommandId)
        {
            case ThermostatFanModeCommand.Report:
            {
                ThermostatFanModeReport report = ThermostatFanModeReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnThermostatFanModeReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct ThermostatFanModeSetCommand : ICommand
    {
        public ThermostatFanModeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ThermostatFanModeSetCommand Create(byte version, ThermostatFanMode fanMode, bool off)
        {
            // V2+ format: bit 7 = Off, bits 6-4 = reserved (0), bits 3-0 = Fan Mode
            // V1 format: bits 7-4 = reserved (MUST be 0), bits 3-0 = Fan Mode
            byte value = (byte)((byte)fanMode & 0x0F);
            if (off && version >= 2)
            {
                value |= 0x80;
            }

            ReadOnlySpan<byte> commandParameters = [value];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ThermostatFanModeSetCommand(frame);
        }
    }

    internal readonly struct ThermostatFanModeGetCommand : ICommand
    {
        public ThermostatFanModeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ThermostatFanModeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatFanModeGetCommand(frame);
        }
    }

    internal readonly struct ThermostatFanModeReportCommand : ICommand
    {
        public ThermostatFanModeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ThermostatFanModeReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Thermostat Fan Mode Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Thermostat Fan Mode Report frame is too short");
            }

            byte value = frame.CommandParameters.Span[0];

            // V2+: bit 7 = Off flag
            // V1: bit 7 is reserved, but per forward-compatibility we parse it unconditionally
            bool off = (value & 0x80) != 0;

            // Bits 3-0 = Fan Mode
            ThermostatFanMode fanMode = (ThermostatFanMode)(value & 0x0F);

            return new ThermostatFanModeReport(fanMode, off);
        }
    }

    internal readonly struct ThermostatFanModeSupportedGetCommand : ICommand
    {
        public ThermostatFanModeSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static ThermostatFanModeSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatFanModeSupportedGetCommand(frame);
        }
    }

    internal readonly struct ThermostatFanModeSupportedReportCommand : ICommand
    {
        public ThermostatFanModeSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static HashSet<ThermostatFanMode> Parse(CommandClassFrame frame, ILogger logger)
        {
            // The bitmask length is determined from the frame length. It may be empty if the
            // device supports no modes (unusual but valid per frame format).
            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span;
            HashSet<ThermostatFanMode> supportedModes = BitMaskHelper.ParseBitMask<ThermostatFanMode>(bitMask);
            return supportedModes;
        }
    }
}
