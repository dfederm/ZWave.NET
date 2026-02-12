namespace ZWave.Serial.Commands;

public readonly struct ResponseStatusResponse : ICommand<ResponseStatusResponse>
{
    public ResponseStatusResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => throw new InvalidOperationException(); // This response type is used by various commands, so don't claim any.

    public DataFrame Frame { get; }

    public bool WasRequestAccepted => Frame.CommandParameters.Span[0] != 0;

    public static ResponseStatusResponse Create(DataFrame frame) => new ResponseStatusResponse(frame);
}
