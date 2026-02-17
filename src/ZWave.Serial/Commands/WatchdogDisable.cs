namespace ZWave.Serial.Commands;

/// <summary>
/// Disable the 500 Series Z-Wave SoC built-in watchdog.
/// </summary>
public readonly struct WatchdogDisableRequest : ICommand<WatchdogDisableRequest>
{
    public WatchdogDisableRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.WatchdogDisable;

    public DataFrame Frame { get; }

    public static WatchdogDisableRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new WatchdogDisableRequest(frame);
    }

    public static WatchdogDisableRequest Create(DataFrame frame) => new WatchdogDisableRequest(frame);
}
