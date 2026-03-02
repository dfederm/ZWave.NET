using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands for the Association Command Class.
/// </summary>
public enum AssociationCommand : byte
{
    /// <summary>
    /// Add NodeID destinations to a given association group.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current destinations of a given association group.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current destinations of a given association group.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Remove NodeID destinations from a given association group.
    /// </summary>
    Remove = 0x04,

    /// <summary>
    /// Request the number of association groups that this node supports.
    /// </summary>
    SupportedGroupingsGet = 0x05,

    /// <summary>
    /// Advertise the maximum number of association groups implemented by this node.
    /// </summary>
    SupportedGroupingsReport = 0x06,

    /// <summary>
    /// Request the association group representing the most recently detected button.
    /// </summary>
    SpecificGroupGet = 0x0B,

    /// <summary>
    /// Advertise the association group representing the most recently detected button.
    /// </summary>
    SpecificGroupReport = 0x0C,
}

/// <summary>
/// Implementation of the Association Command Class (CC:0085, versions 1-4).
/// </summary>
/// <remarks>
/// The Association Command Class is used to manage associations to NodeID destinations.
/// A NodeID destination may be a simple device or the Root Device of a Multi Channel device.
/// </remarks>
[CommandClass(CommandClassId.Association)]
public sealed partial class AssociationCommandClass : CommandClass<AssociationCommand>
{
    internal AssociationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    internal override CommandClassCategory Category => CommandClassCategory.Management;

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

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        // Association Reports and Supported Groupings Reports are only sent in response
        // to Get commands (per spec CC:0085.01.02.11.001 and CC:0085.01.05.11.001),
        // so there are no unsolicited commands to handle.
    }
}
