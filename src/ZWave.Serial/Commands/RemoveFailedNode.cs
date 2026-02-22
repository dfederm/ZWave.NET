namespace ZWave.Serial.Commands;

/// <summary>
/// The status of the remove failed node operation.
/// </summary>
public enum RemoveFailedNodeStatus : byte
{
    /// <summary>
    /// The node is responding and cannot be removed.
    /// </summary>
    NodeOk = 0x00,

    /// <summary>
    /// The node has been removed.
    /// </summary>
    NodeRemoved = 0x01,

    /// <summary>
    /// The node could not be removed.
    /// </summary>
    NodeNotRemoved = 0x02,
}

/// <summary>
/// Remove a non-responding node from the routing table in the requesting controller.
/// </summary>
public readonly struct RemoveFailedNodeRequest : IRequestWithCallback<RemoveFailedNodeRequest>
{
    public RemoveFailedNodeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RemoveFailedNode;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static RemoveFailedNodeRequest Create(
        ushort nodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RemoveFailedNodeRequest(frame);
    }

    public static RemoveFailedNodeRequest Create(DataFrame frame) => new RemoveFailedNodeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RemoveFailedNodeRequest"/> command.
/// </summary>
public readonly struct RemoveFailedNodeCallback : ICommand<RemoveFailedNodeCallback>
{
    public RemoveFailedNodeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RemoveFailedNode;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the remove failed node operation.
    /// </summary>
    public RemoveFailedNodeStatus Status => (RemoveFailedNodeStatus)Frame.CommandParameters.Span[1];

    public static RemoveFailedNodeCallback Create(DataFrame frame) => new RemoveFailedNodeCallback(frame);
}
