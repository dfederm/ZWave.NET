namespace ZWave.CommandClasses;

public sealed partial class MultiChannelAssociationCommandClass
{
    /// <summary>
    /// Add destinations to a given association group.
    /// </summary>
    /// <param name="groupingIdentifier">The association group identifier (1–255).</param>
    /// <param name="nodeIdDestinations">NodeID-only destinations to add.</param>
    /// <param name="endpointDestinations">End Point destinations to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SetAsync(
        byte groupingIdentifier,
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndpointDestination> endpointDestinations,
        CancellationToken cancellationToken)
    {
        MultiChannelAssociationSetCommand command = MultiChannelAssociationSetCommand.Create(
            groupingIdentifier,
            nodeIdDestinations,
            endpointDestinations);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove destinations from a given association group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per the spec, the combination of <paramref name="groupingIdentifier"/>,
    /// <paramref name="nodeIdDestinations"/>, and <paramref name="endpointDestinations"/>
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
    /// <param name="endpointDestinations">End Point destinations to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RemoveAsync(
        byte groupingIdentifier,
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndpointDestination> endpointDestinations,
        CancellationToken cancellationToken)
    {
        MultiChannelAssociationRemoveCommand command = MultiChannelAssociationRemoveCommand.Create(
            groupingIdentifier,
            nodeIdDestinations,
            endpointDestinations);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static int CountWireEntries(EndpointDestination dest)
    {
        int bitAddressable = 0;
        int other = 0;
        for (int i = 0; i < dest.Endpoints.Count; i++)
        {
            byte ep = dest.Endpoints[i];
            if (ep >= 1 && ep <= 7)
            {
                bitAddressable++;
            }
            else
            {
                other++;
            }
        }
        return (bitAddressable >= 2 ? 1 : bitAddressable) + other;
    }

    private static int ComputeDestinationPayloadLength(
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndpointDestination> endpointDestinations)
    {
        int length = nodeIdDestinations.Count;
        int wireEntryCount = 0;
        for (int i = 0; i < endpointDestinations.Count; i++)
        {
            wireEntryCount += CountWireEntries(endpointDestinations[i]);
        }
        if (wireEntryCount > 0)
        {
            length += 1 + (wireEntryCount * 2);
        }
        return length;
    }

    private static void WriteDestinationPayload(
        Span<byte> buffer,
        int offset,
        IReadOnlyList<byte> nodeIdDestinations,
        IReadOnlyList<EndpointDestination> endpointDestinations)
    {
        for (int i = 0; i < nodeIdDestinations.Count; i++)
        {
            buffer[offset++] = nodeIdDestinations[i];
        }

        bool hasEndpoints = false;
        for (int i = 0; i < endpointDestinations.Count; i++)
        {
            if (endpointDestinations[i].Endpoints.Count > 0)
            {
                hasEndpoints = true;
                break;
            }
        }

        if (hasEndpoints)
        {
            buffer[offset++] = Marker;

            for (int i = 0; i < endpointDestinations.Count; i++)
            {
                EndpointDestination dest = endpointDestinations[i];

                byte bitMask = 0;
                int bitAddressableCount = 0;
                for (int j = 0; j < dest.Endpoints.Count; j++)
                {
                    byte ep = dest.Endpoints[j];
                    if (ep >= 1 && ep <= 7)
                    {
                        bitMask |= (byte)(1 << (ep - 1));
                        bitAddressableCount++;
                    }
                }

                bool useBitAddress = bitAddressableCount >= 2;

                if (useBitAddress)
                {
                    buffer[offset++] = dest.NodeId;
                    buffer[offset++] = (byte)(0b1000_0000 | bitMask);
                }

                for (int j = 0; j < dest.Endpoints.Count; j++)
                {
                    byte ep = dest.Endpoints[j];
                    if (useBitAddress && ep >= 1 && ep <= 7)
                        continue;
                    buffer[offset++] = dest.NodeId;
                    buffer[offset++] = (byte)(ep & 0b0111_1111);
                }
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
            IReadOnlyList<EndpointDestination> endpointDestinations)
        {
            int payloadLength = 1 + ComputeDestinationPayloadLength(nodeIdDestinations, endpointDestinations);
            Span<byte> commandParameters = stackalloc byte[payloadLength];
            commandParameters[0] = groupingIdentifier;
            WriteDestinationPayload(commandParameters, 1, nodeIdDestinations, endpointDestinations);

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
            IReadOnlyList<EndpointDestination> endpointDestinations)
        {
            int payloadLength = 1 + ComputeDestinationPayloadLength(nodeIdDestinations, endpointDestinations);
            Span<byte> commandParameters = stackalloc byte[payloadLength];
            commandParameters[0] = groupingIdentifier;
            WriteDestinationPayload(commandParameters, 1, nodeIdDestinations, endpointDestinations);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultiChannelAssociationRemoveCommand(frame);
        }
    }
}
