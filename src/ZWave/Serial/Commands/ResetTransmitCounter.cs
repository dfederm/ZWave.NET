namespace ZWave.Serial.Commands;

/// <summary>
/// Reset the number of transmits that the protocol has done since last reset of the variable.
/// </summary>
public readonly struct ResetTransmitCounterRequest : ICommand<ResetTransmitCounterRequest>
{
    public ResetTransmitCounterRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ResetTransmitCounter;

    public DataFrame Frame { get; }

    public static ResetTransmitCounterRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new ResetTransmitCounterRequest(frame);
    }

    public static ResetTransmitCounterRequest Create(DataFrame frame) => new ResetTransmitCounterRequest(frame);
}
