using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public enum MultiChannelAssociationCommand : byte
{
    /// <summary>
    /// Add destinations to a given association group.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current destinations of a given association group.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current destinations for a given association group.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Remove destinations from a given association group.
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
}

/// <summary>
/// Represents an endpoint destination in a Multi Channel association.
/// </summary>
public readonly record struct EndpointDestination
{
    /// <summary>
    /// Creates an endpoint destination targeting a single endpoint on a node.
    /// </summary>
    public EndpointDestination(byte nodeId, byte endpoint)
    {
        NodeId = nodeId;
        Endpoints = [endpoint];
    }

    /// <summary>
    /// Creates an endpoint destination targeting multiple endpoints on a node.
    /// </summary>
    public EndpointDestination(byte nodeId, ReadOnlySpan<byte> endpoints)
    {
        if (endpoints.Length == 0)
        {
            throw new ArgumentException("At least one endpoint must be specified.", nameof(endpoints));
        }

        NodeId = nodeId;
        Endpoints = endpoints.ToArray();
    }

    /// <summary>
    /// The NodeID of the destination.
    /// </summary>
    public byte NodeId { get; }

    /// <summary>
    /// The endpoint indices on this node.
    /// </summary>
    public IReadOnlyList<byte> Endpoints { get; }
}

[CommandClass(CommandClassId.MultiChannelAssociation)]
public sealed partial class MultiChannelAssociationCommandClass : CommandClass<MultiChannelAssociationCommand>
{
    /// <summary>
    /// The marker byte value used to separate NodeID destinations from End Point destinations.
    /// </summary>
    internal const byte Marker = 0x00;

    internal MultiChannelAssociationCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    internal override CommandClassCategory Category => CommandClassCategory.Management;

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

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        // Association Reports and Supported Groupings Reports are only sent in response
        // to Get commands (per spec CC:008E.02.02.11.001 and CC:008E.02.05.11.001),
        // so there are no unsolicited commands to handle.
    }
}
