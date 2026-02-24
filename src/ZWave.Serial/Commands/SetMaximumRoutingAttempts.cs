namespace ZWave.Serial.Commands;

/// <summary>
/// Set the maximum number of source routing attempts before the next mechanism kicks in.
/// </summary>
public readonly struct SetMaximumRoutingAttemptsRequest : ICommand<SetMaximumRoutingAttemptsRequest>
{
    public SetMaximumRoutingAttemptsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetMaximumRoutingAttempts;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the maximum number of routing attempts.
    /// </summary>
    /// <param name="maxRoutingAttempts">The maximum number of routing attempts.</param>
    public static SetMaximumRoutingAttemptsRequest Create(byte maxRoutingAttempts)
    {
        ReadOnlySpan<byte> commandParameters = [maxRoutingAttempts];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetMaximumRoutingAttemptsRequest(frame);
    }

    public static SetMaximumRoutingAttemptsRequest Create(DataFrame frame, CommandParsingContext context) => new SetMaximumRoutingAttemptsRequest(frame);
}
