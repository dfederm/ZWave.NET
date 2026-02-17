namespace ZWave.Serial.Commands;

/// <summary>
/// Assign an application defined Priority Return Route to a routing or an enhanced slave.
/// </summary>
public readonly struct AssignPriorityReturnRouteRequest : IRequestWithCallback<AssignPriorityReturnRouteRequest>
{
    public AssignPriorityReturnRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignPriorityReturnRoute;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[7];

    public static AssignPriorityReturnRouteRequest Create(
        byte sourceNodeId,
        byte destinationNodeId,
        ReadOnlySpan<byte> route,
        byte routeSpeed,
        byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[8];
        commandParameters[0] = sourceNodeId;
        commandParameters[1] = destinationNodeId;
        route.CopyTo(commandParameters.Slice(2, 4));
        commandParameters[6] = routeSpeed;
        commandParameters[7] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AssignPriorityReturnRouteRequest(frame);
    }

    public static AssignPriorityReturnRouteRequest Create(DataFrame frame) => new AssignPriorityReturnRouteRequest(frame);
}

/// <summary>
/// Callback for the <see cref="AssignPriorityReturnRouteRequest"/> command.
/// </summary>
public readonly struct AssignPriorityReturnRouteCallback : ICommand<AssignPriorityReturnRouteCallback>
{
    public AssignPriorityReturnRouteCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignPriorityReturnRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static AssignPriorityReturnRouteCallback Create(DataFrame frame) => new AssignPriorityReturnRouteCallback(frame);
}
