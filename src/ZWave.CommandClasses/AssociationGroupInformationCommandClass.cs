using System.Text;

namespace ZWave.CommandClasses;

public enum AssociationGroupInformationCommand : byte
{
    /// <summary>
    /// Request the name of an association group.
    /// </summary>
    NameGet = 0x01,

    /// <summary>
    /// Advertise the name of an association group.
    /// </summary>
    NameReport = 0x02,

    /// <summary>
    /// Request info for association groups.
    /// </summary>
    InfoGet = 0x03,

    /// <summary>
    /// Advertise info for association groups.
    /// </summary>
    InfoReport = 0x04,

    /// <summary>
    /// Request the command list for an association group.
    /// </summary>
    CommandListGet = 0x05,

    /// <summary>
    /// Advertise the command list for an association group.
    /// </summary>
    CommandListReport = 0x06,
}

/// <summary>
/// Represents information about an association group.
/// </summary>
public readonly struct AssociationGroupInfo
{
    public AssociationGroupInfo(
        string name,
        ushort profile,
        IReadOnlyList<CommandClassCommandPair> commandList)
    {
        Name = name;
        Profile = profile;
        CommandList = commandList;
    }

    /// <summary>
    /// Gets the name of the association group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the AGI profile for the association group.
    /// </summary>
    public ushort Profile { get; }

    /// <summary>
    /// Gets the list of command class/command ID pairs for the association group.
    /// </summary>
    public IReadOnlyList<CommandClassCommandPair> CommandList { get; }
}

/// <summary>
/// Represents a command class and command ID pair.
/// </summary>
public readonly struct CommandClassCommandPair
{
    public CommandClassCommandPair(CommandClassId commandClassId, byte commandId)
    {
        CommandClassId = commandClassId;
        CommandId = commandId;
    }

    /// <summary>
    /// Gets the command class identifier.
    /// </summary>
    public CommandClassId CommandClassId { get; }

    /// <summary>
    /// Gets the command identifier within the command class.
    /// </summary>
    public byte CommandId { get; }
}

[CommandClass(CommandClassId.AssociationGroupInformation)]
public sealed class AssociationGroupInformationCommandClass : CommandClass<AssociationGroupInformationCommand>
{
    private static readonly CommandClassId[] DependencyList = new[]
    {
        CommandClassId.Version,
        CommandClassId.Association,
    };

    private Dictionary<byte, string>? _groupNames;
    private Dictionary<byte, ushort>? _groupProfiles;
    private Dictionary<byte, IReadOnlyList<CommandClassCommandPair>>? _groupCommandLists;

    internal AssociationGroupInformationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the association group information keyed by group ID.
    /// </summary>
    public IReadOnlyDictionary<byte, AssociationGroupInfo>? Groups
    {
        get
        {
            if (_groupNames == null)
            {
                return null;
            }

            Dictionary<byte, AssociationGroupInfo> groups = new Dictionary<byte, AssociationGroupInfo>();
            foreach (KeyValuePair<byte, string> kvp in _groupNames)
            {
                byte groupId = kvp.Key;
                string name = kvp.Value;
                ushort profile = _groupProfiles != null && _groupProfiles.TryGetValue(groupId, out ushort p) ? p : (ushort)0;
                IReadOnlyList<CommandClassCommandPair> commandList = _groupCommandLists != null && _groupCommandLists.TryGetValue(groupId, out IReadOnlyList<CommandClassCommandPair>? cl)
                    ? cl
                    : [];
                groups[groupId] = new AssociationGroupInfo(name, profile, commandList);
            }

            return groups;
        }
    }

    /// <inheritdoc />
    internal override CommandClassId[] Dependencies => DependencyList;

    /// <inheritdoc />
    public override bool? IsCommandSupported(AssociationGroupInformationCommand command)
        => command switch
        {
            AssociationGroupInformationCommand.NameGet => true,
            AssociationGroupInformationCommand.InfoGet => true,
            AssociationGroupInformationCommand.CommandListGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        AssociationCommandClass associationCC = (AssociationCommandClass)Node.GetCommandClass(CommandClassId.Association);
        byte groupCount = associationCC.SupportedGroupings ?? 0;

        for (byte groupId = 1; groupId <= groupCount; groupId++)
        {
            _ = await GetNameAsync(groupId, cancellationToken).ConfigureAwait(false);
        }

        // Use list mode to get info for all groups at once
        await GetInfoAsync(listMode: true, groupId: 0, cancellationToken).ConfigureAwait(false);

        for (byte groupId = 1; groupId <= groupCount; groupId++)
        {
            _ = await GetCommandListAsync(groupId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request the name of an association group.
    /// </summary>
    public async Task<string> GetNameAsync(byte groupId, CancellationToken cancellationToken)
    {
        AGINameGetCommand command = AGINameGetCommand.Create(groupId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<AGINameReportCommand>(
            predicate: frame =>
            {
                AGINameReportCommand report = new AGINameReportCommand(frame);
                return report.GroupId == groupId;
            },
            cancellationToken).ConfigureAwait(false);
        return _groupNames![groupId];
    }

    /// <summary>
    /// Request info for association groups.
    /// </summary>
    public async Task GetInfoAsync(bool listMode, byte groupId, CancellationToken cancellationToken)
    {
        AGIInfoGetCommand command = AGIInfoGetCommand.Create(listMode, groupId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<AGIInfoReportCommand>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the command list for an association group.
    /// </summary>
    public async Task<IReadOnlyList<CommandClassCommandPair>> GetCommandListAsync(byte groupId, CancellationToken cancellationToken)
    {
        AGICommandListGetCommand command = AGICommandListGetCommand.Create(allowCache: true, groupId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<AGICommandListReportCommand>(
            predicate: frame =>
            {
                AGICommandListReportCommand report = new AGICommandListReportCommand(frame);
                return report.GroupId == groupId;
            },
            cancellationToken).ConfigureAwait(false);
        return _groupCommandLists![groupId];
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((AssociationGroupInformationCommand)frame.CommandId)
        {
            case AssociationGroupInformationCommand.NameGet:
            case AssociationGroupInformationCommand.InfoGet:
            case AssociationGroupInformationCommand.CommandListGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case AssociationGroupInformationCommand.NameReport:
            {
                AGINameReportCommand command = new AGINameReportCommand(frame);
                _groupNames ??= new Dictionary<byte, string>();
                _groupNames[command.GroupId] = command.Name;
                break;
            }
            case AssociationGroupInformationCommand.InfoReport:
            {
                AGIInfoReportCommand command = new AGIInfoReportCommand(frame);
                _groupProfiles ??= new Dictionary<byte, ushort>();
                foreach (AGIInfoReportCommand.GroupInfoEntry entry in command.GroupEntries)
                {
                    _groupProfiles[entry.GroupId] = entry.Profile;
                }

                break;
            }
            case AssociationGroupInformationCommand.CommandListReport:
            {
                AGICommandListReportCommand command = new AGICommandListReportCommand(frame);
                _groupCommandLists ??= new Dictionary<byte, IReadOnlyList<CommandClassCommandPair>>();
                _groupCommandLists[command.GroupId] = command.CommandList;
                break;
            }
        }
    }

    private readonly struct AGINameGetCommand : ICommand
    {
        public AGINameGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.NameGet;

        public CommandClassFrame Frame { get; }

        public static AGINameGetCommand Create(byte groupId)
        {
            ReadOnlySpan<byte> commandParameters = [groupId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AGINameGetCommand(frame);
        }
    }

    private readonly struct AGINameReportCommand : ICommand
    {
        public AGINameReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.NameReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The association group identifier.
        /// </summary>
        public byte GroupId => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The length of the name in bytes.
        /// </summary>
        public byte NameLength => Frame.CommandParameters.Span[1];

        /// <summary>
        /// The name of the association group.
        /// </summary>
        public string Name
        {
            get
            {
                byte length = NameLength;
                if (length == 0)
                {
                    return string.Empty;
                }

                ReadOnlySpan<byte> nameData = Frame.CommandParameters.Span.Slice(2, length);
                return Encoding.UTF8.GetString(nameData);
            }
        }
    }

    private readonly struct AGIInfoGetCommand : ICommand
    {
        public AGIInfoGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.InfoGet;

        public CommandClassFrame Frame { get; }

        public static AGIInfoGetCommand Create(bool listMode, byte groupId)
        {
            byte flags = listMode ? (byte)0x40 : (byte)0x00;
            ReadOnlySpan<byte> commandParameters = [flags, groupId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AGIInfoGetCommand(frame);
        }
    }

    private readonly struct AGIInfoReportCommand : ICommand
    {
        public AGIInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.InfoReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Whether list mode was used.
        /// </summary>
        public bool ListMode => (Frame.CommandParameters.Span[0] & 0x40) != 0;

        /// <summary>
        /// Whether the information is dynamic.
        /// </summary>
        public bool DynamicInfo => (Frame.CommandParameters.Span[0] & 0x80) != 0;

        /// <summary>
        /// The number of groups in this report.
        /// </summary>
        public byte GroupCount => (byte)(Frame.CommandParameters.Span[0] & 0x3F);

        /// <summary>
        /// The group info entries in this report.
        /// </summary>
        public GroupInfoEntry[] GroupEntries
        {
            get
            {
                byte count = GroupCount;
                GroupInfoEntry[] entries = new GroupInfoEntry[count];
                ReadOnlySpan<byte> data = Frame.CommandParameters.Span[1..];
                for (int i = 0; i < count; i++)
                {
                    int offset = i * 7;
                    byte groupId = data[offset];
                    ushort profile = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
                    entries[i] = new GroupInfoEntry(groupId, profile);
                }

                return entries;
            }
        }

        public readonly struct GroupInfoEntry
        {
            public GroupInfoEntry(byte groupId, ushort profile)
            {
                GroupId = groupId;
                Profile = profile;
            }

            public byte GroupId { get; }

            public ushort Profile { get; }
        }
    }

    private readonly struct AGICommandListGetCommand : ICommand
    {
        public AGICommandListGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.CommandListGet;

        public CommandClassFrame Frame { get; }

        public static AGICommandListGetCommand Create(bool allowCache, byte groupId)
        {
            byte flags = allowCache ? (byte)0x80 : (byte)0x00;
            ReadOnlySpan<byte> commandParameters = [flags, groupId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AGICommandListGetCommand(frame);
        }
    }

    private readonly struct AGICommandListReportCommand : ICommand
    {
        public AGICommandListReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.CommandListReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The association group identifier.
        /// </summary>
        public byte GroupId => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The length of the command list in bytes.
        /// </summary>
        public byte ListLength => Frame.CommandParameters.Span[1];

        /// <summary>
        /// The command class/command ID pairs in this group.
        /// </summary>
        public IReadOnlyList<CommandClassCommandPair> CommandList
        {
            get
            {
                byte length = ListLength;
                List<CommandClassCommandPair> pairs = new List<CommandClassCommandPair>();
                ReadOnlySpan<byte> data = Frame.CommandParameters.Span.Slice(2, length);
                int i = 0;
                while (i < data.Length)
                {
                    CommandClassId ccId;
                    if (data[i] >= 0xF1)
                    {
                        // Extended 2-byte CC ID
                        if (i + 2 >= data.Length)
                        {
                            break;
                        }

                        ccId = (CommandClassId)((data[i] << 8) | data[i + 1]);
                        i += 2;
                    }
                    else
                    {
                        ccId = (CommandClassId)data[i];
                        i += 1;
                    }

                    if (i >= data.Length)
                    {
                        break;
                    }

                    byte cmdId = data[i];
                    i += 1;
                    pairs.Add(new CommandClassCommandPair(ccId, cmdId));
                }

                return pairs;
            }
        }
    }
}
