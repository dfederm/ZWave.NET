namespace ZWave.Serial.Commands;

/// <summary>
/// The status of the neighbor update request.
/// </summary>
public enum RequestNodeNeighborDiscoveryStatus : byte
{
    /// <summary>
    /// The neighbor update process has started.
    /// </summary>
    Started = 0x21,

    /// <summary>
    /// The neighbor update process has completed successfully.
    /// </summary>
    Done = 0x22,

    /// <summary>
    /// The neighbor update process has failed.
    /// </summary>
    Failed = 0x23,
}

/// <summary>
/// Request node neighbor discovery from the specified node.
/// </summary>
public readonly struct RequestNodeNeighborDiscoveryRequest : IRequestWithCallback<RequestNodeNeighborDiscoveryRequest>
{
    public RequestNodeNeighborDiscoveryRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNodeNeighborDiscovery;

    public static bool ExpectsResponseStatus => false;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static RequestNodeNeighborDiscoveryRequest Create(
        ushort nodeId,
        NodeIdType nodeIdType,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize + 1];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        commandParameters[offset] = sessionId;
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RequestNodeNeighborDiscoveryRequest(frame);
    }

    public static RequestNodeNeighborDiscoveryRequest Create(DataFrame frame, CommandParsingContext context) => new RequestNodeNeighborDiscoveryRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RequestNodeNeighborDiscoveryRequest"/> command.
/// </summary>
public readonly struct RequestNodeNeighborDiscoveryCallback : ICommand<RequestNodeNeighborDiscoveryCallback>
{
    public RequestNodeNeighborDiscoveryCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNodeNeighborDiscovery;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the neighbor update request.
    /// </summary>
    public RequestNodeNeighborDiscoveryStatus Status => (RequestNodeNeighborDiscoveryStatus)Frame.CommandParameters.Span[1];

    public static RequestNodeNeighborDiscoveryCallback Create(DataFrame frame, CommandParsingContext context) => new RequestNodeNeighborDiscoveryCallback(frame);
}
