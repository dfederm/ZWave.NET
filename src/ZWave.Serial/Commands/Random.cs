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

    public static RandomRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new RandomRequest(frame);
    }

    public static RandomRequest Create(DataFrame frame, CommandParsingContext context) => new RandomRequest(frame);
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
    /// The random number.
    /// </summary>
    public byte RandomNumber => Frame.CommandParameters.Span[0];

    public static RandomResponse Create(DataFrame frame, CommandParsingContext context) => new RandomResponse(frame);
}
