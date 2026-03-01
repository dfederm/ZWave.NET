namespace ZWave.CommandClasses;

public sealed partial class MultiChannelAssociationCommandClass
{
    /// <summary>
    /// Add destinations to a given association group.
    /// </summary>
    /// <param name="groupingIdentifier">The association group identifier (1–255).</param>
    /// <param name="nodeIdDestinations">NodeID-only destinations to add.</param>
    /// <param name="endPointDestinations">End Point destinations to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SetAsync(
        byte groupingIdentifier,
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndPointDestination> endPointDestinations,
        CancellationToken cancellationToken)
    {
        MultiChannelAssociationSetCommand command = MultiChannelAssociationSetCommand.Create(
            groupingIdentifier,
            nodeIdDestinations,
            endPointDestinations);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove destinations from a given association group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per the spec, the combination of <paramref name="groupingIdentifier"/>,
    /// <paramref name="nodeIdDestinations"/>, and <paramref name="endPointDestinations"/>
    /// determines the behavior:
    /// </para>
    /// <list type="bullet">
    /// <item>GroupId &gt; 0, with destinations: Remove specified destinations from the group.</item>
    /// <item>GroupId &gt; 0, no destinations: Remove all destinations from the group.</item>
    /// <item>GroupId = 0, with destinations: Remove specified destinations from all groups.</item>
    /// <item>GroupId = 0, no destinations: Remove all destinations from all groups.</item>
    /// </list>
    /// </remarks>
    /// <param name="groupingIdentifier">The association group identifier, or 0 to target all groups.</param>
    /// <param name="nodeIdDestinations">NodeID-only destinations to remove.</param>
    /// <param name="endPointDestinations">End Point destinations to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RemoveAsync(
        byte groupingIdentifier,
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndPointDestination> endPointDestinations,
        CancellationToken cancellationToken)
    {
        MultiChannelAssociationRemoveCommand command = MultiChannelAssociationRemoveCommand.Create(
            groupingIdentifier,
            nodeIdDestinations,
            endPointDestinations);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static int ComputeDestinationPayloadLength(
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndPointDestination> endPointDestinations)
    {
        int length = nodeIdDestinations.Count;
        if (endPointDestinations.Count > 0)
        {
            // Marker byte + 2 bytes per End Point destination (NodeID + BitAddress|EndPoint).
            length += 1 + (endPointDestinations.Count * 2);
        }

        return length;
    }

    private static void WriteDestinationPayload(
        Span<byte> buffer,
        int offset,
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndPointDestination> endPointDestinations)
    {
        // Write NodeID destinations.
        for (int i = 0; i < nodeIdDestinations.Count; i++)
        {
            buffer[offset++] = nodeIdDestinations[i];
        }

        if (endPointDestinations.Count > 0)
        {
            // Write marker.
            buffer[offset++] = Marker;

            // Write End Point destinations.
            for (int i = 0; i < endPointDestinations.Count; i++)
            {
                EndPointDestination dest = endPointDestinations[i];
                buffer[offset++] = dest.NodeId;
                buffer[offset++] = (byte)((dest.IsBitAddress ? 0x80 : 0x00) | (dest.Destination & 0x7F));
            }
        }
    }

    internal readonly struct MultiChannelAssociationSetCommand : ICommand
    {
        public MultiChannelAssociationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationSetCommand Create(
            byte groupingIdentifier,
            IReadOnlyList<byte> nodeIdDestinations,
            IReadOnlyList<EndPointDestination> endPointDestinations)
        {
            int payloadLength = 1 + ComputeDestinationPayloadLength(nodeIdDestinations, endPointDestinations);
            Span<byte> commandParameters = stackalloc byte[payloadLength];
            commandParameters[0] = groupingIdentifier;
            WriteDestinationPayload(commandParameters, 1, nodeIdDestinations, endPointDestinations);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultiChannelAssociationSetCommand(frame);
        }
    }

    internal readonly struct MultiChannelAssociationRemoveCommand : ICommand
    {
        public MultiChannelAssociationRemoveCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.Remove;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationRemoveCommand Create(
            byte groupingIdentifier,
            IReadOnlyList<byte> nodeIdDestinations,
            IReadOnlyList<EndPointDestination> endPointDestinations)
        {
            int payloadLength = 1 + ComputeDestinationPayloadLength(nodeIdDestinations, endPointDestinations);
            Span<byte> commandParameters = stackalloc byte[payloadLength];
            commandParameters[0] = groupingIdentifier;
            WriteDestinationPayload(commandParameters, 1, nodeIdDestinations, endPointDestinations);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultiChannelAssociationRemoveCommand(frame);
        }
    }
}
