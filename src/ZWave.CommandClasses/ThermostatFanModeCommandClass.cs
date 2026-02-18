namespace ZWave.CommandClasses;

public enum ThermostatFanModeCommand : byte
{
    /// <summary>
    /// Set the fan mode in the device.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the fan mode from a node.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current fan mode at the sending node.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported fan modes from the device.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Advertise the supported fan modes of the device.
    /// </summary>
    SupportedReport = 0x05,
}

/// <summary>
/// Identifies the fan mode of a thermostat.
/// </summary>
public enum ThermostatFanMode : byte
{
    AutoLow = 0x00,

    Low = 0x01,

    AutoHigh = 0x02,

    High = 0x03,

    AutoMedium = 0x04,

    Medium = 0x05,

    Circulation = 0x06,

    HumidityCirculation = 0x07,

    LeftRight = 0x08,

    UpDown = 0x09,

    Quiet = 0x0A,

    ExternalCirculation = 0x0B,
}

[CommandClass(CommandClassId.ThermostatFanMode)]
public sealed class ThermostatFanModeCommandClass : CommandClass<ThermostatFanModeCommand>
{
    internal ThermostatFanModeCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the current fan mode.
    /// </summary>
    public ThermostatFanMode? CurrentMode { get; private set; }

    /// <summary>
    /// Gets the supported fan modes.
    /// </summary>
    public IReadOnlySet<ThermostatFanMode>? SupportedModes { get; private set; }

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
        _ = await GetSupportedModesAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current fan mode from a node.
    /// </summary>
    public async Task<ThermostatFanMode> GetAsync(CancellationToken cancellationToken)
    {
        var command = ThermostatFanModeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatFanModeReportCommand>(cancellationToken).ConfigureAwait(false);
        return CurrentMode!.Value;
    }

    /// <summary>
    /// Set the fan mode in the device.
    /// </summary>
    public async Task SetAsync(
        ThermostatFanMode mode,
        bool off,
        CancellationToken cancellationToken)
    {
        var command = ThermostatFanModeSetCommand.Create(EffectiveVersion, mode, off);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported fan modes from the device.
    /// </summary>
    public async Task<IReadOnlySet<ThermostatFanMode>> GetSupportedModesAsync(CancellationToken cancellationToken)
    {
        var command = ThermostatFanModeSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatFanModeSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedModes!;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ThermostatFanModeCommand)frame.CommandId)
        {
            case ThermostatFanModeCommand.Set:
            case ThermostatFanModeCommand.Get:
            case ThermostatFanModeCommand.SupportedGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ThermostatFanModeCommand.Report:
            {
                var command = new ThermostatFanModeReportCommand(frame);
                CurrentMode = command.Mode;
                break;
            }
            case ThermostatFanModeCommand.SupportedReport:
            {
                var command = new ThermostatFanModeSupportedReportCommand(frame);
                SupportedModes = command.SupportedModes;
                break;
            }
        }
    }

    private readonly struct ThermostatFanModeSetCommand : ICommand
    {
        public ThermostatFanModeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ThermostatFanModeSetCommand Create(byte version, ThermostatFanMode mode, bool off)
        {
            byte modeByte = (byte)((byte)mode & 0x0F);
            if (version >= 3 && off)
            {
                modeByte |= 0x80;
            }

            Span<byte> commandParameters = stackalloc byte[1];
            commandParameters[0] = modeByte;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ThermostatFanModeSetCommand(frame);
        }
    }

    private readonly struct ThermostatFanModeGetCommand : ICommand
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

    private readonly struct ThermostatFanModeReportCommand : ICommand
    {
        public ThermostatFanModeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current fan mode.
        /// </summary>
        public ThermostatFanMode Mode => (ThermostatFanMode)(Frame.CommandParameters.Span[0] & 0x0F);
    }

    private readonly struct ThermostatFanModeSupportedGetCommand : ICommand
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

    private readonly struct ThermostatFanModeSupportedReportCommand : ICommand
    {
        public ThermostatFanModeSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatFanMode;

        public static byte CommandId => (byte)ThermostatFanModeCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The supported fan modes.
        /// </summary>
        public IReadOnlySet<ThermostatFanMode> SupportedModes
        {
            get
            {
                var supportedModes = new HashSet<ThermostatFanMode>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            ThermostatFanMode mode = (ThermostatFanMode)((byteNum << 3) + bitNum);
                            supportedModes.Add(mode);
                        }
                    }
                }

                return supportedModes;
            }
        }
    }
}
