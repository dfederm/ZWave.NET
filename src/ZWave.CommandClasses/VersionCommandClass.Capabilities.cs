using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies version capabilities supported by the device.
/// </summary>
[Flags]
public enum VersionCapabilities : byte
{
    /// <summary>
    /// Advertise support for the version information queried with the Version Get Command.
    /// </summary>
    Version = 1 << 0,

    /// <summary>
    /// Advertise support for the Command Class version information queried with the Version Command Class Get Command.
    /// This flag MUST always be set.
    /// </summary>
    CommandClass = 1 << 1,

    /// <summary>
    /// Advertise support for the detailed Z-Wave software version information queried with the Version Z-Wave Software
    /// Get Command.
    /// </summary>
    ZWaveSoftware = 1 << 2,

    /// <summary>
    /// Advertise support for data migration and the Version Migration Capabilities Get Command.
    /// Available in Version CC V4+.
    /// </summary>
    MigrationSupport = 1 << 3,
}

public sealed partial class VersionCommandClass
{
    /// <summary>
    /// Occurs when a Version Capabilities Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<VersionCapabilities>? OnCapabilitiesReportReceived;

    /// <summary>
    /// Gets the version capabilities advertised by the device.
    /// </summary>
    public VersionCapabilities? Capabilities { get; private set; }

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
        OnCapabilitiesReportReceived?.Invoke(capabilities);
        return capabilities;
    }

    internal readonly struct VersionCapabilitiesGetCommand : ICommand
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

    internal readonly struct VersionCapabilitiesReportCommand : ICommand
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
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Version Capabilities Report frame is too short");
            }

            return (VersionCapabilities)frame.CommandParameters.Span[0];
        }
    }
}
