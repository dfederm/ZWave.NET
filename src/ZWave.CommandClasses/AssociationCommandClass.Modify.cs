namespace ZWave.CommandClasses;

public sealed partial class AssociationCommandClass
{
    /// <summary>
    /// Add NodeID destinations to a given association group.
    /// </summary>
    /// <param name="groupingIdentifier">The association group identifier (1-255).</param>
    /// <param name="nodeIdDestinations">NodeID destinations to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SetAsync(
        byte groupingIdentifier,
        IReadOnlyList<byte> nodeIdDestinations,
        CancellationToken cancellationToken)
    {
        AssociationSetCommand command = AssociationSetCommand.Create(groupingIdentifier, nodeIdDestinations);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove NodeID destinations from a given association group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per the spec, the combination of <paramref name="groupingIdentifier"/> and
    /// <paramref name="nodeIdDestinations"/> determines the behavior:
    /// </para>
    /// <list type="bullet">
    /// <item>GroupId &gt; 0, with NodeIDs: Remove specified NodeIDs from the group.</item>
    /// <item>GroupId &gt; 0, no NodeIDs: Remove all NodeIDs from the group.</item>
    /// <item>GroupId = 0, with NodeIDs: Remove specified NodeIDs from all groups (V2+).</item>
    /// <item>GroupId = 0, no NodeIDs: Remove all NodeIDs from all groups (V2+).</item>
    /// </list>
    /// </remarks>
    /// <param name="groupingIdentifier">The association group identifier, or 0 to target all groups (V2+).</param>
    /// <param name="nodeIdDestinations">NodeID destinations to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RemoveAsync(
        byte groupingIdentifier,
        IReadOnlyList<byte> nodeIdDestinations,
        CancellationToken cancellationToken)
    {
        AssociationRemoveCommand command = AssociationRemoveCommand.Create(groupingIdentifier, nodeIdDestinations);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct AssociationSetCommand : ICommand
    {
        public AssociationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static AssociationSetCommand Create(byte groupingIdentifier, IReadOnlyList<byte> nodeIdDestinations)
        {
            Span<byte> commandParameters = stackalloc byte[1 + nodeIdDestinations.Count];
            commandParameters[0] = groupingIdentifier;
            for (int i = 0; i < nodeIdDestinations.Count; i++)
            {
                commandParameters[1 + i] = nodeIdDestinations[i];
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationSetCommand(frame);
        }
    }

    internal readonly struct AssociationRemoveCommand : ICommand
    {
        public AssociationRemoveCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Remove;

        public CommandClassFrame Frame { get; }

        public static AssociationRemoveCommand Create(byte groupingIdentifier, IReadOnlyList<byte> nodeIdDestinations)
        {
            Span<byte> commandParameters = stackalloc byte[1 + nodeIdDestinations.Count];
            commandParameters[0] = groupingIdentifier;
            for (int i = 0; i < nodeIdDestinations.Count; i++)
            {
                commandParameters[1 + i] = nodeIdDestinations[i];
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationRemoveCommand(frame);
        }
    }
}
