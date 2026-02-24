namespace ZWave.Serial.Commands;

/// <summary>
/// Locks or unlocks all last working routes.
/// </summary>
/// <remarks>
/// Per Z-Wave Host API Specification §4.4.3.17, this command has no NodeID field.
/// It globally locks or unlocks whether last working routes are saved by the Z-Wave API module.
/// </remarks>
public readonly struct LockUnlockLastRouteRequest : ICommand<LockUnlockLastRouteRequest>
{
    public LockUnlockLastRouteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.LockUnlockLastRoute;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to lock or unlock all last working routes.
    /// </summary>
    /// <param name="locked">True to lock (save) last working routes, false to unlock.</param>
    public static LockUnlockLastRouteRequest Create(bool locked)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)(locked ? 1 : 0)];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new LockUnlockLastRouteRequest(frame);
    }

    public static LockUnlockLastRouteRequest Create(DataFrame frame, CommandParsingContext context) => new LockUnlockLastRouteRequest(frame);
}
