namespace ZWave.Serial.Commands;

/// <summary>
/// Get the route with the highest priority.
/// </summary>
public readonly struct GetPriorityRouteRequest : ICommand<GetPriorityRouteRequest>
{
    public GetPriorityRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetPriorityRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to get the priority route for a node.
    /// </summary>
    /// <param name="nodeId">The node ID to get the priority route for.</param>
    public static GetPriorityRouteRequest Create(ushort nodeId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetPriorityRouteRequest(frame);
    }

    public static GetPriorityRouteRequest Create(DataFrame frame) => new GetPriorityRouteRequest(frame);
}

public readonly struct GetPriorityRouteResponse : ICommand<GetPriorityRouteResponse>
{
    public GetPriorityRouteResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetPriorityRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether a priority route exists for the node.
    /// </summary>
    public bool RouteExists => Frame.CommandParameters.Span[0] != 0;

    /// <summary>
    /// The repeater nodes in the priority route (4 bytes).
    /// </summary>
    public ReadOnlyMemory<byte> Repeaters => Frame.CommandParameters.Slice(1, 4);

    /// <summary>
    /// The speed setting for the priority route.
    /// </summary>
    public byte Speed => Frame.CommandParameters.Span[5];

    public static GetPriorityRouteResponse Create(DataFrame frame) => new GetPriorityRouteResponse(frame);
}
