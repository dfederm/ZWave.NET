namespace ZWave.Serial.Commands;

/// <summary>
/// Request a SUC/SIS controller to update the requesting nodes neighbors.
/// </summary>
public readonly struct RediscoveryNeededRequest : IRequestWithCallback<RediscoveryNeededRequest>
{
    public RediscoveryNeededRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RediscoveryNeeded;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static RediscoveryNeededRequest Create(
        ushort nodeId,
        NodeIdType nodeIdType,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize + 1];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        commandParameters[offset] = sessionId;
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RediscoveryNeededRequest(frame);
    }

    public static RediscoveryNeededRequest Create(DataFrame frame, CommandParsingContext context) => new RediscoveryNeededRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RediscoveryNeededRequest"/> command.
/// </summary>
public readonly struct RediscoveryNeededCallback : ICommand<RediscoveryNeededCallback>
{
    public RediscoveryNeededCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RediscoveryNeeded;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// Whether the rediscovery request succeeded.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[1] != 0;

    public static RediscoveryNeededCallback Create(DataFrame frame, CommandParsingContext context) => new RediscoveryNeededCallback(frame);
}
