namespace ZWave.Serial.Commands;

/// <summary>
/// Notify presence of a SUC/SIS to a Routing Slave or Enhanced 232 Slave.
/// </summary>
public readonly struct AssignSucReturnRouteRequest : IRequestWithCallback<AssignSucReturnRouteRequest>
{
    public AssignSucReturnRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignSucReturnRoute;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static AssignSucReturnRouteRequest Create(
        byte nodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AssignSucReturnRouteRequest(frame);
    }

    public static AssignSucReturnRouteRequest Create(DataFrame frame) => new AssignSucReturnRouteRequest(frame);
}

/// <summary>
/// Callback for the <see cref="AssignSucReturnRouteRequest"/> command.
/// </summary>
public readonly struct AssignSucReturnRouteCallback : ICommand<AssignSucReturnRouteCallback>
{
    public AssignSucReturnRouteCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AssignSucReturnRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static AssignSucReturnRouteCallback Create(DataFrame frame) => new AssignSucReturnRouteCallback(frame);
}
