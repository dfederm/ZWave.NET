namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the humidity control mode.
/// </summary>
public enum HumidityControlMode : byte
{
    /// <summary>
    /// Humidity control is off.
    /// </summary>
    Off = 0x00,

    /// <summary>
    /// The device is in humidify mode.
    /// </summary>
    Humidify = 0x01,

    /// <summary>
    /// The device is in dehumidify mode.
    /// </summary>
    Dehumidify = 0x02,

    /// <summary>
    /// The device is in auto mode.
    /// </summary>
    Auto = 0x03,
}

public enum HumidityControlModeCommand : byte
{
    /// <summary>
    /// Set the humidity control mode.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current humidity control mode.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current humidity control mode.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported humidity control modes.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Advertise the supported humidity control modes.
    /// </summary>
    SupportedReport = 0x05,
}

[CommandClass(CommandClassId.HumidityControlMode)]
public sealed class HumidityControlModeCommandClass : CommandClass<HumidityControlModeCommand>
{
    internal HumidityControlModeCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported humidity control mode.
    /// </summary>
    public HumidityControlMode? Mode { get; private set; }

    /// <summary>
    /// Gets the supported humidity control modes.
    /// </summary>
    public IReadOnlySet<HumidityControlMode>? SupportedModes { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(HumidityControlModeCommand command)
        => command switch
        {
            HumidityControlModeCommand.Set => true,
            HumidityControlModeCommand.Get => true,
            HumidityControlModeCommand.SupportedGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetSupportedModesAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current humidity control mode.
    /// </summary>
    public async Task<HumidityControlMode> GetAsync(CancellationToken cancellationToken)
    {
        var command = HumidityControlModeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<HumidityControlModeReportCommand>(cancellationToken).ConfigureAwait(false);
        return Mode!.Value;
    }

    /// <summary>
    /// Set the humidity control mode.
    /// </summary>
    public async Task SetAsync(HumidityControlMode mode, CancellationToken cancellationToken)
    {
        var command = HumidityControlModeSetCommand.Create(mode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported humidity control modes.
    /// </summary>
    public async Task<IReadOnlySet<HumidityControlMode>> GetSupportedModesAsync(CancellationToken cancellationToken)
    {
        var command = HumidityControlModeSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<HumidityControlModeSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedModes!;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((HumidityControlModeCommand)frame.CommandId)
        {
            case HumidityControlModeCommand.Set:
            case HumidityControlModeCommand.Get:
            case HumidityControlModeCommand.SupportedGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case HumidityControlModeCommand.Report:
            {
                var command = new HumidityControlModeReportCommand(frame);
                Mode = command.Mode;
                break;
            }
            case HumidityControlModeCommand.SupportedReport:
            {
                var command = new HumidityControlModeSupportedReportCommand(frame);
                SupportedModes = command.SupportedModes;
                break;
            }
        }
    }

    private readonly struct HumidityControlModeSetCommand : ICommand
    {
        public HumidityControlModeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.Set;

        public CommandClassFrame Frame { get; }

        public static HumidityControlModeSetCommand Create(HumidityControlMode mode)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)mode & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlModeSetCommand(frame);
        }
    }

    private readonly struct HumidityControlModeGetCommand : ICommand
    {
        public HumidityControlModeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.Get;

        public CommandClassFrame Frame { get; }

        public static HumidityControlModeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new HumidityControlModeGetCommand(frame);
        }
    }

    private readonly struct HumidityControlModeReportCommand : ICommand
    {
        public HumidityControlModeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current humidity control mode.
        /// </summary>
        public HumidityControlMode Mode => (HumidityControlMode)(Frame.CommandParameters.Span[0] & 0x0F);
    }

    private readonly struct HumidityControlModeSupportedGetCommand : ICommand
    {
        public HumidityControlModeSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static HumidityControlModeSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new HumidityControlModeSupportedGetCommand(frame);
        }
    }

    private readonly struct HumidityControlModeSupportedReportCommand : ICommand
    {
        public HumidityControlModeSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The supported humidity control modes.
        /// </summary>
        public IReadOnlySet<HumidityControlMode> SupportedModes
        {
            get
            {
                var supportedModes = new HashSet<HumidityControlMode>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            HumidityControlMode mode = (HumidityControlMode)((byteNum << 3) + bitNum);
                            supportedModes.Add(mode);
                        }
                    }
                }

                return supportedModes;
            }
        }
    }
}
