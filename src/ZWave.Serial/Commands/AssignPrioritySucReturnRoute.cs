namespace ZWave.Serial.Commands;

/// <summary>
/// Assign an application defined Priority SUC Return Route to a routing or an enhanced slave.
/// </summary>
public readonly struct AssignPrioritySucReturnRouteRequest : IRequestWithCallback<AssignPrioritySucReturnRouteRequest>
{
    public AssignPrioritySucReturnRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignPrioritySucReturnRoute;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static AssignPrioritySucReturnRouteRequest Create(
        ushort nodeId,
        NodeIdType nodeIdType,
        ReadOnlySpan<byte> route,
        byte routeSpeed,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[6 + nodeIdSize];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        route.CopyTo(commandParameters.Slice(offset, 4));
        commandParameters[offset + 4] = routeSpeed;
        commandParameters[offset + 5] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AssignPrioritySucReturnRouteRequest(frame);
    }

    public static AssignPrioritySucReturnRouteRequest Create(DataFrame frame, CommandParsingContext context) => new AssignPrioritySucReturnRouteRequest(frame);
}

/// <summary>
/// Callback for the <see cref="AssignPrioritySucReturnRouteRequest"/> command.
/// </summary>
public readonly struct AssignPrioritySucReturnRouteCallback : ICommand<AssignPrioritySucReturnRouteCallback>
{
    public AssignPrioritySucReturnRouteCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignPrioritySucReturnRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static AssignPrioritySucReturnRouteCallback Create(DataFrame frame, CommandParsingContext context) => new AssignPrioritySucReturnRouteCallback(frame);
}
