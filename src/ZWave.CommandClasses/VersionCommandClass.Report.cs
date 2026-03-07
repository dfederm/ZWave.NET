using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the type of Z-Wave library.
/// </summary>
public enum ZWaveLibraryType : byte
{
    /// <summary>
    /// Not applicable.
    /// </summary>
    NotApplicable = 0x00,

    /// <summary>
    /// Static Controller library.
    /// </summary>
    StaticController = 0x01,

    /// <summary>
    /// Controller library.
    /// </summary>
    Controller = 0x02,

    /// <summary>
    /// Enhanced End Node library.
    /// </summary>
    EnhancedEndNode = 0x03,

    /// <summary>
    /// End Node library.
    /// </summary>
    EndNode = 0x04,

    /// <summary>
    /// Installer library.
    /// </summary>
    Installer = 0x05,

    /// <summary>
    /// Routing End Node library.
    /// </summary>
    RoutingEndNode = 0x06,

    /// <summary>
    /// Bridge Controller library.
    /// </summary>
    BridgeController = 0x07,

    /// <summary>
    /// Device Under Test (DUT) library.
    /// </summary>
    DeviceUnderTest = 0x08,

    /// <summary>
    /// Not applicable.
    /// </summary>
    NotApplicable2 = 0x09,

    /// <summary>
    /// AV Remote library.
    /// </summary>
    AvRemote = 0x0A,

    /// <summary>
    /// AV Device library.
    /// </summary>
    AvDevice = 0x0B,
}

/// <summary>
/// Represents a Version Report received from a device, containing the library type,
/// protocol version, firmware versions, and optionally the hardware version.
/// </summary>
public readonly record struct VersionReport(
    /// <summary>
    /// The Z-Wave Protocol Library Type.
    /// </summary>
    ZWaveLibraryType LibraryType,

    /// <summary>
    /// The Z-Wave protocol version (SDK version provided by Silicon Labs).
    /// </summary>
    Version ProtocolVersion,

    /// <summary>
    /// The firmware versions of the device. Firmware 0 is dedicated to the Z-Wave chip firmware.
    /// Additional firmware versions represent other firmware images (e.g. host processor).
    /// </summary>
    IReadOnlyList<Version> FirmwareVersions,

    /// <summary>
    /// A value which is unique to this particular version of the product.
    /// Available in Version CC V2+.
    /// </summary>
    byte? HardwareVersion);

public sealed partial class VersionCommandClass
{
    /// <summary>
    /// Occurs when a Version Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<VersionReport>? OnVersionReportReceived;

    /// <summary>
    /// Gets the hardware and firmware version information.
    /// </summary>
    public VersionReport? HardwareInfo { get; private set; }

    /// <summary>
    /// Request the library type, protocol version and application version from a device that supports
    /// the Version Command Class.
    /// </summary>
    public async Task<VersionReport> GetAsync(CancellationToken cancellationToken)
    {
        var command = VersionGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionReportCommand>(cancellationToken).ConfigureAwait(false);
        VersionReport report = VersionReportCommand.Parse(reportFrame, Logger);
        HardwareInfo = report;
        OnVersionReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct VersionGetCommand : ICommand
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

    internal readonly struct VersionReportCommand : ICommand
    {
        public VersionReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.Report;

        public CommandClassFrame Frame { get; }

        public static VersionReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // V1 minimum: LibraryType(1) + ProtocolVersion(1) + ProtocolSubVersion(1) + Firmware0Version(1) + Firmware0SubVersion(1) = 5 bytes
            if (frame.CommandParameters.Length < 5)
            {
                logger.LogWarning("Version Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Version Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            ZWaveLibraryType libraryType = (ZWaveLibraryType)span[0];
            Version protocolVersion = new Version(span[1], span[2]);

            // V2+ fields: HardwareVersion at offset 5, NumberOfFirmwareTargets at offset 6
            byte? hardwareVersion = span.Length > 5 ? span[5] : null;

            int numFirmwareVersions = 1;
            if (span.Length > 6)
            {
                byte declaredAdditionalFirmware = span[6];
                int requiredLength = 7 + (declaredAdditionalFirmware * 2);

                if (span.Length < requiredLength)
                {
                    logger.LogWarning(
                        "Version Report declares {Count} additional firmware targets but payload is too short ({Length} bytes)",
                        declaredAdditionalFirmware,
                        frame.CommandParameters.Length);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Version Report payload is too short for declared firmware count");
                }

                numFirmwareVersions += declaredAdditionalFirmware;
            }

            Version[] firmwareVersions = new Version[numFirmwareVersions];
            firmwareVersions[0] = new Version(span[3], span[4]);

            for (int i = 1; i < numFirmwareVersions; i++)
            {
                int versionOffset = 5 + (2 * i);
                firmwareVersions[i] = new Version(span[versionOffset], span[versionOffset + 1]);
            }

            return new VersionReport(libraryType, protocolVersion, firmwareVersions, hardwareVersion);
        }
    }
}
