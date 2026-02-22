namespace ZWave.Serial.Commands;

/// <summary>
/// Read out neighbor information from the protocol.
/// </summary>
public readonly struct GetRoutingInfoRequest : ICommand<GetRoutingInfoRequest>
{
    public GetRoutingInfoRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetRoutingInfo;

    public DataFrame Frame { get; }

    /// <param name="nodeId">The node ID to get routing info for.</param>
    /// <param name="removeBadNodes">Remove non-responding nodes from the routing table.</param>
    /// <param name="removeNonRepeaters">Remove non-repeater nodes from the routing table.</param>
    public static GetRoutingInfoRequest Create(ushort nodeId, bool removeBadNodes, bool removeNonRepeaters)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            (byte)nodeId, // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
            (byte)(removeBadNodes ? 1 : 0),
            (byte)(removeNonRepeaters ? 1 : 0),
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetRoutingInfoRequest(frame);
    }

    public static GetRoutingInfoRequest Create(DataFrame frame) => new GetRoutingInfoRequest(frame);
}

public readonly struct GetRoutingInfoResponse : ICommand<GetRoutingInfoResponse>
{
    public GetRoutingInfoResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetRoutingInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// The neighbor bitmask. Each bit represents a node ID (bit 0 of byte 0 = node 1, etc.)
    /// </summary>
    public HashSet<ushort> NeighborNodeIds
    {
        get
        {
            ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
            return CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);
        }
    }

    public static GetRoutingInfoResponse Create(DataFrame frame) => new GetRoutingInfoResponse(frame);
}
