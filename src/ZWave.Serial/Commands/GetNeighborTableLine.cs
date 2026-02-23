namespace ZWave.Serial.Commands;

/// <summary>
/// Read out neighbor information from the protocol for a given node.
/// </summary>
public readonly struct GetNeighborTableLineRequest : ICommand<GetNeighborTableLineRequest>
{
    public GetNeighborTableLineRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetNeighborTableLine;

    public DataFrame Frame { get; }

    /// <param name="nodeId">The node ID to get the neighbor table line for.</param>
    /// <param name="removeBadLink">Remove the latest bad link from the neighbor table line.</param>
    /// <param name="removeNonRepeaters">Remove non-repeater nodes from the neighbor table line.</param>
    public static GetNeighborTableLineRequest Create(ushort nodeId, bool removeBadLink, bool removeNonRepeaters)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            (byte)nodeId, // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
            (byte)(removeBadLink ? 1 : 0),
            (byte)(removeNonRepeaters ? 1 : 0),
        ];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetNeighborTableLineRequest(frame);
    }

    public static GetNeighborTableLineRequest Create(DataFrame frame) => new GetNeighborTableLineRequest(frame);
}

public readonly struct GetNeighborTableLineResponse : ICommand<GetNeighborTableLineResponse>
{
    public GetNeighborTableLineResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetNeighborTableLine;

    public DataFrame Frame { get; }

    /// <summary>
    /// The neighbor bitmask. Each bit represents a node ID (bit 0 of byte 0 = node 1, etc.)
    /// </summary>
    public IReadOnlySet<ushort> NeighborNodeIds
        => CommandDataParsingHelpers.ParseNodeBitmask(Frame.CommandParameters.Span, baseNodeId: 1);

    public static GetNeighborTableLineResponse Create(DataFrame frame) => new GetNeighborTableLineResponse(frame);
}
