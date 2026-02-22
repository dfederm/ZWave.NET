namespace ZWave.Serial.Commands;

/// <summary>
/// Assign static return routes (up to 4) to a Routing Slave or Enhanced 232 Slave node.
/// </summary>
public readonly struct AssignReturnRouteRequest : IRequestWithCallback<AssignReturnRouteRequest>
{
    public AssignReturnRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignReturnRoute;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[2];

    public static AssignReturnRouteRequest Create(
        ushort sourceNodeId,
        ushort destinationNodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)sourceNodeId, (byte)destinationNodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AssignReturnRouteRequest(frame);
    }

    public static AssignReturnRouteRequest Create(DataFrame frame) => new AssignReturnRouteRequest(frame);
}

/// <summary>
/// Callback for the <see cref="AssignReturnRouteRequest"/> command.
/// </summary>
public readonly struct AssignReturnRouteCallback : ICommand<AssignReturnRouteCallback>
{
    public AssignReturnRouteCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignReturnRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static AssignReturnRouteCallback Create(DataFrame frame) => new AssignReturnRouteCallback(frame);
}
