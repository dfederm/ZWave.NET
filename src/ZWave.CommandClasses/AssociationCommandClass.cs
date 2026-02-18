namespace ZWave.CommandClasses;

public enum AssociationCommand : byte
{
    /// <summary>
    /// Add node IDs to an association group.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the node IDs in an association group.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the node IDs in an association group.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Remove node IDs from an association group.
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

    /// <summary>
    /// Request the group that a specific node ID belongs to (v2).
    /// </summary>
    SpecificGroupGet = 0x0B,

    /// <summary>
    /// Advertise the group that a specific node ID belongs to (v2).
    /// </summary>
    SpecificGroupReport = 0x0C,
}

/// <summary>
/// Represents the state of an association group.
/// </summary>
public readonly struct AssociationGroup
{
    public AssociationGroup(
        byte maxNodesSupported,
        IReadOnlySet<byte> nodeIds)
    {
        MaxNodesSupported = maxNodesSupported;
        NodeIds = nodeIds;
    }

    /// <summary>
    /// Gets the maximum number of node IDs supported in this group.
    /// </summary>
    public byte MaxNodesSupported { get; }

    /// <summary>
    /// Gets the node IDs currently in this group.
    /// </summary>
    public IReadOnlySet<byte> NodeIds { get; }
}

[CommandClass(CommandClassId.Association)]
public sealed class AssociationCommandClass : CommandClass<AssociationCommand>
{
    private Dictionary<byte, AssociationGroup>? _groups;

    internal AssociationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the number of supported association groups.
    /// </summary>
    public byte? SupportedGroupings { get; private set; }

    /// <summary>
    /// Gets the association groups and their node ID assignments.
    /// </summary>
    public IReadOnlyDictionary<byte, AssociationGroup>? Groups => _groups;

    /// <inheritdoc />
    public override bool? IsCommandSupported(AssociationCommand command)
        => command switch
        {
            AssociationCommand.Set => true,
            AssociationCommand.Get => true,
            AssociationCommand.Remove => true,
            AssociationCommand.SupportedGroupingsGet => true,
            AssociationCommand.SpecificGroupGet => Version.HasValue ? Version >= 2 : null,
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
        var command = AssociationSupportedGroupingsGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<AssociationSupportedGroupingsReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedGroupings!.Value;
    }

    /// <summary>
    /// Request the node IDs in an association group.
    /// </summary>
    public async Task<AssociationGroup> GetAsync(byte groupId, CancellationToken cancellationToken)
    {
        var command = AssociationGetCommand.Create(groupId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<AssociationReportCommand>(
            predicate: frame =>
            {
                var report = new AssociationReportCommand(frame);
                return report.GroupingIdentifier == groupId;
            },
            cancellationToken).ConfigureAwait(false);
        return _groups![groupId];
    }

    /// <summary>
    /// Add node IDs to an association group.
    /// </summary>
    public Task SetAsync(byte groupId, ReadOnlySpan<byte> nodeIds, CancellationToken cancellationToken)
    {
        var command = AssociationSetCommand.Create(groupId, nodeIds);
        return SendCommandAsync(command, cancellationToken);
    }

    /// <summary>
    /// Remove node IDs from an association group. An empty node ID list removes all associations.
    /// </summary>
    public Task RemoveAsync(byte groupId, ReadOnlySpan<byte> nodeIds, CancellationToken cancellationToken)
    {
        var command = AssociationRemoveCommand.Create(groupId, nodeIds);
        return SendCommandAsync(command, cancellationToken);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((AssociationCommand)frame.CommandId)
        {
            case AssociationCommand.Set:
            case AssociationCommand.Get:
            case AssociationCommand.Remove:
            case AssociationCommand.SupportedGroupingsGet:
            case AssociationCommand.SpecificGroupGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case AssociationCommand.Report:
            {
                var command = new AssociationReportCommand(frame);
                byte groupId = command.GroupingIdentifier;
                byte maxNodesSupported = command.MaxNodesSupported;
                HashSet<byte> nodeIds = command.NodeIds;

                _groups ??= new Dictionary<byte, AssociationGroup>();
                _groups[groupId] = new AssociationGroup(maxNodesSupported, nodeIds);
                break;
            }
            case AssociationCommand.SupportedGroupingsReport:
            {
                var command = new AssociationSupportedGroupingsReportCommand(frame);
                SupportedGroupings = command.SupportedGroupingsCount;
                break;
            }
            case AssociationCommand.SpecificGroupReport:
            {
                // Specific group report is handled by the caller if needed.
                break;
            }
        }
    }

    private readonly struct AssociationSetCommand : ICommand
    {
        public AssociationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static AssociationSetCommand Create(byte groupId, ReadOnlySpan<byte> nodeIds)
        {
            Span<byte> commandParameters = stackalloc byte[1 + nodeIds.Length];
            commandParameters[0] = groupId;
            nodeIds.CopyTo(commandParameters[1..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationSetCommand(frame);
        }
    }

    private readonly struct AssociationGetCommand : ICommand
    {
        public AssociationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static AssociationGetCommand Create(byte groupId)
        {
            ReadOnlySpan<byte> commandParameters = [groupId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationGetCommand(frame);
        }
    }

    private readonly struct AssociationReportCommand : ICommand
    {
        public AssociationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The association group identifier.
        /// </summary>
        public byte GroupingIdentifier => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The maximum number of node IDs supported in this group.
        /// </summary>
        public byte MaxNodesSupported => Frame.CommandParameters.Span[1];

        /// <summary>
        /// The number of reports to follow.
        /// </summary>
        public byte ReportsToFollow => Frame.CommandParameters.Span[2];

        /// <summary>
        /// The node IDs in this association group.
        /// </summary>
        public HashSet<byte> NodeIds
        {
            get
            {
                var nodeIds = new HashSet<byte>();
                ReadOnlySpan<byte> nodeIdList = Frame.CommandParameters.Span[3..];
                for (int i = 0; i < nodeIdList.Length; i++)
                {
                    nodeIds.Add(nodeIdList[i]);
                }

                return nodeIds;
            }
        }
    }

    private readonly struct AssociationRemoveCommand : ICommand
    {
        public AssociationRemoveCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.Remove;

        public CommandClassFrame Frame { get; }

        public static AssociationRemoveCommand Create(byte groupId, ReadOnlySpan<byte> nodeIds)
        {
            Span<byte> commandParameters = stackalloc byte[1 + nodeIds.Length];
            commandParameters[0] = groupId;
            nodeIds.CopyTo(commandParameters[1..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationRemoveCommand(frame);
        }
    }

    private readonly struct AssociationSupportedGroupingsGetCommand : ICommand
    {
        public AssociationSupportedGroupingsGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.SupportedGroupingsGet;

        public CommandClassFrame Frame { get; }

        public static AssociationSupportedGroupingsGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new AssociationSupportedGroupingsGetCommand(frame);
        }
    }

    private readonly struct AssociationSupportedGroupingsReportCommand : ICommand
    {
        public AssociationSupportedGroupingsReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.SupportedGroupingsReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The number of supported association groups.
        /// </summary>
        public byte SupportedGroupingsCount => Frame.CommandParameters.Span[0];
    }
}
