using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public enum ZWavePlusInfoCommand : byte
{
    /// <summary>
    /// Get additional information of the Z-Wave Plus device in question.
    /// </summary>
    Get = 0x01,

    /// <summary>
    /// Report version of Z-Wave Plus framework used and additional information of the Z-Wave Plus device in question.
    /// </summary>
    Report = 0x02,
}

/// <summary>
/// Identifies the Z-Wave Plus role type of a node.
/// </summary>
public enum ZWavePlusRoleType : byte
{
    CentralStaticController = 0x00,
    SubStaticController = 0x01,
    PortableController = 0x02,
    ReportingPortableController = 0x03,
    PortableSlave = 0x04,
    AlwaysOnSlave = 0x05,
    ReportingSleepingSlave = 0x06,
    ListeningSleepingSlave = 0x07,
    NetworkAwareSlave = 0x08,
}

/// <summary>
/// Represents Z-Wave Plus information for a device.
/// </summary>
public readonly record struct ZWavePlusInfo(
    /// <summary>
    /// Enables a future revision of the Z-Wave Plus framework where it is necessary to distinguish it from the previous frameworks
    /// </summary>
    byte ZWavePlusVersion,

    /// <summary>
    /// Indicates the role the Z-Wave Plus device in question possess in the network and functionalities supported.
    /// </summary>
    ZWavePlusRoleType RoleType,

    /// <summary>
    /// Indicates the type of node the Z-Wave Plus device in question possess in the network.
    /// </summary>
    ZWavePlusNodeType NodeType,

    /// <summary>
    /// Indicates the icon to use in Graphical User Interfaces for network management
    /// </summary>
    ushort InstallerIconType,

    /// <summary>
    /// Indicates the icon to use in Graphical User Interfaces for end users
    /// </summary>
    ushort UserIconType);

/// <summary>
/// Identifies the Z-Wave Plus node type.
/// </summary>
public enum ZWavePlusNodeType : byte
{
    /// <summary>
    /// Z-Wave Plus node
    /// </summary>
    Node = 0x00,

    /// <summary>
    /// Z-Wave Plus for IP gateway
    /// </summary>
    IpGateway = 0x02,
}

[CommandClass(CommandClassId.ZWavePlusInfo)]
public sealed class ZWavePlusInfoCommandClass : CommandClass<ZWavePlusInfoCommand>
{
    internal ZWavePlusInfoCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    /// <summary>
    /// Gets the Z-Wave Plus information.
    /// </summary>
    public ZWavePlusInfo? ZWavePlusInfo { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ZWavePlusInfoCommand command)
        => command switch
        {
            ZWavePlusInfoCommand.Get => true,
            _ => false,
        };

    /// <summary>
    /// Get additional information of the Z-Wave Plus device in question.
    /// </summary>
    public async Task<ZWavePlusInfo> GetAsync(CancellationToken cancellationToken)
    {
        ZWavePlusInfoGetCommand command = ZWavePlusInfoGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ZWavePlusInfoReportCommand>(cancellationToken).ConfigureAwait(false);
        ZWavePlusInfo info = ZWavePlusInfoReportCommand.Parse(reportFrame, Logger);
        ZWavePlusInfo = info;
        return info;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ZWavePlusInfoCommand)frame.CommandId)
        {
            case ZWavePlusInfoCommand.Get:
            {
                break;
            }
            case ZWavePlusInfoCommand.Report:
            {
                ZWavePlusInfo = ZWavePlusInfoReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct ZWavePlusInfoGetCommand : ICommand
    {
        public ZWavePlusInfoGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ZWavePlusInfo;

        public static byte CommandId => (byte)ZWavePlusInfoCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ZWavePlusInfoGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ZWavePlusInfoGetCommand(frame);
        }
    }

    private readonly struct ZWavePlusInfoReportCommand : ICommand
    {
        public ZWavePlusInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ZWavePlusInfo;

        public static byte CommandId => (byte)ZWavePlusInfoCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ZWavePlusInfo Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 7)
            {
                logger.LogWarning("Z-Wave Plus Info Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Z-Wave Plus Info Report frame is too short");
            }

            byte zwavePlusVersion = frame.CommandParameters.Span[0];
            ZWavePlusRoleType roleType = (ZWavePlusRoleType)frame.CommandParameters.Span[1];
            ZWavePlusNodeType nodeType = (ZWavePlusNodeType)frame.CommandParameters.Span[2];
            ushort installerIconType = frame.CommandParameters.Span[3..5].ToUInt16BE();
            ushort userIconType = frame.CommandParameters.Span[5..7].ToUInt16BE();
            return new ZWavePlusInfo(zwavePlusVersion, roleType, nodeType, installerIconType, userIconType);
        }
    }
}
