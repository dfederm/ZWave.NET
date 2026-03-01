using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands supported by the Version Command Class.
/// </summary>
public enum VersionCommand : byte
{
    /// <summary>
    /// Request the library type, protocol version and application version from a device that supports
    /// the Version Command Class.
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
    /// Advertise the version commands supported by the sending node.
    /// </summary>
    CapabilitiesReport = 0x16,

    /// <summary>
    /// Request the detailed Z-Wave chip software version information of a node.
    /// </summary>
    ZWaveSoftwareGet = 0x17,

    /// <summary>
    /// Advertise the detailed Z-Wave chip software version information of a node.
    /// </summary>
    ZWaveSoftwareReport = 0x18,

    /// <summary>
    /// Request the migration capabilities of a supporting node.
    /// </summary>
    MigrationCapabilitiesGet = 0x19,

    /// <summary>
    /// Advertise which migration operations are supported by the sending node.
    /// </summary>
    MigrationCapabilitiesReport = 0x1A,

    /// <summary>
    /// Trigger a specific migration operation.
    /// </summary>
    MigrationSet = 0x1B,

    /// <summary>
    /// Request the status of a migration operation.
    /// </summary>
    MigrationGet = 0x1C,

    /// <summary>
    /// Report the status of a migration operation.
    /// </summary>
    MigrationReport = 0x1D,
}

/// <summary>
/// Implements the Version Command Class (V4).
/// </summary>
[CommandClass(CommandClassId.Version)]
public sealed partial class VersionCommandClass : CommandClass<VersionCommand>
{
    internal VersionCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(VersionCommand command)
        => command switch
        {
            VersionCommand.Get => true,
            VersionCommand.CommandClassGet => true,
            VersionCommand.CapabilitiesGet => Version.HasValue ? Version >= 3 : null,
            VersionCommand.ZWaveSoftwareGet => Capabilities.HasValue
                ? (Capabilities & VersionCapabilities.ZWaveSoftware) != 0
                : null,
            VersionCommand.MigrationCapabilitiesGet => Capabilities.HasValue
                ? (Capabilities & VersionCapabilities.MigrationSupport) != 0
                : null,
            VersionCommand.MigrationSet => Capabilities.HasValue
                ? (Capabilities & VersionCapabilities.MigrationSupport) != 0
                : null,
            VersionCommand.MigrationGet => Capabilities.HasValue
                ? (Capabilities & VersionCapabilities.MigrationSupport) != 0
                : null,
            _ => false,
        };

    // Overriding since the base class implementation is to depend on this CC.
    internal override CommandClassId[] Dependencies => [];

    internal override CommandClassCategory Category => CommandClassCategory.Management;

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Populate the version of every command class the node implements
        foreach (KeyValuePair<CommandClassId, CommandClassInfo> pair in Endpoint.CommandClasses)
        {
            CommandClassId commandClassId = pair.Key;
            _ = await GetCommandClassVersionAsync(commandClassId, cancellationToken).ConfigureAwait(false);
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
            case VersionCommand.Report:
            {
                VersionReport report = VersionReportCommand.Parse(frame, Logger);
                HardwareInfo = report;
                OnVersionReportReceived?.Invoke(report);
                break;
            }
            case VersionCommand.CommandClassReport:
            {
                (CommandClassId requestedCommandClass, byte commandClassVersion) = VersionCommandClassReportCommand.Parse(frame, Logger);
                Endpoint.GetCommandClass(requestedCommandClass).SetVersion(commandClassVersion);
                break;
            }
            case VersionCommand.CapabilitiesReport:
            {
                VersionCapabilities capabilities = VersionCapabilitiesReportCommand.Parse(frame, Logger);
                Capabilities = capabilities;
                OnCapabilitiesReportReceived?.Invoke(capabilities);
                break;
            }
            case VersionCommand.ZWaveSoftwareReport:
            {
                VersionSoftwareInfo softwareInfo = VersionZWaveSoftwareReportCommand.Parse(frame, Logger);
                SoftwareInfo = softwareInfo;
                OnZWaveSoftwareReportReceived?.Invoke(softwareInfo);
                break;
            }
            case VersionCommand.MigrationReport:
            {
                VersionMigrationReport report = VersionMigrationReportCommand.Parse(frame, Logger);
                OnMigrationReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
