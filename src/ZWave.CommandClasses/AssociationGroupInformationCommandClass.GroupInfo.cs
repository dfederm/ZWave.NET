using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Well-known AGI profile categories.
/// </summary>
/// <remarks>
/// Profile identifiers consist of a category byte (MSB) and a specific identifier byte (LSB).
/// </remarks>
public enum AssociationGroupProfileCategory : byte
{
    /// <summary>
    /// General profile category. LSB 0x00 = N/A, 0x01 = Lifeline.
    /// </summary>
    General = 0x00,

    /// <summary>
    /// Control profile category. LSB indicates the key number (0x01-0x20).
    /// </summary>
    Control = 0x20,

    /// <summary>
    /// Sensor profile category. LSB is the multilevel sensor type.
    /// </summary>
    Sensor = 0x31,

    /// <summary>
    /// Meter profile category (v2+). LSB is the meter type.
    /// </summary>
    Meter = 0x32,

    /// <summary>
    /// Irrigation profile category (v3+). LSB indicates the channel number (0x01-0x20).
    /// </summary>
    Irrigation = 0x6B,

    /// <summary>
    /// Notification profile category. LSB is the notification type.
    /// </summary>
    Notification = 0x71,
}

/// <summary>
/// Represents the profile of an association group.
/// </summary>
/// <remarks>
/// The profile defines the scope of events which triggers the transmission of commands
/// to the actual association group. The profile consists of a 2-byte identifier:
/// the MSB identifies the profile category and the LSB identifies the specific profile
/// within the category.
/// </remarks>
public readonly record struct AssociationGroupProfile(
    /// <summary>
    /// The profile category (MSB).
    /// </summary>
    byte Category,

    /// <summary>
    /// The profile-specific identifier (LSB).
    /// </summary>
    byte Identifier);

/// <summary>
/// Represents the properties of a single association group from a Group Info Report.
/// </summary>
public readonly record struct AssociationGroupInfo(
    /// <summary>
    /// The association group identifier.
    /// </summary>
    byte GroupingIdentifier,

    /// <summary>
    /// The profile of this association group.
    /// </summary>
    AssociationGroupProfile Profile);

public sealed partial class AssociationGroupInformationCommandClass
{
    /// <summary>
    /// Gets the cached group info, keyed by grouping identifier.
    /// </summary>
    public IReadOnlyDictionary<byte, AssociationGroupInfo> GroupInfo => _groupInfo;

    private readonly Dictionary<byte, AssociationGroupInfo> _groupInfo = [];

    /// <summary>
    /// Gets whether the AGI information is dynamic and may change at runtime.
    /// </summary>
    /// <remarks>
    /// If true, a controlling node should perform periodic cache refreshes.
    /// If false, the information is static and should not be re-queried.
    /// </remarks>
    public bool? IsDynamic { get; private set; }

    /// <summary>
    /// Request the properties of all association groups using List Mode.
    /// </summary>
    /// <remarks>
    /// Uses List Mode to request all group properties at once. Per spec
    /// CC:0059.01.04.13.001, the response may span multiple report frames.
    /// Since the Group Info Report has no "reports to follow" field, the expected
    /// group count is determined from the Association or Multi Channel Association CC
    /// (whichever has been interviewed). If the expected count is not available,
    /// only a single report frame is read.
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all association group info entries.</returns>
    public async Task<IReadOnlyList<AssociationGroupInfo>> GetGroupInfoAsync(CancellationToken cancellationToken)
    {
        var command = GroupInfoGetCommand.Create(listMode: true, groupingIdentifier: 0);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        byte expectedGroupCount = GetAssociationGroupCount();
        List<AssociationGroupInfo> allGroups = [];
        bool dynamicInfo = false;

        do
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<GroupInfoReportCommand>(cancellationToken).ConfigureAwait(false);
            (bool reportDynamicInfo, List<AssociationGroupInfo> groups) = GroupInfoReportCommand.Parse(reportFrame, Logger);
            dynamicInfo |= reportDynamicInfo;
            allGroups.AddRange(groups);
        }
        while (expectedGroupCount > 0 && allGroups.Count < expectedGroupCount);

        IsDynamic = dynamicInfo;

        foreach (AssociationGroupInfo info in allGroups)
        {
            _groupInfo[info.GroupingIdentifier] = info;
        }

        return allGroups;
    }

    /// <summary>
    /// Request the properties of a single association group.
    /// </summary>
    /// <param name="groupingIdentifier">The association group identifier (1-255).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The association group info for the specified group.</returns>
    public async Task<AssociationGroupInfo> GetGroupInfoAsync(byte groupingIdentifier, CancellationToken cancellationToken)
    {
        var command = GroupInfoGetCommand.Create(listMode: false, groupingIdentifier: groupingIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<GroupInfoReportCommand>(
            frame =>
            {
                // Non-list-mode reports have Group Count = 1, and the first group entry's
                // grouping identifier is at offset 1 in the command parameters.
                return frame.CommandParameters.Length >= 2
                    && frame.CommandParameters.Span[1] == groupingIdentifier;
            },
            cancellationToken).ConfigureAwait(false);
        (bool dynamicInfo, List<AssociationGroupInfo> groups) = GroupInfoReportCommand.Parse(reportFrame, Logger);
        IsDynamic = dynamicInfo;

        if (groups.Count == 0)
        {
            Logger.LogWarning(
                "Association Group Info Report for group {GroupId} contained no group entries",
                groupingIdentifier);
            throw new ZWaveException(
                ZWaveErrorCode.InvalidPayload,
                "Association Group Info Report contained no group entries");
        }

        AssociationGroupInfo info = groups[0];
        _groupInfo[info.GroupingIdentifier] = info;
        return info;
    }

    internal readonly struct GroupInfoGetCommand : ICommand
    {
        public GroupInfoGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.GroupInfoGet;

        public CommandClassFrame Frame { get; }

        public static GroupInfoGetCommand Create(bool listMode, byte groupingIdentifier)
        {
            // Byte 0: [Refresh Cache (1 bit)] [List Mode (1 bit)] [Reserved (6 bits)]
            byte flags = 0;
            if (listMode)
            {
                flags |= 0b0100_0000;
            }

            ReadOnlySpan<byte> commandParameters = [flags, groupingIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new GroupInfoGetCommand(frame);
        }
    }

    internal readonly struct GroupInfoReportCommand : ICommand
    {
        public GroupInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.GroupInfoReport;

        public CommandClassFrame Frame { get; }

        public static GroupInfoReportCommand Create(
            bool listMode,
            bool dynamicInfo,
            IReadOnlyList<AssociationGroupInfo> groups)
        {
            const int GroupEntrySize = 7;
            Span<byte> commandParameters = stackalloc byte[1 + (groups.Count * GroupEntrySize)];

            // Byte 0: [List Mode (1 bit)] [Dynamic Info (1 bit)] [Group Count (6 bits)]
            byte flags = (byte)(groups.Count & 0b0011_1111);
            if (listMode)
            {
                flags |= 0b1000_0000;
            }

            if (dynamicInfo)
            {
                flags |= 0b0100_0000;
            }

            commandParameters[0] = flags;

            for (int i = 0; i < groups.Count; i++)
            {
                int offset = 1 + (i * GroupEntrySize);
                commandParameters[offset] = groups[i].GroupingIdentifier;
                commandParameters[offset + 1] = 0; // Mode = 0 per spec CC:0059.01.04.11.008
                commandParameters[offset + 2] = groups[i].Profile.Category;
                commandParameters[offset + 3] = groups[i].Profile.Identifier;
                commandParameters[offset + 4] = 0; // Reserved per spec CC:0059.01.04.11.00A
                commandParameters[offset + 5] = 0; // Event Code MSB = 0 per spec CC:0059.01.04.11.00B
                commandParameters[offset + 6] = 0; // Event Code LSB = 0
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new GroupInfoReportCommand(frame);
        }

        /// <summary>
        /// Parse an Association Group Info Report frame.
        /// </summary>
        /// <returns>The dynamic info flag and the list of group info entries.</returns>
        public static (bool DynamicInfo, List<AssociationGroupInfo> Groups) Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: flags byte (1)
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Association Group Info Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Group Info Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            // Byte 0: [List Mode (1 bit)] [Dynamic Info (1 bit)] [Group Count (6 bits)]
            bool dynamicInfo = (span[0] & 0b0100_0000) != 0;
            int groupCount = span[0] & 0b0011_1111;

            // Each group entry is 7 bytes:
            //   Grouping Identifier (1) + Mode (1) + Profile MSB (1) + Profile LSB (1)
            //   + Reserved (1) + Event Code MSB (1) + Event Code LSB (1)
            const int GroupEntrySize = 7;
            int requiredLength = 1 + (groupCount * GroupEntrySize);
            if (frame.CommandParameters.Length < requiredLength)
            {
                logger.LogWarning(
                    "Association Group Info Report frame is too short for {GroupCount} groups (need {Required} bytes, have {Available})",
                    groupCount,
                    requiredLength,
                    frame.CommandParameters.Length);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Group Info Report frame is too short for declared group count");
            }

            List<AssociationGroupInfo> groups = new List<AssociationGroupInfo>(groupCount);
            for (int i = 0; i < groupCount; i++)
            {
                int offset = 1 + (i * GroupEntrySize);
                byte groupingIdentifier = span[offset];
                // Mode at offset+1 is reserved (ignored per spec CC:0059.01.04.11.008)
                byte profileMsb = span[offset + 2];
                byte profileLsb = span[offset + 3];
                // Reserved at offset+4 (ignored per spec CC:0059.01.04.11.00A)
                // Event Code at offset+5..6 (ignored per spec CC:0059.01.04.11.00B)

                AssociationGroupProfile profile = new(profileMsb, profileLsb);
                groups.Add(new AssociationGroupInfo(groupingIdentifier, profile));
            }

            return (dynamicInfo, groups);
        }
    }
}
