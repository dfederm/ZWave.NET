namespace ZWave.Serial.Commands;

/// <summary>
/// Restore protocol node information from a backup.
/// </summary>
public readonly struct StoreNodeInfoRequest : IRequestWithCallback<StoreNodeInfoRequest>
{
    public StoreNodeInfoRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.StoreNodeInfo;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    /// <summary>
    /// Create a request to store node information.
    /// </summary>
    /// <param name="nodeId">The node ID to store information for.</param>
    /// <param name="nodeInfo">The node information data (NODEINFO field).</param>
    /// <param name="sessionId">The session ID for correlating the callback.</param>
    public static StoreNodeInfoRequest Create(ushort nodeId, NodeIdType nodeIdType, ReadOnlySpan<byte> nodeInfo, byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize + nodeInfo.Length + 1];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        nodeInfo.CopyTo(commandParameters[offset..]);
        commandParameters[^1] = sessionId;

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new StoreNodeInfoRequest(frame);
    }

    public static StoreNodeInfoRequest Create(DataFrame frame, CommandParsingContext context) => new StoreNodeInfoRequest(frame);
}

/// <summary>
/// Response to a <see cref="StoreNodeInfoRequest"/> command.
/// </summary>
public readonly struct StoreNodeInfoResponse : ICommand<StoreNodeInfoResponse>
{
    public StoreNodeInfoResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.StoreNodeInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// Whether the store operation succeeded.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static StoreNodeInfoResponse Create(DataFrame frame, CommandParsingContext context) => new StoreNodeInfoResponse(frame);
}

/// <summary>
/// Callback for the <see cref="StoreNodeInfoRequest"/> command.
/// </summary>
public readonly struct StoreNodeInfoCallback : ICommand<StoreNodeInfoCallback>
{
    public StoreNodeInfoCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.StoreNodeInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    public static StoreNodeInfoCallback Create(DataFrame frame, CommandParsingContext context) => new StoreNodeInfoCallback(frame);
}
