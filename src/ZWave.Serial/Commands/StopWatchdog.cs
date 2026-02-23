namespace ZWave.Serial.Commands;

/// <summary>
/// Stop Watchdog functionality on Z-Wave module.
/// </summary>
public readonly struct StopWatchdogRequest : ICommand<StopWatchdogRequest>
{
    public StopWatchdogRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.StopWatchdog;

    public DataFrame Frame { get; }

    public static StopWatchdogRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId);
        return new StopWatchdogRequest(frame);
    }

    public static StopWatchdogRequest Create(DataFrame frame, CommandParsingContext context) => new StopWatchdogRequest(frame);
}
