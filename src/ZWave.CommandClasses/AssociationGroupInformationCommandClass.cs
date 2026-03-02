using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands for the Association Group Information Command Class.
/// </summary>
public enum AssociationGroupInformationCommand : byte
{
    /// <summary>
    /// Request the name of an association group.
    /// </summary>
    GroupNameGet = 0x01,

    /// <summary>
    /// Advertise the assigned name of an association group.
    /// </summary>
    GroupNameReport = 0x02,

    /// <summary>
    /// Request the properties of one or more association groups.
    /// </summary>
    GroupInfoGet = 0x03,

    /// <summary>
    /// Advertise the properties of one or more association groups.
    /// </summary>
    GroupInfoReport = 0x04,

    /// <summary>
    /// Request the commands that are sent via a given association group.
    /// </summary>
    CommandListGet = 0x05,

    /// <summary>
    /// Advertise the commands that are sent via an association group.
    /// </summary>
    CommandListReport = 0x06,
}

/// <summary>
/// Implementation of the Association Group Information (AGI) Command Class (CC:0059, versions 1-3).
/// </summary>
/// <remarks>
/// The AGI Command Class allows a node to advertise the capabilities of each association group
/// supported by a given application resource, including the group name, profile, and the commands
/// that are sent via each group.
/// </remarks>
[CommandClass(CommandClassId.AssociationGroupInformation)]
public sealed partial class AssociationGroupInformationCommandClass
    : CommandClass<AssociationGroupInformationCommand>
{
    // Per spec CC:0059.01.00.21.001, a node supporting AGI MUST support Association CC.
    // We depend on Association so its SupportedGroupings is available for our interview.
    // We do NOT depend on Multi Channel Association (it may not be present), but we
    // check it first in GetAssociationGroupCount since it takes priority when present.
    private static readonly CommandClassId[] CcDependencies =
    [
        CommandClassId.Version,
        CommandClassId.Association,
    ];

    internal AssociationGroupInformationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    internal override CommandClassCategory Category => CommandClassCategory.Management;

    internal override CommandClassId[] Dependencies => CcDependencies;

    /// <inheritdoc />
    public override bool? IsCommandSupported(AssociationGroupInformationCommand command)
        => command switch
        {
            AssociationGroupInformationCommand.GroupNameGet => true,
            AssociationGroupInformationCommand.GroupInfoGet => true,
            AssociationGroupInformationCommand.CommandListGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        byte groupCount = GetAssociationGroupCount();
        if (groupCount == 0)
        {
            return;
        }

        // Query each group individually for name, info, and command list.
        // This avoids the multi-report aggregation problem with List Mode
        // (the Group Info Report has no "reports to follow" field).
        for (byte groupId = 1; groupId <= groupCount; groupId++)
        {
            _ = await GetGroupNameAsync(groupId, cancellationToken).ConfigureAwait(false);
            _ = await GetGroupInfoAsync(groupId, cancellationToken).ConfigureAwait(false);
            _ = await GetCommandListAsync(groupId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the number of association groups from the Association or Multi Channel Association CC.
    /// </summary>
    /// <remarks>
    /// Multi Channel Association takes priority over Association per CL:0085.01.51.01.1.
    /// Falls back to Association CC (guaranteed present per CC:0059.01.00.21.001).
    /// </remarks>
    private byte GetAssociationGroupCount()
    {
        // Check Multi Channel Association first (takes priority when present).
        if (Endpoint.CommandClasses.ContainsKey(CommandClassId.MultiChannelAssociation))
        {
            MultiChannelAssociationCommandClass mcAssocCC =
                (MultiChannelAssociationCommandClass)Endpoint.GetCommandClass(CommandClassId.MultiChannelAssociation);
            if (mcAssocCC.SupportedGroupings.HasValue)
            {
                return mcAssocCC.SupportedGroupings.Value;
            }
        }

        // Fall back to Association CC.
        if (Endpoint.CommandClasses.ContainsKey(CommandClassId.Association))
        {
            AssociationCommandClass assocCC =
                (AssociationCommandClass)Endpoint.GetCommandClass(CommandClassId.Association);
            if (assocCC.SupportedGroupings.HasValue)
            {
                return assocCC.SupportedGroupings.Value;
            }
        }

        return 0;
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        // All AGI reports are only sent in response to Get commands (per spec).
        // There are no unsolicited commands to handle.
    }
}
