namespace ZWave.Serial.Commands;

/// <summary>
/// Enable the 500 Series Z-Wave SoC built-in watchdog.
/// </summary>
public readonly struct WatchdogEnableRequest : ICommand<WatchdogEnableRequest>
{
    public WatchdogEnableRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.WatchdogEnable;

    public DataFrame Frame { get; }

    public static WatchdogEnableRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new WatchdogEnableRequest(frame);
    }

    public static WatchdogEnableRequest Create(DataFrame frame) => new WatchdogEnableRequest(frame);
}
