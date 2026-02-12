namespace ZWave.Serial.Commands;

/// <summary>
/// Test if a node ID is stored in the failed node ID list.
/// </summary>
public readonly struct IsFailedNodeRequest : ICommand<IsFailedNodeRequest>
{
    public IsFailedNodeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.IsFailedNode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to check if a node is in the failed node list.
    /// </summary>
    /// <param name="nodeId">The node ID to check.</param>
    public static IsFailedNodeRequest Create(byte nodeId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new IsFailedNodeRequest(frame);
    }

    public static IsFailedNodeRequest Create(DataFrame frame) => new IsFailedNodeRequest(frame);
}

public readonly struct IsFailedNodeResponse : ICommand<IsFailedNodeResponse>
{
    public IsFailedNodeResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.IsFailedNode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the node is in the failed node list.
    /// </summary>
    public bool IsFailedNode => Frame.CommandParameters.Span[0] != 0;

    public static IsFailedNodeResponse Create(DataFrame frame) => new IsFailedNodeResponse(frame);
}
