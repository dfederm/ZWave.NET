namespace ZWave.Serial.Commands;

/// <summary>
/// Returns a pseudo-random number.
/// </summary>
public readonly struct RandomRequest : ICommand<RandomRequest>
{
    public RandomRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.Random;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request for random bytes.
    /// </summary>
    /// <param name="count">The number of random bytes to generate (1-32).</param>
    public static RandomRequest Create(byte count)
    {
        ReadOnlySpan<byte> commandParameters = [count];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RandomRequest(frame);
    }

    public static RandomRequest Create(DataFrame frame) => new RandomRequest(frame);
}

public readonly struct RandomResponse : ICommand<RandomResponse>
{
    public RandomResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.Random;

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

    public static RandomResponse Create(DataFrame frame) => new RandomResponse(frame);
}
