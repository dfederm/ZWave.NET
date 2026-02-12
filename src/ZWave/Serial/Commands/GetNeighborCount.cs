namespace ZWave.Serial.Commands;

/// <summary>
/// Get the number of neighbors the specified node has registered.
/// </summary>
public readonly struct GetNeighborCountRequest : ICommand<GetNeighborCountRequest>
{
    public GetNeighborCountRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetNeighborCount;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to get the neighbor count for a node.
    /// </summary>
    /// <param name="nodeId">The node ID to query.</param>
    public static GetNeighborCountRequest Create(byte nodeId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetNeighborCountRequest(frame);
    }

    public static GetNeighborCountRequest Create(DataFrame frame) => new GetNeighborCountRequest(frame);
}

public readonly struct GetNeighborCountResponse : ICommand<GetNeighborCountResponse>
{
    public GetNeighborCountResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetNeighborCount;

    public DataFrame Frame { get; }

    /// <summary>
    /// The number of neighbors the node has registered.
    /// </summary>
    public byte Count => Frame.CommandParameters.Span[0];

    public static GetNeighborCountResponse Create(DataFrame frame) => new GetNeighborCountResponse(frame);
}
