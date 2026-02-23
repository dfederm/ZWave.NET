namespace ZWave.Serial.Commands;

/// <summary>
/// The node type for a neighbor update request.
/// </summary>
public enum NeighborUpdateNodeType : byte
{
    NonListening = 0x00,
    Listening = 0x01,
    FLiRS = 0x02,
}

/// <summary>
/// The status of the neighbor discovery process.
/// </summary>
public enum NeighborDiscoveryStatus : byte
{
    /// <summary>
    /// The neighbor discovery process has started.
    /// </summary>
    Started = 0x21,

    /// <summary>
    /// The neighbor discovery process has completed successfully.
    /// </summary>
    Completed = 0x22,

    /// <summary>
    /// The neighbor discovery process has failed.
    /// </summary>
    Failed = 0x23,

    /// <summary>
    /// The neighbor discovery is not supported.
    /// </summary>
    NotSupported = 0xFF,
}

/// <summary>
/// Request a node to perform a new neighbor update for a specific node type.
/// </summary>
public readonly struct RequestNodeTypeNeighborUpdateRequest : ICommand<RequestNodeTypeNeighborUpdateRequest>
{
    public RequestNodeTypeNeighborUpdateRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNodeTypeNeighborUpdate;

    public DataFrame Frame { get; }

    public static RequestNodeTypeNeighborUpdateRequest Create(
        ushort nodeId,
        NeighborUpdateNodeType nodeType,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            (byte)nodeId, // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
            (byte)nodeType,
            sessionId,
        ];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RequestNodeTypeNeighborUpdateRequest(frame);
    }

    public static RequestNodeTypeNeighborUpdateRequest Create(DataFrame frame) => new RequestNodeTypeNeighborUpdateRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RequestNodeTypeNeighborUpdateRequest"/> command.
/// </summary>
public readonly struct RequestNodeTypeNeighborUpdateCallback : ICommand<RequestNodeTypeNeighborUpdateCallback>
{
    public RequestNodeTypeNeighborUpdateCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNodeTypeNeighborUpdate;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the neighbor discovery process.
    /// </summary>
    public NeighborDiscoveryStatus Status => (NeighborDiscoveryStatus)Frame.CommandParameters.Span[1];

    public static RequestNodeTypeNeighborUpdateCallback Create(DataFrame frame) => new RequestNodeTypeNeighborUpdateCallback(frame);
}
