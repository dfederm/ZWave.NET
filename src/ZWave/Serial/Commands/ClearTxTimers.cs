namespace ZWave.Serial.Commands;

/// <summary>
/// Clears the protocol's internal tx timers.
/// </summary>
public readonly struct ClearTxTimersRequest : ICommand<ClearTxTimersRequest>
{
    public ClearTxTimersRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ClearTxTimers;

    public DataFrame Frame { get; }

    public static ClearTxTimersRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new ClearTxTimersRequest(frame);
    }

    public static ClearTxTimersRequest Create(DataFrame frame) => new ClearTxTimersRequest(frame);
}
