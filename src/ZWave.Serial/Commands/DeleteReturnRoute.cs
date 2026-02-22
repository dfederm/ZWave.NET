namespace ZWave.Serial.Commands;

/// <summary>
/// Delete all static return routes from a Routing Slave or Enhanced 232 Slave node.
/// </summary>
public readonly struct DeleteReturnRouteRequest : IRequestWithCallback<DeleteReturnRouteRequest>
{
    public DeleteReturnRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.DeleteReturnRoute;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static DeleteReturnRouteRequest Create(
        ushort nodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new DeleteReturnRouteRequest(frame);
    }

    public static DeleteReturnRouteRequest Create(DataFrame frame) => new DeleteReturnRouteRequest(frame);
}

/// <summary>
/// Callback for the <see cref="DeleteReturnRouteRequest"/> command.
/// </summary>
public readonly struct DeleteReturnRouteCallback : ICommand<DeleteReturnRouteCallback>
{
    public DeleteReturnRouteCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.DeleteReturnRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static DeleteReturnRouteCallback Create(DataFrame frame) => new DeleteReturnRouteCallback(frame);
}
