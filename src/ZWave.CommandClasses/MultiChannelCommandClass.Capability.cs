using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the Multi Channel Capability Report data for a single End Point.
/// </summary>
public readonly record struct MultiChannelCapabilityReport(
    /// <summary>
    /// Whether this End Point is dynamic.
    /// </summary>
    bool IsDynamic,

    /// <summary>
    /// The End Point index.
    /// </summary>
    byte EndpointIndex,

    /// <summary>
    /// The Generic Device Class of the End Point.
    /// </summary>
    byte GenericDeviceClass,

    /// <summary>
    /// The Specific Device Class of the End Point.
    /// </summary>
    byte SpecificDeviceClass,

    /// <summary>
    /// The command classes supported by this End Point (non-secure).
    /// </summary>
    IReadOnlyList<CommandClassInfo> CommandClasses);

public sealed partial class MultiChannelCommandClass
{
    /// <summary>
    /// Event raised when a Capability Report is received (solicited or unsolicited).
    /// </summary>
    public Action<MultiChannelCapabilityReport>? OnCapabilityReportReceived { get; set; }

    /// <summary>
    /// Queries the capabilities of a specific End Point.
    /// </summary>
    public async Task<MultiChannelCapabilityReport> GetCapabilityAsync(byte endpointIndex, CancellationToken cancellationToken)
    {
        MultiChannelCapabilityGetCommand command = MultiChannelCapabilityGetCommand.Create(endpointIndex);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        // Wait for a report matching this specific endpoint
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultiChannelCapabilityReportCommand>(
            frame => MultiChannelCapabilityReportCommand.GetEndpointIndex(frame) == endpointIndex,
            cancellationToken).ConfigureAwait(false);
        MultiChannelCapabilityReport report = MultiChannelCapabilityReportCommand.Parse(reportFrame, Logger);
        OnCapabilityReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct MultiChannelCapabilityGetCommand : ICommand
    {
        public MultiChannelCapabilityGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.CapabilityGet;

        public CommandClassFrame Frame { get; }

        public static MultiChannelCapabilityGetCommand Create(byte endpointIndex)
        {
            if (endpointIndex is 0 or > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(endpointIndex), endpointIndex, "Endpoint index must be between 1 and 127.");
            }

            ReadOnlySpan<byte> parameters = [endpointIndex];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new MultiChannelCapabilityGetCommand(frame);
        }
    }

    internal readonly struct MultiChannelCapabilityReportCommand : ICommand
    {
        public MultiChannelCapabilityReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.CapabilityReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Gets the End Point index from a capability report frame without fully parsing it.
        /// Used for matching awaited reports.
        /// </summary>
        public static byte GetEndpointIndex(CommandClassFrame frame)
            => (byte)(frame.CommandParameters.Span[0] & 0b0111_1111);

        /// <summary>
        /// Parses a Multi Channel Capability Report frame.
        /// </summary>
        /// <remarks>
        /// Wire format (from zwave.xml):
        ///   params[0]: Dynamic(bit 7) | End Point(bits 6..0)
        ///   params[1]: Generic Device Class
        ///   params[2]: Specific Device Class
        ///   params[3..N]: Command Class list (may include 0xEF mark separating supported/controlled)
        /// </remarks>
        public static MultiChannelCapabilityReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Multi Channel Capability Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multi Channel Capability Report frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            bool isDynamic = (parameters[0] & 0b1000_0000) != 0;
            byte endpointIndex = (byte)(parameters[0] & 0b0111_1111);
            byte genericDeviceClass = parameters[1];
            byte specificDeviceClass = parameters[2];

            // Parse the command class list from remaining bytes.
            // Per spec §4.2.2.6: "The Multi Channel Command Class MUST NOT be advertised in this list."
            IReadOnlyList<CommandClassInfo> commandClasses = CommandClassInfo.ParseList(parameters[3..]);

            return new MultiChannelCapabilityReport(isDynamic, endpointIndex, genericDeviceClass, specificDeviceClass, commandClasses);
        }
    }
}
