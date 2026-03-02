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
    /// The NodeID-only destinations in this association group.
    /// </summary>
    IReadOnlyList<byte> NodeIdDestinations,

    /// <summary>
    /// The End Point destinations in this association group.
    /// </summary>
    IReadOnlyList<EndpointDestination> EndpointDestinations);

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

    private readonly Dictionary<byte, MultiChannelAssociationReport> _groupReports = [];

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

        List<byte> allNodeIdDestinations = [];
        List<EndpointDestination> allEndpointDestinations = [];
        byte maxNodesSupported = 0;

        byte reportsToFollow;
        do
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<MultiChannelAssociationReportCommand>(
                frame => frame.CommandParameters.Length >= 1 && frame.CommandParameters.Span[0] == groupingIdentifier,
                cancellationToken).ConfigureAwait(false);
            (maxNodesSupported, reportsToFollow) = MultiChannelAssociationReportCommand.ParseInto(
                reportFrame, allNodeIdDestinations, allEndpointDestinations, Logger);
        }
        while (reportsToFollow > 0);

        MultiChannelAssociationReport report = new(
            groupingIdentifier,
            maxNodesSupported,
            allNodeIdDestinations,
            allEndpointDestinations);
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

        public static (byte MaxNodesSupported, byte ReportsToFollow) ParseInto(
            CommandClassFrame frame,
            List<byte> nodeIdDestinations,
            List<EndpointDestination> endpointDestinations,
            ILogger logger)
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

            int markerIndex = destinationData.IndexOf(Marker);

            if (markerIndex < 0)
            {
                for (int i = 0; i < destinationData.Length; i++)
                {
                    nodeIdDestinations.Add(destinationData[i]);
                }
            }
            else
            {
                for (int i = 0; i < markerIndex; i++)
                {
                    nodeIdDestinations.Add(destinationData[i]);
                }

                // Parse endpoint destinations, group by NodeId, expand bit masks.
                ReadOnlySpan<byte> endpointData = destinationData[(markerIndex + 1)..];
                Dictionary<byte, List<byte>>? grouped = null;
                for (int i = 0; i + 1 < endpointData.Length; i += 2)
                {
                    byte nodeId = endpointData[i];
                    byte properties = endpointData[i + 1];
                    bool bitAddress = (properties & 0b1000_0000) != 0;
                    byte endpointValue = (byte)(properties & 0b0111_1111);

                    grouped ??= [];
                    if (!grouped.TryGetValue(nodeId, out List<byte>? endpoints))
                    {
                        endpoints = [];
                        grouped[nodeId] = endpoints;
                    }

                    if (bitAddress)
                    {
                        for (int bit = 0; bit < 7; bit++)
                        {
                            if ((endpointValue & (1 << bit)) != 0)
                            {
                                endpoints.Add((byte)(bit + 1));
                            }
                        }
                    }
                    else
                    {
                        endpoints.Add(endpointValue);
                    }
                }

                if (grouped != null)
                {
                    foreach (KeyValuePair<byte, List<byte>> entry in grouped)
                    {
                        endpointDestinations.Add(new EndpointDestination(entry.Key, (ReadOnlySpan<byte>)entry.Value.ToArray()));
                    }
                }

                if (endpointData.Length % 2 != 0)
                {
                    logger.LogWarning(
                        "Multi Channel Association Report has a trailing byte after the marker (odd endpoint data length: {Length})",
                        endpointData.Length);
                }
            }

            return (maxNodesSupported, reportsToFollow);
        }
    }
}
