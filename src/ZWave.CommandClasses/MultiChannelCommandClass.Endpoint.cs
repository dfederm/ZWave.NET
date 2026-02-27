using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the Multi Channel End Point Report data.
/// </summary>
public readonly record struct MultiChannelEndpointReport(
    /// <summary>
    /// Whether the node implements dynamic End Points.
    /// </summary>
    bool IsDynamic,

    /// <summary>
    /// Whether all End Points have identical capabilities.
    /// </summary>
    bool AreIdentical,

    /// <summary>
    /// The number of individual End Points (1–127).
    /// </summary>
    byte IndividualEndpointCount,

    /// <summary>
    /// The number of Aggregated End Points (version 4).
    /// </summary>
    byte AggregatedEndpointCount);

public sealed partial class MultiChannelCommandClass
{
    /// <summary>
    /// Event raised when an End Point Report is received (solicited or unsolicited).
    /// </summary>
    public event Action<MultiChannelEndpointReport>? OnEndpointReportReceived;

    /// <summary>
    /// Gets the last End Point Report received.
    /// </summary>
    public MultiChannelEndpointReport? LastEndpointReport { get; private set; }

    /// <summary>
    /// Queries the number of End Points implemented by the node.
    /// </summary>
    public async Task<MultiChannelEndpointReport> GetEndpointReportAsync(CancellationToken cancellationToken)
    {
        MultiChannelEndpointGetCommand command = MultiChannelEndpointGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultiChannelEndpointReportCommand>(cancellationToken).ConfigureAwait(false);
        MultiChannelEndpointReport report = MultiChannelEndpointReportCommand.Parse(reportFrame, Logger);
        LastEndpointReport = report;
        OnEndpointReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct MultiChannelEndpointGetCommand : ICommand
    {
        public MultiChannelEndpointGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.EndpointGet;

        public CommandClassFrame Frame { get; }

        public static MultiChannelEndpointGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultiChannelEndpointGetCommand(frame);
        }
    }

    internal readonly struct MultiChannelEndpointReportCommand : ICommand
    {
        public MultiChannelEndpointReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.EndpointReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Parses a Multi Channel End Point Report frame.
        /// </summary>
        /// <remarks>
        /// Wire format (from zwave.xml, version 3-4):
        ///   params[0] (Properties1): bit7=Dynamic, bit6=Identical, bits5..0=Reserved
        ///   params[1] (Properties2): bit7=Reserved, bits6..0=Individual End Points (7 bits, range 1..127)
        ///   params[2] (Properties3, v4 only): bit7=Reserved, bits6..0=Aggregated End Points
        /// </remarks>
        public static MultiChannelEndpointReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Multi Channel End Point Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multi Channel End Point Report frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            bool isDynamic = (parameters[0] & 0b1000_0000) != 0;
            bool areIdentical = (parameters[0] & 0b0100_0000) != 0;
            byte individualEndpointCount = (byte)(parameters[1] & 0b0111_1111);

            byte aggregatedEndpointCount = 0;
            if (parameters.Length >= 3)
            {
                aggregatedEndpointCount = (byte)(parameters[2] & 0b0111_1111);
            }

            return new MultiChannelEndpointReport(isDynamic, areIdentical, individualEndpointCount, aggregatedEndpointCount);
        }
    }
}
