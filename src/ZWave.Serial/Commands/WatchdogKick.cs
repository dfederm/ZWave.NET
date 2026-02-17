namespace ZWave.Serial.Commands;

/// <summary>
/// Keep the watchdog timer from resetting the 500 Series Z-Wave SoC.
/// </summary>
public readonly struct WatchdogKickRequest : ICommand<WatchdogKickRequest>
{
    public WatchdogKickRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.WatchdogKick;

    public DataFrame Frame { get; }

    public static WatchdogKickRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new WatchdogKickRequest(frame);
    }

    public static WatchdogKickRequest Create(DataFrame frame) => new WatchdogKickRequest(frame);
}
