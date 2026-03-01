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
    /// <summary>
    /// Central Static Controller (CSC).
    /// </summary>
    CentralStaticController = 0x00,

    /// <summary>
    /// Sub Static Controller (SSC).
    /// </summary>
    SubStaticController = 0x01,

    /// <summary>
    /// Portable Controller (PC).
    /// </summary>
    PortableController = 0x02,

    /// <summary>
    /// Reporting Portable Controller (RPC).
    /// </summary>
    ReportingPortableController = 0x03,

    /// <summary>
    /// Portable End Node (PEN).
    /// </summary>
    PortableEndNode = 0x04,

    /// <summary>
    /// Always On End Node (AOEN).
    /// </summary>
    AlwaysOnEndNode = 0x05,

    /// <summary>
    /// Reporting Sleeping End Node (RSEN).
    /// </summary>
    ReportingSleepingEndNode = 0x06,

    /// <summary>
    /// Listening Sleeping End Node (LSEN).
    /// </summary>
    ListeningSleepingEndNode = 0x07,

    /// <summary>
    /// Network Aware End Node (NAEN).
    /// </summary>
    NetworkAwareEndNode = 0x08,

    /// <summary>
    /// Wake On Event End Node (WOEEN).
    /// </summary>
    WakeOnEventEndNode = 0x09,
}

/// <summary>
/// Identifies the Z-Wave Plus node type.
/// </summary>
public enum ZWavePlusNodeType : byte
{
    /// <summary>
    /// Z-Wave Plus node.
    /// </summary>
    Node = 0x00,

    /// <summary>
    /// Z-Wave Plus for IP gateway.
    /// </summary>
    IpGateway = 0x02,
}

/// <summary>
/// Represents a Z-Wave Plus Info Report received from a device.
/// </summary>
public readonly record struct ZWavePlusInfoReport(
    /// <summary>
    /// Enables a future revision of the Z-Wave Plus framework where it is necessary to distinguish
    /// it from the previous frameworks.
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
    /// Indicates the icon to use in Graphical User Interfaces for network management.
    /// </summary>
    ushort InstallerIconType,

    /// <summary>
    /// Indicates the icon to use in Graphical User Interfaces for end users.
    /// </summary>
    ushort UserIconType);

[CommandClass(CommandClassId.ZWavePlusInfo)]
public sealed class ZWavePlusInfoCommandClass : CommandClass<ZWavePlusInfoCommand>
{
    internal ZWavePlusInfoCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last Z-Wave Plus Info Report received from the device.
    /// </summary>
    public ZWavePlusInfoReport? LastReport { get; private set; }

    /// <summary>
    /// Occurs when a Z-Wave Plus Info Report is received.
    /// </summary>
    public event Action<ZWavePlusInfoReport>? OnZWavePlusInfoReportReceived;

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
    public async Task<ZWavePlusInfoReport> GetAsync(CancellationToken cancellationToken)
    {
        ZWavePlusInfoGetCommand command = ZWavePlusInfoGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ZWavePlusInfoReportCommand>(cancellationToken).ConfigureAwait(false);
        ZWavePlusInfoReport report = ZWavePlusInfoReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnZWavePlusInfoReportReceived?.Invoke(report);
        return report;
    }

    internal override CommandClassCategory Category => CommandClassCategory.Management;

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ZWavePlusInfoCommand)frame.CommandId)
        {
            case ZWavePlusInfoCommand.Report:
            {
                ZWavePlusInfoReport report = ZWavePlusInfoReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnZWavePlusInfoReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct ZWavePlusInfoGetCommand : ICommand
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

    internal readonly struct ZWavePlusInfoReportCommand : ICommand
    {
        public ZWavePlusInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ZWavePlusInfo;

        public static byte CommandId => (byte)ZWavePlusInfoCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ZWavePlusInfoReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 7)
            {
                logger.LogWarning("Z-Wave Plus Info Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Z-Wave Plus Info Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            byte zwavePlusVersion = span[0];
            ZWavePlusRoleType roleType = (ZWavePlusRoleType)span[1];
            ZWavePlusNodeType nodeType = (ZWavePlusNodeType)span[2];
            ushort installerIconType = span[3..5].ToUInt16BE();
            ushort userIconType = span[5..7].ToUInt16BE();
            return new ZWavePlusInfoReport(zwavePlusVersion, roleType, nodeType, installerIconType, userIconType);
        }
    }
}
