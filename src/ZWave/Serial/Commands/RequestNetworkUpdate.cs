namespace ZWave.Serial.Commands;

/// <summary>
/// The status of the network update request.
/// </summary>
public enum RequestNetworkUpdateStatus : byte
{
    /// <summary>
    /// The network update process has completed successfully.
    /// </summary>
    Done = 0x00,

    /// <summary>
    /// The network update process was aborted.
    /// </summary>
    Abort = 0x01,

    /// <summary>
    /// The network update process is waiting.
    /// </summary>
    Wait = 0x02,

    /// <summary>
    /// The network update process is disabled.
    /// </summary>
    Disabled = 0x03,
}

/// <summary>
/// Request a network update from a SUC/SIS controller.
/// </summary>
public readonly struct RequestNetworkUpdateRequest : IRequestWithCallback<RequestNetworkUpdateRequest>
{
    public RequestNetworkUpdateRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNetworkUpdate;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public static RequestNetworkUpdateRequest Create(byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RequestNetworkUpdateRequest(frame);
    }

    public static RequestNetworkUpdateRequest Create(DataFrame frame) => new RequestNetworkUpdateRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RequestNetworkUpdateRequest"/> command.
/// </summary>
public readonly struct RequestNetworkUpdateCallback : ICommand<RequestNetworkUpdateCallback>
{
    public RequestNetworkUpdateCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNetworkUpdate;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the network update request.
    /// </summary>
    public RequestNetworkUpdateStatus Status => (RequestNetworkUpdateStatus)Frame.CommandParameters.Span[1];

    public static RequestNetworkUpdateCallback Create(DataFrame frame) => new RequestNetworkUpdateCallback(frame);
}
