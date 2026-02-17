namespace ZWave.Serial.Commands;

/// <summary>
/// Abort the ongoing transmit started with SendData or SendDataMulti.
/// </summary>
public readonly struct SendDataAbortRequest : ICommand<SendDataAbortRequest>
{
    public SendDataAbortRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataAbort;

    public DataFrame Frame { get; }

    public static SendDataAbortRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new SendDataAbortRequest(frame);
    }

    public static SendDataAbortRequest Create(DataFrame frame) => new SendDataAbortRequest(frame);
}
