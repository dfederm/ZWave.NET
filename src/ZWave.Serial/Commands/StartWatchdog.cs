namespace ZWave.Serial.Commands;

/// <summary>
/// Start Watchdog functionality on Z-Wave module.
/// </summary>
public readonly struct StartWatchdogRequest : ICommand<StartWatchdogRequest>
{
    public StartWatchdogRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.StartWatchdog;

    public DataFrame Frame { get; }

    public static StartWatchdogRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId);
        return new StartWatchdogRequest(frame);
    }

    public static StartWatchdogRequest Create(DataFrame frame, CommandParsingContext context) => new StartWatchdogRequest(frame);
}
