namespace ZWave.Serial.Commands;

/// <summary>
/// Test if a node ID is stored in the failed node ID list.
/// </summary>
public readonly struct IsNodeFailedRequest : ICommand<IsNodeFailedRequest>
{
    public IsNodeFailedRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.IsNodeFailed;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to check if a node is in the failed node list.
    /// </summary>
    /// <param name="nodeId">The node ID to check.</param>
    public static IsNodeFailedRequest Create(ushort nodeId, NodeIdType nodeIdType)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize];
        nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new IsNodeFailedRequest(frame);
    }

    public static IsNodeFailedRequest Create(DataFrame frame, CommandParsingContext context) => new IsNodeFailedRequest(frame);
}

public readonly struct IsNodeFailedResponse : ICommand<IsNodeFailedResponse>
{
    public IsNodeFailedResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.IsNodeFailed;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the node is in the failed node list.
    /// </summary>
    public bool IsNodeFailed => Frame.CommandParameters.Span[0] != 0;

    public static IsNodeFailedResponse Create(DataFrame frame, CommandParsingContext context) => new IsNodeFailedResponse(frame);
}
