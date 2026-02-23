namespace ZWave.Serial.Commands;

/// <summary>
/// Request entries from the controller routing table.
/// </summary>
public readonly struct GetRoutingTableEntriesRequest : ICommand<GetRoutingTableEntriesRequest>
{
    public GetRoutingTableEntriesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetRoutingTableEntries;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to get routing table entries for a given node.
    /// </summary>
    /// <param name="nodeId">The node ID to get routing table entries for. Always 8-bit per spec (routing tables are Z-Wave Classic only).</param>
    public static GetRoutingTableEntriesRequest Create(byte nodeId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetRoutingTableEntriesRequest(frame);
    }

    public static GetRoutingTableEntriesRequest Create(DataFrame frame) => new GetRoutingTableEntriesRequest(frame);
}

public readonly struct GetRoutingTableEntriesResponse : ICommand<GetRoutingTableEntriesResponse>
{
    public GetRoutingTableEntriesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetRoutingTableEntries;

    public DataFrame Frame { get; }

    /// <summary>
    /// The number of routing table entries.
    /// </summary>
    public int RoutesCount => Frame.CommandParameters.Span[0];

    /// <summary>
    /// Gets the routing table entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the route entry.</param>
    public RoutingTableEntry GetRoute(int index)
    {
        if (index < 0 || index >= RoutesCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int offset = 1 + (index * 6);
        return new RoutingTableEntry(Frame.CommandParameters.Slice(offset, 6));
    }

    public static GetRoutingTableEntriesResponse Create(DataFrame frame) => new GetRoutingTableEntriesResponse(frame);
}
