namespace ZWave.Serial.Commands;

/// <summary>
/// Delete the return routes of the SUC/SIS node from a Routing Slave node or Enhanced 232 Slave node.
/// </summary>
public readonly struct DeleteSucReturnRouteRequest : IRequestWithCallback<DeleteSucReturnRouteRequest>
{
    public DeleteSucReturnRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.DeleteSucReturnRoute;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static DeleteSucReturnRouteRequest Create(
        ushort nodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new DeleteSucReturnRouteRequest(frame);
    }

    public static DeleteSucReturnRouteRequest Create(DataFrame frame) => new DeleteSucReturnRouteRequest(frame);
}

/// <summary>
/// Callback for the <see cref="DeleteSucReturnRouteRequest"/> command.
/// </summary>
public readonly struct DeleteSucReturnRouteCallback : ICommand<DeleteSucReturnRouteCallback>
{
    public DeleteSucReturnRouteCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.DeleteSucReturnRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static DeleteSucReturnRouteCallback Create(DataFrame frame) => new DeleteSucReturnRouteCallback(frame);
}
