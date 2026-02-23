namespace ZWave.Serial.Commands;

/// <summary>
/// Set the Priority Route for a destination node.
/// </summary>
public readonly struct SetPriorityRouteRequest : ICommand<SetPriorityRouteRequest>
{
    public SetPriorityRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetPriorityRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the priority route for a node.
    /// </summary>
    /// <param name="nodeId">The destination node ID.</param>
    /// <param name="repeaters">The 4-byte repeater list.</param>
    /// <param name="speed">The speed setting for the route.</param>
    public static SetPriorityRouteRequest Create(ushort nodeId, NodeIdType nodeIdType, ReadOnlySpan<byte> repeaters, PriorityRouteSpeed speed)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize + 5];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        repeaters.CopyTo(commandParameters[offset..]);
        commandParameters[offset + 4] = (byte)speed;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetPriorityRouteRequest(frame);
    }

    public static SetPriorityRouteRequest Create(DataFrame frame, CommandParsingContext context) => new SetPriorityRouteRequest(frame);
}

public readonly struct SetPriorityRouteResponse : ICommand<SetPriorityRouteResponse>
{
    public SetPriorityRouteResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetPriorityRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the priority route was successfully set.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static SetPriorityRouteResponse Create(DataFrame frame, CommandParsingContext context) => new SetPriorityRouteResponse(frame);
}
