using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the data from an Association Report command.
/// </summary>
public readonly record struct AssociationReport(
    /// <summary>
    /// The association group identifier.
    /// </summary>
    byte GroupingIdentifier,

    /// <summary>
    /// The maximum number of destinations supported by this association group.
    /// Each destination may be a NodeID destination or an End Point destination
    /// (if the node supports the Multi Channel Association Command Class).
    /// </summary>
    byte MaxNodesSupported,

    /// <summary>
    /// The NodeID destinations in this association group.
    /// </summary>
    IReadOnlyList<byte> NodeIdDestinations);

public sealed partial class AssociationCommandClass
{
    /// <summary>
    /// Event raised when an Association Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<AssociationReport>? OnReportReceived;

    /// <summary>
    /// Gets the last report received for each association group.
    /// </summary>
    public IReadOnlyDictionary<byte, AssociationReport> GroupReports => _groupReports;

    private readonly Dictionary<byte, AssociationReport> _groupReports = [];

    private void UpdateGroupReport(AssociationReport report)
    {
        _groupReports[report.GroupingIdentifier] = report;
    }

    /// <summary>
    /// Request the current destinations of a given association group.
    /// </summary>
    /// <remarks>
    /// The report may span multiple frames if the destination list is large.
    /// This method aggregates all frames before returning.
    /// </remarks>
    /// <param name="groupingIdentifier">The association group identifier (1-255).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The association report for the given group.</returns>
    public async Task<AssociationReport> GetAsync(byte groupingIdentifier, CancellationToken cancellationToken)
    {
        AssociationGetCommand command = AssociationGetCommand.Create(groupingIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        List<byte> allNodeIdDestinations = [];
        byte maxNodesSupported = 0;

        byte reportsToFollow;
        do
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<AssociationReportCommand>(
                frame => frame.CommandParameters.Length >= 1 && frame.CommandParameters.Span[0] == groupingIdentifier,
                cancellationToken).ConfigureAwait(false);
            (maxNodesSupported, reportsToFollow) = AssociationReportCommand.ParseInto(
                reportFrame, allNodeIdDestinations, Logger);
        }
        while (reportsToFollow > 0);

        AssociationReport report = new(
            groupingIdentifier,
            maxNodesSupported,
            allNodeIdDestinations);
        UpdateGroupReport(report);
        OnReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct AssociationGetCommand : ICommand
    {
        public AssociationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static AssociationGetCommand Create(byte groupingIdentifier)
        {
            Span<byte> commandParameters = [groupingIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationGetCommand(frame);
        }
    }

    internal readonly struct AssociationReportCommand : ICommand
    {
        public AssociationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Report;

        public CommandClassFrame Frame { get; }

        public static AssociationReportCommand Create(
            byte groupingIdentifier,
            byte maxNodesSupported,
            byte reportsToFollow,
            ReadOnlySpan<byte> nodeIdDestinations)
        {
            Span<byte> commandParameters = stackalloc byte[3 + nodeIdDestinations.Length];
            commandParameters[0] = groupingIdentifier;
            commandParameters[1] = maxNodesSupported;
            commandParameters[2] = reportsToFollow;
            nodeIdDestinations.CopyTo(commandParameters[3..]);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationReportCommand(frame);
        }

        /// <summary>
        /// Parse a single Association Report frame, appending NodeID destinations to the provided list.
        /// </summary>
        /// <returns>The max nodes supported and reports-to-follow count from this frame.</returns>
        public static (byte MaxNodesSupported, byte ReportsToFollow) ParseInto(
            CommandClassFrame frame,
            List<byte> nodeIdDestinations,
            ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning(
                    "Association Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte maxNodesSupported = span[1];
            byte reportsToFollow = span[2];
            ReadOnlySpan<byte> destinationData = span[3..];

            for (int i = 0; i < destinationData.Length; i++)
            {
                nodeIdDestinations.Add(destinationData[i]);
            }

            return (maxNodesSupported, reportsToFollow);
        }
    }
}
