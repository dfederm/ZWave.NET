namespace ZWave.Serial.Commands;

/// <summary>
/// Request new return route destinations from the SUC/SIS node.
/// </summary>
public readonly struct RequestNewRouteDestinationsRequest : IRequestWithCallback<RequestNewRouteDestinationsRequest>
{
    public RequestNewRouteDestinationsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNewRouteDestinations;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public static RequestNewRouteDestinationsRequest Create(byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RequestNewRouteDestinationsRequest(frame);
    }

    public static RequestNewRouteDestinationsRequest Create(DataFrame frame) => new RequestNewRouteDestinationsRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RequestNewRouteDestinationsRequest"/> command.
/// </summary>
public readonly struct RequestNewRouteDestinationsCallback : ICommand<RequestNewRouteDestinationsCallback>
{
    public RequestNewRouteDestinationsCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNewRouteDestinations;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the request.
    /// </summary>
    public byte Status => Frame.CommandParameters.Span[1];

    public static RequestNewRouteDestinationsCallback Create(DataFrame frame) => new RequestNewRouteDestinationsCallback(frame);
}
