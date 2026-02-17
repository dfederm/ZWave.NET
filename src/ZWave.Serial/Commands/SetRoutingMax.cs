namespace ZWave.Serial.Commands;

/// <summary>
/// Set the maximum number of source routing attempts before the next mechanism kicks in.
/// </summary>
public readonly struct SetRoutingMaxRequest : ICommand<SetRoutingMaxRequest>
{
    public SetRoutingMaxRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetRoutingMax;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the maximum number of routing attempts.
    /// </summary>
    /// <param name="maxRoutingAttempts">The maximum number of routing attempts.</param>
    public static SetRoutingMaxRequest Create(byte maxRoutingAttempts)
    {
        ReadOnlySpan<byte> commandParameters = [maxRoutingAttempts];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetRoutingMaxRequest(frame);
    }

    public static SetRoutingMaxRequest Create(DataFrame frame) => new SetRoutingMaxRequest(frame);
}
