namespace ZWave.Serial.Commands;

/// <summary>
/// Locks or unlocks response route for a given node ID.
/// </summary>
public readonly struct LockRouteRequest : ICommand<LockRouteRequest>
{
    public LockRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.LockRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to lock or unlock a route.
    /// </summary>
    /// <param name="nodeId">The node ID to lock or unlock.</param>
    /// <param name="locked">True to lock the route, false to unlock.</param>
    public static LockRouteRequest Create(byte nodeId, bool locked)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId, (byte)(locked ? 1 : 0)];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new LockRouteRequest(frame);
    }

    public static LockRouteRequest Create(DataFrame frame) => new LockRouteRequest(frame);
}
