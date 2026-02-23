namespace ZWave.Serial.Commands;

/// <summary>
/// Returns random bytes using the built-in hardware random number generator.
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

    /// <summary>
    /// Create a request for random bytes.
    /// </summary>
    /// <param name="count">The number of random bytes to generate (1-32). If 0 or omitted, 2 bytes are returned.</param>
    public static GetRandomWordRequest Create(byte count = 0)
    {
        ReadOnlySpan<byte> commandParameters = count > 0 ? [count] : [];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
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
    /// Indicates whether the random number generation was successful.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    /// <summary>
    /// The number of random bytes generated.
    /// </summary>
    public byte Count => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The random bytes.
    /// </summary>
    public ReadOnlyMemory<byte> RandomBytes => Frame.CommandParameters.Slice(2, Count);

    public static GetRandomWordResponse Create(DataFrame frame) => new GetRandomWordResponse(frame);
}
