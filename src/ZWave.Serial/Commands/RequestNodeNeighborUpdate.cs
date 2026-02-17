namespace ZWave.Serial.Commands;

/// <summary>
/// The status of the neighbor update request.
/// </summary>
public enum RequestNodeNeighborUpdateStatus : byte
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
/// Get the neighbors from the specified node.
/// </summary>
public readonly struct RequestNodeNeighborUpdateRequest : IRequestWithCallback<RequestNodeNeighborUpdateRequest>
{
    public RequestNodeNeighborUpdateRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNodeNeighborUpdate;

    public static bool ExpectsResponseStatus => false;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static RequestNodeNeighborUpdateRequest Create(
        byte nodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RequestNodeNeighborUpdateRequest(frame);
    }

    public static RequestNodeNeighborUpdateRequest Create(DataFrame frame) => new RequestNodeNeighborUpdateRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RequestNodeNeighborUpdateRequest"/> command.
/// </summary>
public readonly struct RequestNodeNeighborUpdateCallback : ICommand<RequestNodeNeighborUpdateCallback>
{
    public RequestNodeNeighborUpdateCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNodeNeighborUpdate;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the neighbor update request.
    /// </summary>
    public RequestNodeNeighborUpdateStatus Status => (RequestNodeNeighborUpdateStatus)Frame.CommandParameters.Span[1];

    public static RequestNodeNeighborUpdateCallback Create(DataFrame frame) => new RequestNodeNeighborUpdateCallback(frame);
}
