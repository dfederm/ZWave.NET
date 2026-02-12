namespace ZWave.Serial.Commands;

/// <summary>
/// Returns a random word using the built-in hardware random number generator.
/// </summary>
public readonly struct GetRandomWordRequest : ICommand<GetRandomWordRequest>
{
    public GetRandomWordRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetRandomWord;

    public DataFrame Frame { get; }

    public static GetRandomWordRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetRandomWordRequest(frame);
    }

    public static GetRandomWordRequest Create(DataFrame frame) => new GetRandomWordRequest(frame);
}

public readonly struct GetRandomWordResponse : ICommand<GetRandomWordResponse>
{
    public GetRandomWordResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetRandomWord;

    public DataFrame Frame { get; }

    /// <summary>
    /// The first random byte.
    /// </summary>
    public byte RandomByte1 => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The second random byte.
    /// </summary>
    public byte RandomByte2 => Frame.CommandParameters.Span[1];

    public static GetRandomWordResponse Create(DataFrame frame) => new GetRandomWordResponse(frame);
}
