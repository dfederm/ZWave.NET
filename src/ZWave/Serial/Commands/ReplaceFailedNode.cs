namespace ZWave.Serial.Commands;

/// <summary>
/// The status of the replace failed node operation.
/// </summary>
public enum ReplaceFailedNodeStatus : byte
{
    /// <summary>
    /// The node is responding and cannot be replaced.
    /// </summary>
    NodeOk = 0x00,

    /// <summary>
    /// The node has been replaced successfully.
    /// </summary>
    ReplaceDone = 0x03,

    /// <summary>
    /// The node replacement has failed.
    /// </summary>
    ReplaceFailed = 0x04,
}

/// <summary>
/// Replaces a non-responding node with a new one in the requesting controller.
/// </summary>
public readonly struct ReplaceFailedNodeRequest : IRequestWithCallback<ReplaceFailedNodeRequest>
{
    public ReplaceFailedNodeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ReplaceFailedNode;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static ReplaceFailedNodeRequest Create(
        byte nodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ReplaceFailedNodeRequest(frame);
    }

    public static ReplaceFailedNodeRequest Create(DataFrame frame) => new ReplaceFailedNodeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="ReplaceFailedNodeRequest"/> command.
/// </summary>
public readonly struct ReplaceFailedNodeCallback : ICommand<ReplaceFailedNodeCallback>
{
    public ReplaceFailedNodeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ReplaceFailedNode;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the replace failed node operation.
    /// </summary>
    public ReplaceFailedNodeStatus Status => (ReplaceFailedNodeStatus)Frame.CommandParameters.Span[1];

    public static ReplaceFailedNodeCallback Create(DataFrame frame) => new ReplaceFailedNodeCallback(frame);
}
