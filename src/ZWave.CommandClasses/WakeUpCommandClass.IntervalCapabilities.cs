using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the wake up interval capabilities of a device.
/// </summary>
public readonly record struct WakeUpIntervalCapabilities(
    /// <summary>
    /// The minimum Wake Up Interval supported by the sending node.
    /// </summary>
    uint MinimumWakeUpIntervalInSeconds,

    /// <summary>
    /// The maximum Wake Up Interval supported by the sending node.
    /// </summary>
    uint MaximumWakeUpIntervalInSeconds,

    /// <summary>
    /// The default Wake Up Interval for the sending node.
    /// </summary>
    uint DefaultWakeUpIntervalInSeconds,

    /// <summary>
    /// The resolution of valid Wake Up Interval values for the sending node.
    /// </summary>
    uint WakeUpIntervalStepInSeconds,

    /// <summary>
    /// Whether the supporting node supports the Wake Up On Demand functionality.
    /// </summary>
    bool? SupportsWakeUpOnDemand);

public sealed partial class WakeUpCommandClass
{
    /// <summary>
    /// Gets the wake up interval capabilities.
    /// </summary>
    public WakeUpIntervalCapabilities? IntervalCapabilities { get; private set; }

    /// <summary>
    /// Request the wake up interval capabilities from the device.
    /// </summary>
    public async Task<WakeUpIntervalCapabilities> GetIntervalCapabilitiesAsync(CancellationToken cancellationToken)
    {
        WakeUpIntervalCapabilitiesGetCommand command = WakeUpIntervalCapabilitiesGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<WakeUpIntervalCapabilitiesReportCommand>(cancellationToken).ConfigureAwait(false);
        WakeUpIntervalCapabilities capabilities = WakeUpIntervalCapabilitiesReportCommand.Parse(reportFrame, Logger);
        IntervalCapabilities = capabilities;
        return capabilities;
    }

    internal readonly struct WakeUpIntervalCapabilitiesGetCommand : ICommand
    {
        public WakeUpIntervalCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalCapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalCapabilitiesGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WakeUpIntervalCapabilitiesGetCommand(frame);
        }
    }

    internal readonly struct WakeUpIntervalCapabilitiesReportCommand : ICommand
    {
        public WakeUpIntervalCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalCapabilitiesReport;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalCapabilities Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 12)
            {
                logger.LogWarning("Wake Up Interval Capabilities Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Wake Up Interval Capabilities Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            uint minimumWakeUpIntervalInSeconds = (uint)(span[0] << 16) | (uint)(span[1] << 8) | span[2];
            uint maximumWakeUpIntervalInSeconds = (uint)(span[3] << 16) | (uint)(span[4] << 8) | span[5];
            uint defaultWakeUpIntervalInSeconds = (uint)(span[6] << 16) | (uint)(span[7] << 8) | span[8];
            uint wakeUpIntervalStepInSeconds = (uint)(span[9] << 16) | (uint)(span[10] << 8) | span[11];

            bool? supportsWakeUpOnDemand = frame.CommandParameters.Length > 12
                ? (span[12] & 0x01) != 0
                : null;

            return new WakeUpIntervalCapabilities(
                minimumWakeUpIntervalInSeconds,
                maximumWakeUpIntervalInSeconds,
                defaultWakeUpIntervalInSeconds,
                wakeUpIntervalStepInSeconds,
                supportsWakeUpOnDemand);
        }
    }
}
