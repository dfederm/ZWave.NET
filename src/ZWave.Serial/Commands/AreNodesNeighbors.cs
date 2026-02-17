namespace ZWave.Serial.Commands;

/// <summary>
/// Check if two nodes are marked as being within direct range of each other.
/// </summary>
public readonly struct AreNodesNeighborsRequest : ICommand<AreNodesNeighborsRequest>
{
    public AreNodesNeighborsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AreNodesNeighbors;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to check if two nodes are neighbors.
    /// </summary>
    /// <param name="nodeId1">The first node ID.</param>
    /// <param name="nodeId2">The second node ID.</param>
    public static AreNodesNeighborsRequest Create(byte nodeId1, byte nodeId2)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId1, nodeId2];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AreNodesNeighborsRequest(frame);
    }

    public static AreNodesNeighborsRequest Create(DataFrame frame) => new AreNodesNeighborsRequest(frame);
}

public readonly struct AreNodesNeighborsResponse : ICommand<AreNodesNeighborsResponse>
{
    public AreNodesNeighborsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.AreNodesNeighbors;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the two nodes are neighbors.
    /// </summary>
    public bool AreNeighbors => Frame.CommandParameters.Span[0] != 0;

    public static AreNodesNeighborsResponse Create(DataFrame frame) => new AreNodesNeighborsResponse(frame);
}
