namespace ZWave.Serial.Commands;

/// <summary>
/// Set the WUT timer interval.
/// </summary>
public readonly struct SetWutTimeoutRequest : ICommand<SetWutTimeoutRequest>
{
    public SetWutTimeoutRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetWutTimeout;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the WUT timeout.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    public static SetWutTimeoutRequest Create(byte timeout)
    {
        ReadOnlySpan<byte> commandParameters = [timeout];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetWutTimeoutRequest(frame);
    }

    public static SetWutTimeoutRequest Create(DataFrame frame) => new SetWutTimeoutRequest(frame);
}
