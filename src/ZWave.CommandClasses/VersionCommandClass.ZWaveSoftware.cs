using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the detailed Z-Wave chip software version information of a device.
/// </summary>
public readonly record struct VersionSoftwareInfo(
    /// <summary>
    /// The SDK version used for building the Z-Wave chip software components for the node.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    Version? SdkVersion,

    /// <summary>
    /// The Z-Wave Application Framework API version used by the node.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    Version? ApplicationFrameworkApiVersion,

    /// <summary>
    /// The Z-Wave Application Framework build number running on the node.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    ushort? ApplicationFrameworkBuildNumber,

    /// <summary>
    /// The version of the Serial API exposed to a host CPU or a second Chip.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    Version? HostInterfaceVersion,

    /// <summary>
    /// The build number of the Serial API software exposed to a host CPU or second Chip.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    ushort? HostInterfaceBuildNumber,

    /// <summary>
    /// The Z-Wave protocol version used by the node.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    Version? ZWaveProtocolVersion,

    /// <summary>
    /// The actual build number of the Z-Wave protocol software used by the node.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    ushort? ZWaveProtocolBuildNumber,

    /// <summary>
    /// The version of application software used by the node on its Z-Wave chip.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    Version? ApplicationVersion,

    /// <summary>
    /// The actual build number of the application software used by the node on its Z-Wave chip.
    /// The value <see langword="null"/> indicates that this field is unused.
    /// </summary>
    ushort? ApplicationBuildNumber);

public sealed partial class VersionCommandClass
{
    /// <summary>
    /// Occurs when a Version Z-Wave Software Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<VersionSoftwareInfo>? OnZWaveSoftwareReportReceived;

    /// <summary>
    /// Gets the detailed Z-Wave chip software version information.
    /// </summary>
    public VersionSoftwareInfo? SoftwareInfo { get; private set; }

    /// <summary>
    /// Request the detailed Z-Wave chip software version information of a node.
    /// </summary>
    public async Task<VersionSoftwareInfo> GetZWaveSoftwareAsync(CancellationToken cancellationToken)
    {
        var command = VersionZWaveSoftwareGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionZWaveSoftwareReportCommand>(cancellationToken).ConfigureAwait(false);
        VersionSoftwareInfo softwareInfo = VersionZWaveSoftwareReportCommand.Parse(reportFrame, Logger);
        SoftwareInfo = softwareInfo;
        OnZWaveSoftwareReportReceived?.Invoke(softwareInfo);
        return softwareInfo;
    }

    internal readonly struct VersionZWaveSoftwareGetCommand : ICommand
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

    internal readonly struct VersionZWaveSoftwareReportCommand : ICommand
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
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Version Z-Wave Software Report frame is too short");
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
            ushort buildNum = bytes.ToUInt16BE();

            // The value 0 MUST indicate that this field is unused.
            return buildNum != 0
                ? buildNum
                : null;
        }
    }
}
