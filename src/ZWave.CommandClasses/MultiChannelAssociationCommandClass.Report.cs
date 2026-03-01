using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the data from a Multi Channel Association Report command.
/// </summary>
public readonly record struct MultiChannelAssociationReport(
    /// <summary>
    /// The association group identifier.
    /// </summary>
    byte GroupingIdentifier,

    /// <summary>
    /// The maximum number of destinations supported by this association group.
    /// Each destination may be a NodeID destination or an End Point destination.
    /// </summary>
    byte MaxNodesSupported,

    /// <summary>
    /// The number of report frames that will follow this report.
    /// </summary>
    byte ReportsToFollow,

    /// <summary>
    /// The NodeID-only destinations in this association group.
    /// </summary>
    IReadOnlyList<byte> NodeIdDestinations,

    /// <summary>
    /// The End Point destinations in this association group.
    /// </summary>
    IReadOnlyList<EndPointDestination> EndPointDestinations);

public sealed partial class MultiChannelAssociationCommandClass
{
    /// <summary>
    /// Event raised when a Multi Channel Association Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<MultiChannelAssociationReport>? OnReportReceived;

    /// <summary>
    /// Gets the last report received for each association group.
    /// </summary>
    public IReadOnlyDictionary<byte, MultiChannelAssociationReport> GroupReports => _groupReports;

    private readonly Dictionary<byte, MultiChannelAssociationReport> _groupReports = new Dictionary<byte, MultiChannelAssociationReport>();

    private void UpdateGroupReport(MultiChannelAssociationReport report)
    {
        _groupReports[report.GroupingIdentifier] = report;
    }

    /// <summary>
    /// Request the current destinations of a given association group.
    /// </summary>
    public async Task<MultiChannelAssociationReport> GetAsync(byte groupingIdentifier, CancellationToken cancellationToken)
    {
        MultiChannelAssociationGetCommand command = MultiChannelAssociationGetCommand.Create(groupingIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultiChannelAssociationReportCommand>(
            frame => frame.CommandParameters.Length >= 1 && frame.CommandParameters.Span[0] == groupingIdentifier,
            cancellationToken).ConfigureAwait(false);
        MultiChannelAssociationReport report = MultiChannelAssociationReportCommand.Parse(reportFrame, Logger);
        UpdateGroupReport(report);
        OnReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct MultiChannelAssociationGetCommand : ICommand
    {
        public MultiChannelAssociationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationGetCommand Create(byte groupingIdentifier)
        {
            Span<byte> commandParameters = [groupingIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultiChannelAssociationGetCommand(frame);
        }
    }

    internal readonly struct MultiChannelAssociationReportCommand : ICommand
    {
        public MultiChannelAssociationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Report;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning(
                    "Multi Channel Association Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Multi Channel Association Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte groupingIdentifier = span[0];
            byte maxNodesSupported = span[1];
            byte reportsToFollow = span[2];

            ReadOnlySpan<byte> destinationData = span[3..];

            // Find the marker byte (0x00) to split NodeID destinations from End Point destinations.
            int markerIndex = destinationData.IndexOf(Marker);

            List<byte> nodeIdDestinations;
            List<EndPointDestination> endPointDestinations;

            if (markerIndex < 0)
            {
                // No marker — all destinations are NodeID-only.
                nodeIdDestinations = new List<byte>(destinationData.Length);
                for (int i = 0; i < destinationData.Length; i++)
                {
                    nodeIdDestinations.Add(destinationData[i]);
                }

                endPointDestinations = new List<EndPointDestination>();
            }
            else
            {
                // Parse NodeID destinations before the marker.
                nodeIdDestinations = new List<byte>(markerIndex);
                for (int i = 0; i < markerIndex; i++)
                {
                    nodeIdDestinations.Add(destinationData[i]);
                }

                // Parse End Point destinations after the marker.
                // Each End Point destination is 2 bytes: NodeID + (BitAddress | EndPoint).
                ReadOnlySpan<byte> endPointData = destinationData[(markerIndex + 1)..];
                int endPointCount = endPointData.Length / 2;
                endPointDestinations = new List<EndPointDestination>(endPointCount);
                for (int i = 0; i + 1 < endPointData.Length; i += 2)
                {
                    byte nodeId = endPointData[i];
                    byte properties = endPointData[i + 1];
                    bool bitAddress = (properties & 0x80) != 0;
                    byte endPoint = (byte)(properties & 0x7F);
                    endPointDestinations.Add(new EndPointDestination(nodeId, bitAddress, endPoint));
                }

                if (endPointData.Length % 2 != 0)
                {
                    logger.LogWarning(
                        "Multi Channel Association Report has a trailing byte after the marker (odd End Point data length: {Length})",
                        endPointData.Length);
                }
            }

            return new MultiChannelAssociationReport(
                groupingIdentifier,
                maxNodesSupported,
                reportsToFollow,
                nodeIdDestinations,
                endPointDestinations);
        }
    }
}
