namespace ZWave.Serial.Commands;

/// <summary>
/// Instruct the Z-Wave API to go to sleep in order to remove the power.
/// </summary>
public readonly struct InitiateShutdownRequest : ICommand<InitiateShutdownRequest>
{
    public InitiateShutdownRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.InitiateShutdown;

    public DataFrame Frame { get; }

    public static InitiateShutdownRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId);
        return new InitiateShutdownRequest(frame);
    }

    public static InitiateShutdownRequest Create(DataFrame frame, CommandParsingContext context) => new InitiateShutdownRequest(frame);
}

public readonly struct InitiateShutdownResponse : ICommand<InitiateShutdownResponse>
{
    public InitiateShutdownResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.InitiateShutdown;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the shutdown request was accepted.
    /// </summary>
    /// <remarks>
    /// 0x00 = not accepted, 0x01 = accepted.
    /// </remarks>
    public bool WasAccepted => Frame.CommandParameters.Span[0] != 0;

    public static InitiateShutdownResponse Create(DataFrame frame, CommandParsingContext context) => new InitiateShutdownResponse(frame);
}
