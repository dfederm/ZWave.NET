using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the type of Z-Wave library.
/// </summary>
public enum ZWaveLibraryType : byte
{
    NotApplicable = 0x00,

    StaticController = 0x01,

    Controller = 0x02,

    EnhancedSlave = 0x03,
    
    Slave = 0x04,
    
    Installer = 0x05,
    
    RoutingSlave = 0x06,
    
    BridgeController = 0x07,
    
    DeviceUnderTest = 0x08,

    NotApplicable2 = 0x09,

    AvRemote = 0x0a,

    AvDevice = 0x0b,
}

public enum VersionCommand : byte
{
    /// <summary>
    /// Request the library type, protocol version and application version from a device that supports
    /// the Version Command Class
    /// </summary>
    Get = 0x11,

    /// <summary>
    /// Advertise the library type, protocol version and application version from a device.
    /// </summary>
    Report = 0x12,

    /// <summary>
    /// Request the individual command class versions from a device.
    /// </summary>
    CommandClassGet = 0x13,

    /// <summary>
    /// Report the individual command class versions from a device.
    /// </summary>
    CommandClassReport = 0x14,

    /// <summary>
    /// Request which version commands are supported by a node.
    /// </summary>
    CapabilitiesGet = 0x15,

    /// <summary>
    /// Advertise the version commands supported by the sending node
    /// </summary>
    CapabilitiesReport = 0x16,

    /// <summary>
    /// Request the detailed Z-Wave chip software version information of a node
    /// </summary>
    ZWaveSoftwareGet = 0x17,

    /// <summary>
    /// Advertise the detailed Z-Wave chip software version information of a node.
    /// </summary>
    ZWaveSoftwareReport = 0x18,
}

/// <summary>
/// Represents the hardware version information of a Z-Wave device.
/// </summary>
public readonly record struct VersionHardwareInfo(
    /// <summary>
    /// The Z-Wave Protocol Library Type
    /// </summary>
    ZWaveLibraryType LibraryType,

    /// <summary>
    /// Advertise information specific to Software Development Kits (SDK) provided by Silicon Labs
    /// </summary>
    Version ProtocolVersion,

    /// <summary>
    /// The firmware versions of the device.
    /// </summary>
    IReadOnlyList<Version> FirmwareVersions,

    /// <summary>
    /// A value which is unique to this particular version of the product
    /// </summary>
    byte? HardwareVersion);

/// <summary>
/// Identifies version capabilities supported by the device.
/// </summary>
[Flags]
public enum VersionCapabilities : byte
{
    /// <summary>
    /// Advertise support for the version information queried with the Version Get Command
    /// </summary>
    Version = 1 << 0,

    /// <summary>
    /// Advertise support for the Command Class version information queried with the Version Command Class Get Command
    /// </summary>
    /// <remarks>
    /// This flag must always be set.
    /// </remarks>
    CommandClass = 1 << 1,

    /// <summary>
    /// Advertise support for the detailed Z-Wave software version information queried with the Version Z-Wave Software
    /// Get Command.
    /// </summary>
    /// <remarks>
    /// This flag must always be set.
    /// </remarks>
    ZWaveSoftware = 1 << 2,
}

/// <summary>
/// Represents the software version information of a Z-Wave device.
/// </summary>
public readonly record struct VersionSoftwareInfo(
    /// <summary>
    /// The SDK version used for building the Z-Wave chip software components for the node.
    /// </summary>
    Version? SdkVersion,

    /// <summary>
    /// The Z-Wave Application Framework API version used by the node
    /// </summary>
    Version? ApplicationFrameworkApiVersion,

    /// <summary>
    /// The Z-Wave Application Framework build number running on the node.
    /// </summary>
    ushort? ApplicationFramworkBuildNumber,

    /// <summary>
    /// The version of the Serial API exposed to a host CPU or a second Chip
    /// </summary>
    Version? HostInterfaceVersion,

    /// <summary>
    /// The build number of the Serial API software exposed to a host CPU or second Chip.
    /// </summary>
    ushort? HostInterfaceBuildNumber,

    /// <summary>
    /// The Z-Wave protocol version used by the node.
    /// </summary>
    Version? ZWaveProtocolVersion,

    /// <summary>
    /// The actual build number of the Z-Wave protocol software used by the node.
    /// </summary>
    ushort? ZWaveProtocolBuildNumber,

    /// <summary>
    /// The version of application software used by the node on its Z-Wave chip.
    /// </summary>
    Version? ApplicationVersion,

    /// <summary>
    /// The actual build of the application software used by the node on its ZWave chip.
    /// </summary>
    ushort? ApplicationBuildNumber);

[CommandClass(CommandClassId.Version)]
public sealed class VersionCommandClass : CommandClass<VersionCommand>
{
    public VersionCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    /// <summary>
    /// Gets the hardware version information.
    /// </summary>
    public VersionHardwareInfo? HardwareInfo { get; private set; }

    /// <summary>
    /// Advertise support for commands
    /// </summary>
    public VersionCapabilities? Capabilities { get; private set; }

    /// <summary>
    /// Gets the software version information.
    /// </summary>
    public VersionSoftwareInfo? SoftwareInfo { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(VersionCommand command)
        => command switch
        {
            VersionCommand.Get => true,
            VersionCommand.CommandClassGet => true,
            VersionCommand.CapabilitiesGet => Version.HasValue ? Version >= 3 : null,
            VersionCommand.ZWaveSoftwareGet => Capabilities.HasValue ? (Capabilities & VersionCapabilities.ZWaveSoftware) != 0 : null,
            _ => false,
        };

    // Overriding since the base class implementation is to depend on this CC.
    internal override CommandClassId[] Dependencies => Array.Empty<CommandClassId>();

    /// <summary>
    /// Request the library type, protocol version and application version from a device that supports
    /// the Version Command Class
    /// </summary>
    public async Task<VersionHardwareInfo> GetAsync(CancellationToken cancellationToken)
    {
        var command = VersionGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionReportCommand>(cancellationToken).ConfigureAwait(false);
        VersionHardwareInfo hardwareInfo = VersionReportCommand.Parse(reportFrame, Logger);
        HardwareInfo = hardwareInfo;
        return hardwareInfo;
    }

    /// <summary>
    /// Request the individual command class versions from a device.
    /// </summary>
    public async Task<byte> GetCommandClassAsync(CommandClassId commandClassId, CancellationToken cancellationToken)
    {
        var command = VersionCommandClassGetCommand.Create(commandClassId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionCommandClassReportCommand>(
            predicate: frame =>
            {
                return frame.CommandParameters.Length > 0
                    && (CommandClassId)frame.CommandParameters.Span[0] == commandClassId;
            },
            cancellationToken).ConfigureAwait(false);
        (CommandClassId _, byte commandClassVersion) = VersionCommandClassReportCommand.Parse(reportFrame, Logger);
        Node.GetCommandClass(commandClassId).SetVersion(commandClassVersion);
        return commandClassVersion;
    }

    /// <summary>
    /// Request which version commands are supported by a node.
    /// </summary>
    public async Task<VersionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var command = VersionCapabilitiesGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionCapabilitiesReportCommand>(cancellationToken).ConfigureAwait(false);
        VersionCapabilities capabilities = VersionCapabilitiesReportCommand.Parse(reportFrame, Logger);
        Capabilities = capabilities;
        return capabilities;
    }

    /// <summary>
    /// Request the detailed Z-Wave chip software version information of a node
    /// </summary>
    public async Task<VersionSoftwareInfo> GetZWaveSoftwareAsync(CancellationToken cancellationToken)
    {
        var command = VersionZWaveSoftwareGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionZWaveSoftwareReportCommand>(cancellationToken).ConfigureAwait(false);
        VersionSoftwareInfo softwareInfo = VersionZWaveSoftwareReportCommand.Parse(reportFrame, Logger);
        SoftwareInfo = softwareInfo;
        return softwareInfo;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Populate the version of every command class the node implements
        foreach (KeyValuePair<CommandClassId, CommandClassInfo> pair in Node.CommandClasses)
        {
            CommandClassId commandClassId = pair.Key;
            _ = await GetCommandClassAsync(commandClassId, cancellationToken).ConfigureAwait(false);
        }

        _ = await GetAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(VersionCommand.CapabilitiesGet).GetValueOrDefault())
        {
            _ = await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (IsCommandSupported(VersionCommand.ZWaveSoftwareGet).GetValueOrDefault())
        {
            _ = await GetZWaveSoftwareAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((VersionCommand)frame.CommandId)
        {
            case VersionCommand.Get:
            case VersionCommand.CommandClassGet:
            case VersionCommand.CapabilitiesGet:
            case VersionCommand.ZWaveSoftwareGet:
            {
                break;
            }
            case VersionCommand.Report:
            {
                HardwareInfo = VersionReportCommand.Parse(frame, Logger);
                break;
            }
            case VersionCommand.CommandClassReport:
            {
                (CommandClassId requestedCommandClass, byte commandClassVersion) = VersionCommandClassReportCommand.Parse(frame, Logger);
                CommandClass commandClass = Node.GetCommandClass(requestedCommandClass);
                commandClass.SetVersion(commandClassVersion);
                break;
            }
            case VersionCommand.CapabilitiesReport:
            {
                Capabilities = VersionCapabilitiesReportCommand.Parse(frame, Logger);
                break;
            }
            case VersionCommand.ZWaveSoftwareReport:
            {
                SoftwareInfo = VersionZWaveSoftwareReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct VersionGetCommand : ICommand
    {
        public VersionGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.Get;

        public CommandClassFrame Frame { get; }

        public static VersionGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new VersionGetCommand(frame);
        }
    }

    private readonly struct VersionReportCommand : ICommand
    {
        public VersionReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.Report;

        public CommandClassFrame Frame { get; }

        public static VersionHardwareInfo Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 5)
            {
                logger.LogWarning("Version Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Version Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            ZWaveLibraryType libraryType = (ZWaveLibraryType)span[0];
            Version protocolVersion = new Version(span[1], span[2]);

            byte? hardwareVersion = span.Length > 5 ? span[5] : null;

            int numFirmwareVersions = 1;
            if (span.Length > 6)
            {
                numFirmwareVersions += span[6];
            }

            Version[] firmwareVersions = new Version[numFirmwareVersions];
            firmwareVersions[0] = new Version(span[3], span[4]);

            for (int i = 1; i < numFirmwareVersions; i++)
            {
                // The starting offset should be 7, but account for i starting at 1
                int versionOffset = 5 + (2 * i);
                firmwareVersions[i] = new Version(span[versionOffset], span[versionOffset + 1]);
            }

            return new VersionHardwareInfo(libraryType, protocolVersion, firmwareVersions, hardwareVersion);
        }
    }

    private readonly struct VersionCommandClassGetCommand : ICommand
    {
        public VersionCommandClassGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.CommandClassGet;

        public CommandClassFrame Frame { get; }

        public static VersionGetCommand Create(CommandClassId commandClassId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)commandClassId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new VersionGetCommand(frame);
        }
    }

    private readonly struct VersionCommandClassReportCommand : ICommand
    {
        public VersionCommandClassReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.CommandClassReport;

        public CommandClassFrame Frame { get; }

        public static (CommandClassId RequestedCommandClass, byte CommandClassVersion) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Version Command Class Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Version Command Class Report frame is too short");
            }

            return ((CommandClassId)frame.CommandParameters.Span[0], frame.CommandParameters.Span[1]);
        }
    }

    private readonly struct VersionCapabilitiesGetCommand : ICommand
    {
        public VersionCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.CapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static VersionCapabilitiesGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new VersionCapabilitiesGetCommand(frame);
        }
    }

    private readonly struct VersionCapabilitiesReportCommand : ICommand
    {
        public VersionCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.CapabilitiesReport;

        public CommandClassFrame Frame { get; }

        public static VersionCapabilities Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Version Capabilities Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Version Capabilities Report frame is too short");
            }

            return (VersionCapabilities)frame.CommandParameters.Span[0];
        }
    }

    private readonly struct VersionZWaveSoftwareGetCommand : ICommand
    {
        public VersionZWaveSoftwareGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.ZWaveSoftwareGet;

        public CommandClassFrame Frame { get; }

        public static VersionZWaveSoftwareGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new VersionZWaveSoftwareGetCommand(frame);
        }
    }

    private readonly struct VersionZWaveSoftwareReportCommand : ICommand
    {
        public VersionZWaveSoftwareReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.ZWaveSoftwareReport;

        public CommandClassFrame Frame { get; }

        public static VersionSoftwareInfo Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 23)
            {
                logger.LogWarning("Version Z-Wave Software Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Version Z-Wave Software Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            return new VersionSoftwareInfo(
                ParseVersion(span[0..3]),
                ParseVersion(span[3..6]),
                ParseBuildNumber(span[6..8]),
                ParseVersion(span[8..11]),
                ParseBuildNumber(span[11..13]),
                ParseVersion(span[13..16]),
                ParseBuildNumber(span[16..18]),
                ParseVersion(span[18..21]),
                ParseBuildNumber(span[21..23]));
        }

        private static Version? ParseVersion(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 3)
            {
                throw new ArgumentException("Expected exactly 3 bytes", nameof(bytes));
            }

            byte major = bytes[0];
            byte minor = bytes[1];
            byte patch = bytes[2];

            // The value 0 MUST indicate that this field is unused.
            return major == 0 && minor == 0 && patch == 0
                ? null
                : new Version(major, minor, patch);
        }

        private static ushort? ParseBuildNumber(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 2)
            {
                throw new ArgumentException("Expected exactly 2 bytes", nameof(bytes));
            }

            ushort buildNum = bytes.ToUInt16BE();

            // The value 0 MUST indicate that this field is unused
            return buildNum != 0
                ? buildNum
                : null;
        }
    }
}
