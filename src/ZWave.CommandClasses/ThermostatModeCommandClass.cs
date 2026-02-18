namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the mode of a thermostat.
/// </summary>
public enum ThermostatMode : byte
{
    /// <summary>
    /// The thermostat is off.
    /// </summary>
    Off = 0x00,

    /// <summary>
    /// The thermostat is in heat mode.
    /// </summary>
    Heat = 0x01,

    /// <summary>
    /// The thermostat is in cool mode.
    /// </summary>
    Cool = 0x02,

    /// <summary>
    /// The thermostat is in auto mode.
    /// </summary>
    Auto = 0x03,

    /// <summary>
    /// The thermostat is in auxiliary/emergency heat mode.
    /// </summary>
    Auxiliary = 0x04,

    /// <summary>
    /// Resume from the last saved mode.
    /// </summary>
    Resume = 0x05,

    /// <summary>
    /// The thermostat is in fan only mode.
    /// </summary>
    FanOnly = 0x06,

    /// <summary>
    /// The thermostat is in furnace mode.
    /// </summary>
    Furnace = 0x07,

    /// <summary>
    /// The thermostat is in dry air mode.
    /// </summary>
    DryAir = 0x08,

    /// <summary>
    /// The thermostat is in moist air mode.
    /// </summary>
    MoistAir = 0x09,

    /// <summary>
    /// The thermostat is in auto changeover mode.
    /// </summary>
    AutoChangeover = 0x0A,

    /// <summary>
    /// The thermostat is in energy save heat mode.
    /// </summary>
    EnergySaveHeat = 0x0B,

    /// <summary>
    /// The thermostat is in energy save cool mode.
    /// </summary>
    EnergySaveCool = 0x0C,

    /// <summary>
    /// The thermostat is in away mode.
    /// </summary>
    Away = 0x0D,

    /// <summary>
    /// The thermostat is in full power mode.
    /// </summary>
    FullPower = 0x0F,

    /// <summary>
    /// The thermostat is in a manufacturer specific mode.
    /// </summary>
    ManufacturerSpecific = 0x1F,
}

public enum ThermostatModeCommand : byte
{
    /// <summary>
    /// Set the mode of the thermostat.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current mode from the thermostat.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current mode of the thermostat.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported modes from the thermostat.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Indicates the supported modes of the thermostat.
    /// </summary>
    SupportedReport = 0x05,
}

[CommandClass(CommandClassId.ThermostatMode)]
public sealed class ThermostatModeCommandClass : CommandClass<ThermostatModeCommand>
{
    internal ThermostatModeCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported thermostat mode.
    /// </summary>
    public ThermostatMode? Mode { get; private set; }

    /// <summary>
    /// Gets the supported thermostat modes.
    /// </summary>
    public IReadOnlySet<ThermostatMode>? SupportedModes { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ThermostatModeCommand command)
        => command switch
        {
            ThermostatModeCommand.Set => true,
            ThermostatModeCommand.Get => true,
            ThermostatModeCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(ThermostatModeCommand.SupportedGet).GetValueOrDefault())
        {
            _ = await GetSupportedModesAsync(cancellationToken).ConfigureAwait(false);
        }

        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current mode from the thermostat.
    /// </summary>
    public async Task<ThermostatMode> GetAsync(CancellationToken cancellationToken)
    {
        var command = ThermostatModeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatModeReportCommand>(cancellationToken).ConfigureAwait(false);
        return Mode!.Value;
    }

    /// <summary>
    /// Set the mode of the thermostat.
    /// </summary>
    public async Task SetAsync(ThermostatMode mode, CancellationToken cancellationToken)
    {
        var command = ThermostatModeSetCommand.Create(mode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported modes from the thermostat.
    /// </summary>
    public async Task<IReadOnlySet<ThermostatMode>> GetSupportedModesAsync(CancellationToken cancellationToken)
    {
        var command = ThermostatModeSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatModeSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedModes!;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ThermostatModeCommand)frame.CommandId)
        {
            case ThermostatModeCommand.Set:
            case ThermostatModeCommand.Get:
            case ThermostatModeCommand.SupportedGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ThermostatModeCommand.Report:
            {
                var command = new ThermostatModeReportCommand(frame);
                Mode = command.Mode;
                break;
            }
            case ThermostatModeCommand.SupportedReport:
            {
                var command = new ThermostatModeSupportedReportCommand(frame);
                SupportedModes = command.SupportedModes;
                break;
            }
        }
    }

    private readonly struct ThermostatModeSetCommand : ICommand
    {
        public ThermostatModeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatMode;

        public static byte CommandId => (byte)ThermostatModeCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ThermostatModeSetCommand Create(ThermostatMode mode)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)mode & 0x1F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ThermostatModeSetCommand(frame);
        }
    }

    private readonly struct ThermostatModeGetCommand : ICommand
    {
        public ThermostatModeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatMode;

        public static byte CommandId => (byte)ThermostatModeCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ThermostatModeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatModeGetCommand(frame);
        }
    }

    private readonly struct ThermostatModeReportCommand : ICommand
    {
        public ThermostatModeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatMode;

        public static byte CommandId => (byte)ThermostatModeCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current thermostat mode.
        /// </summary>
        public ThermostatMode Mode => (ThermostatMode)(Frame.CommandParameters.Span[0] & 0x1F);
    }

    private readonly struct ThermostatModeSupportedGetCommand : ICommand
    {
        public ThermostatModeSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatMode;

        public static byte CommandId => (byte)ThermostatModeCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static ThermostatModeSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatModeSupportedGetCommand(frame);
        }
    }

    private readonly struct ThermostatModeSupportedReportCommand : ICommand
    {
        public ThermostatModeSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatMode;

        public static byte CommandId => (byte)ThermostatModeCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The supported thermostat modes.
        /// </summary>
        public IReadOnlySet<ThermostatMode> SupportedModes
        {
            get
            {
                var supportedModes = new HashSet<ThermostatMode>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            ThermostatMode mode = (ThermostatMode)((byteNum << 3) + bitNum);
                            supportedModes.Add(mode);
                        }
                    }
                }

                return supportedModes;
            }
        }
    }
}
