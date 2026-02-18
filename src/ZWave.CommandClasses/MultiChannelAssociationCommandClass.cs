namespace ZWave.CommandClasses;

public enum MultiChannelAssociationCommand : byte
{
    /// <summary>
    /// Add destinations to an association group.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the destinations in an association group.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the destinations in an association group.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Remove destinations from an association group.
    /// </summary>
    Remove = 0x04,

    /// <summary>
    /// Request the number of supported association groups.
    /// </summary>
    SupportedGroupingsGet = 0x05,

    /// <summary>
    /// Advertise the number of supported association groups.
    /// </summary>
    SupportedGroupingsReport = 0x06,
}

/// <summary>
/// Represents an endpoint destination in a multi channel association group.
/// </summary>
public readonly struct MultiChannelAssociationDestination
{
    public MultiChannelAssociationDestination(byte nodeId, byte endpoint)
    {
        NodeId = nodeId;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Gets the node ID of the destination.
    /// </summary>
    public byte NodeId { get; }

    /// <summary>
    /// Gets the endpoint of the destination.
    /// </summary>
    public byte Endpoint { get; }
}

/// <summary>
/// Represents the state of a multi channel association group.
/// </summary>
public readonly struct MultiChannelAssociationGroup
{
    public MultiChannelAssociationGroup(
        byte maxNodesSupported,
        IReadOnlySet<byte> nodeIds,
        IReadOnlySet<MultiChannelAssociationDestination> endpointDestinations)
    {
        MaxNodesSupported = maxNodesSupported;
        NodeIds = nodeIds;
        EndpointDestinations = endpointDestinations;
    }

    /// <summary>
    /// Gets the maximum number of destinations supported in this group.
    /// </summary>
    public byte MaxNodesSupported { get; }

    /// <summary>
    /// Gets the node IDs currently in this group.
    /// </summary>
    public IReadOnlySet<byte> NodeIds { get; }

    /// <summary>
    /// Gets the endpoint destinations currently in this group.
    /// </summary>
    public IReadOnlySet<MultiChannelAssociationDestination> EndpointDestinations { get; }
}

[CommandClass(CommandClassId.MultiChannelAssociation)]
public sealed class MultiChannelAssociationCommandClass : CommandClass<MultiChannelAssociationCommand>
{
    /// <summary>
    /// The marker byte that separates node IDs from endpoint destinations.
    /// </summary>
    private const byte Marker = 0x00;

    private Dictionary<byte, MultiChannelAssociationGroup>? _groups;

    internal MultiChannelAssociationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the number of supported association groups.
    /// </summary>
    public byte? SupportedGroupings { get; private set; }

    /// <summary>
    /// Gets the multi channel association groups and their assignments.
    /// </summary>
    public IReadOnlyDictionary<byte, MultiChannelAssociationGroup>? Groups => _groups;

    /// <inheritdoc />
    public override bool? IsCommandSupported(MultiChannelAssociationCommand command)
        => command switch
        {
            MultiChannelAssociationCommand.Set => true,
            MultiChannelAssociationCommand.Get => true,
            MultiChannelAssociationCommand.Remove => true,
            MultiChannelAssociationCommand.SupportedGroupingsGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        byte supportedGroupings = await GetSupportedGroupingsAsync(cancellationToken).ConfigureAwait(false);
        for (byte groupId = 1; groupId <= supportedGroupings; groupId++)
        {
            _ = await GetAsync(groupId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request the number of supported association groups.
    /// </summary>
    public async Task<byte> GetSupportedGroupingsAsync(CancellationToken cancellationToken)
    {
        var command = MultiChannelAssociationSupportedGroupingsGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<MultiChannelAssociationSupportedGroupingsReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedGroupings!.Value;
    }

    /// <summary>
    /// Request the destinations in an association group.
    /// </summary>
    public async Task<MultiChannelAssociationGroup> GetAsync(byte groupId, CancellationToken cancellationToken)
    {
        var command = MultiChannelAssociationGetCommand.Create(groupId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<MultiChannelAssociationReportCommand>(
            predicate: frame =>
            {
                var report = new MultiChannelAssociationReportCommand(frame);
                return report.GroupingIdentifier == groupId;
            },
            cancellationToken).ConfigureAwait(false);
        return _groups![groupId];
    }

    /// <summary>
    /// Add destinations to an association group.
    /// </summary>
    public Task SetAsync(
        byte groupId,
        ReadOnlySpan<byte> nodeIds,
        ReadOnlySpan<MultiChannelAssociationDestination> endpointDestinations,
        CancellationToken cancellationToken)
    {
        var command = MultiChannelAssociationSetCommand.Create(groupId, nodeIds, endpointDestinations);
        return SendCommandAsync(command, cancellationToken);
    }

    /// <summary>
    /// Remove destinations from an association group. Empty lists remove all associations.
    /// </summary>
    public Task RemoveAsync(
        byte groupId,
        ReadOnlySpan<byte> nodeIds,
        ReadOnlySpan<MultiChannelAssociationDestination> endpointDestinations,
        CancellationToken cancellationToken)
    {
        var command = MultiChannelAssociationRemoveCommand.Create(groupId, nodeIds, endpointDestinations);
        return SendCommandAsync(command, cancellationToken);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((MultiChannelAssociationCommand)frame.CommandId)
        {
            case MultiChannelAssociationCommand.Set:
            case MultiChannelAssociationCommand.Get:
            case MultiChannelAssociationCommand.Remove:
            case MultiChannelAssociationCommand.SupportedGroupingsGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case MultiChannelAssociationCommand.Report:
            {
                var command = new MultiChannelAssociationReportCommand(frame);
                byte groupId = command.GroupingIdentifier;
                byte maxNodesSupported = command.MaxNodesSupported;
                HashSet<byte> nodeIds = command.NodeIds;
                HashSet<MultiChannelAssociationDestination> endpointDestinations = command.EndpointDestinations;

                _groups ??= new Dictionary<byte, MultiChannelAssociationGroup>();
                _groups[groupId] = new MultiChannelAssociationGroup(maxNodesSupported, nodeIds, endpointDestinations);
                break;
            }
            case MultiChannelAssociationCommand.SupportedGroupingsReport:
            {
                var command = new MultiChannelAssociationSupportedGroupingsReportCommand(frame);
                SupportedGroupings = command.SupportedGroupingsCount;
                break;
            }
        }
    }

    /// <summary>
    /// Encodes node IDs and endpoint destinations into a byte span with the marker separator.
    /// </summary>
    private static void EncodeDestinations(
        Span<byte> destination,
        byte groupId,
        ReadOnlySpan<byte> nodeIds,
        ReadOnlySpan<MultiChannelAssociationDestination> endpointDestinations)
    {
        destination[0] = groupId;
        int offset = 1;

        nodeIds.CopyTo(destination[offset..]);
        offset += nodeIds.Length;

        if (endpointDestinations.Length > 0)
        {
            destination[offset++] = Marker;
            for (int i = 0; i < endpointDestinations.Length; i++)
            {
                destination[offset++] = endpointDestinations[i].NodeId;
                destination[offset++] = endpointDestinations[i].Endpoint;
            }
        }
    }

    /// <summary>
    /// Calculates the byte length needed to encode a group ID, node IDs, and endpoint destinations.
    /// </summary>
    private static int CalculateParameterLength(
        ReadOnlySpan<byte> nodeIds,
        ReadOnlySpan<MultiChannelAssociationDestination> endpointDestinations)
    {
        // 1 (group ID) + node IDs + optional (1 marker + 2 bytes per endpoint destination)
        int length = 1 + nodeIds.Length;
        if (endpointDestinations.Length > 0)
        {
            length += 1 + (endpointDestinations.Length * 2);
        }

        return length;
    }

    /// <summary>
    /// Parses node IDs and endpoint destinations from a command parameter span,
    /// splitting on the marker byte.
    /// </summary>
    private static void ParseDestinations(
        ReadOnlySpan<byte> data,
        out HashSet<byte> nodeIds,
        out HashSet<MultiChannelAssociationDestination> endpointDestinations)
    {
        nodeIds = new HashSet<byte>();
        endpointDestinations = new HashSet<MultiChannelAssociationDestination>();

        int markerIndex = -1;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == Marker)
            {
                markerIndex = i;
                break;
            }
        }

        if (markerIndex == -1)
        {
            // No marker found; all bytes are node IDs
            for (int i = 0; i < data.Length; i++)
            {
                nodeIds.Add(data[i]);
            }
        }
        else
        {
            for (int i = 0; i < markerIndex; i++)
            {
                nodeIds.Add(data[i]);
            }

            ReadOnlySpan<byte> endpointData = data[(markerIndex + 1)..];
            for (int i = 0; i + 1 < endpointData.Length; i += 2)
            {
                endpointDestinations.Add(new MultiChannelAssociationDestination(endpointData[i], endpointData[i + 1]));
            }
        }
    }

    private readonly struct MultiChannelAssociationSetCommand : ICommand
    {
        public MultiChannelAssociationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationSetCommand Create(
            byte groupId,
            ReadOnlySpan<byte> nodeIds,
            ReadOnlySpan<MultiChannelAssociationDestination> endpointDestinations)
        {
            int length = CalculateParameterLength(nodeIds, endpointDestinations);
            Span<byte> commandParameters = stackalloc byte[length];
            EncodeDestinations(commandParameters, groupId, nodeIds, endpointDestinations);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultiChannelAssociationSetCommand(frame);
        }
    }

    private readonly struct MultiChannelAssociationGetCommand : ICommand
    {
        public MultiChannelAssociationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationGetCommand Create(byte groupId)
        {
            ReadOnlySpan<byte> commandParameters = [groupId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultiChannelAssociationGetCommand(frame);
        }
    }

    private readonly struct MultiChannelAssociationReportCommand : ICommand
    {
        public MultiChannelAssociationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The association group identifier.
        /// </summary>
        public byte GroupingIdentifier => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The maximum number of destinations supported in this group.
        /// </summary>
        public byte MaxNodesSupported => Frame.CommandParameters.Span[1];

        /// <summary>
        /// The number of reports to follow.
        /// </summary>
        public byte ReportsToFollow => Frame.CommandParameters.Span[2];

        /// <summary>
        /// The node IDs in this association group (before the marker).
        /// </summary>
        public HashSet<byte> NodeIds
        {
            get
            {
                ParseDestinations(Frame.CommandParameters.Span[3..], out HashSet<byte> nodeIds, out _);
                return nodeIds;
            }
        }

        /// <summary>
        /// The endpoint destinations in this association group (after the marker).
        /// </summary>
        public HashSet<MultiChannelAssociationDestination> EndpointDestinations
        {
            get
            {
                ParseDestinations(Frame.CommandParameters.Span[3..], out _, out HashSet<MultiChannelAssociationDestination> endpointDestinations);
                return endpointDestinations;
            }
        }
    }

    private readonly struct MultiChannelAssociationRemoveCommand : ICommand
    {
        public MultiChannelAssociationRemoveCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Remove;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationRemoveCommand Create(
            byte groupId,
            ReadOnlySpan<byte> nodeIds,
            ReadOnlySpan<MultiChannelAssociationDestination> endpointDestinations)
        {
            int length = CalculateParameterLength(nodeIds, endpointDestinations);
            Span<byte> commandParameters = stackalloc byte[length];
            EncodeDestinations(commandParameters, groupId, nodeIds, endpointDestinations);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultiChannelAssociationRemoveCommand(frame);
        }
    }

    private readonly struct MultiChannelAssociationSupportedGroupingsGetCommand : ICommand
    {
        public MultiChannelAssociationSupportedGroupingsGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.SupportedGroupingsGet;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationSupportedGroupingsGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultiChannelAssociationSupportedGroupingsGetCommand(frame);
        }
    }

    private readonly struct MultiChannelAssociationSupportedGroupingsReportCommand : ICommand
    {
        public MultiChannelAssociationSupportedGroupingsReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.SupportedGroupingsReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The number of supported association groups.
        /// </summary>
        public byte SupportedGroupingsCount => Frame.CommandParameters.Span[0];
    }
}
