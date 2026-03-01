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
/// Represents an End Point destination in a Multi Channel association.
/// </summary>
/// <remarks>
/// <para>
/// An End Point destination identifies a specific endpoint on a node. Use the constructor
/// for a single endpoint destination, or the <see cref="EndPointDestination(byte, ReadOnlySpan{byte})"/>
/// overload for a destination
/// destination targeting multiple endpoints simultaneously.
/// </para>
/// <para>
/// When <see cref="IsBitAddress"/> is true, <see cref="Destination"/> is a bit mask where
/// bit 0 = endpoint 1, bit 1 = endpoint 2, etc. (endpoints 1–7).
/// When false, <see cref="Destination"/> is the single endpoint index (0–127).
/// </para>
/// </remarks>
public readonly record struct EndPointDestination
{
    /// <summary>
    /// Creates an End Point destination targeting a single endpoint on a node.
    /// </summary>
    /// <param name="nodeId">The NodeID of the destination.</param>
    /// <param name="endPoint">The endpoint index (0–127).</param>
    public EndPointDestination(byte nodeId, byte endPoint)
    {
        NodeId = nodeId;
        IsBitAddress = false;
        Destination = endPoint;
    }

    internal EndPointDestination(byte nodeId, bool isBitAddress, byte destination)
    {
        NodeId = nodeId;
        IsBitAddress = isBitAddress;
        Destination = destination;
    }

    /// <summary>
    /// Creates an End Point destination targeting multiple endpoints on a node.
    /// </summary>
    /// <param name="nodeId">The NodeID of the destination.</param>
    /// <param name="endPoints">The endpoint indices to target (each must be 1–7).</param>
    public EndPointDestination(byte nodeId, ReadOnlySpan<byte> endPoints)
    {
        byte bitMask = 0;
        foreach (byte ep in endPoints)
        {
            if (ep < 1 || ep > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(endPoints), ep, "Bit-addressed endpoints must be between 1 and 7.");
            }

            bitMask |= (byte)(1 << (ep - 1));
        }

        NodeId = nodeId;
        IsBitAddress = true;
        Destination = bitMask;
    }

    /// <summary>
    /// The NodeID of the destination.
    /// </summary>
    public byte NodeId { get; }

    /// <summary>
    /// Whether the destination is specified as a bit mask targeting multiple endpoints.
    /// </summary>
    public bool IsBitAddress { get; }

    /// <summary>
    /// The destination endpoint index (0–127) or bit mask (when <see cref="IsBitAddress"/> is true).
    /// </summary>
    public byte Destination { get; }
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
        switch ((MultiChannelAssociationCommand)frame.CommandId)
        {
            case MultiChannelAssociationCommand.Report:
            {
                MultiChannelAssociationReport report = MultiChannelAssociationReportCommand.Parse(frame, Logger);
                UpdateGroupReport(report);
                OnReportReceived?.Invoke(report);
                break;
            }
            case MultiChannelAssociationCommand.SupportedGroupingsReport:
            {
                byte groupings = MultiChannelAssociationSupportedGroupingsReportCommand.Parse(frame, Logger);
                SupportedGroupings = groupings;
                break;
            }
        }
    }
}
