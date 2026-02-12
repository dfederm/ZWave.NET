namespace ZWave.Serial.Commands;

/// <summary>
/// Send a test frame directly to nodeID without any routing.
/// </summary>
public readonly struct SendTestFrameRequest : IRequestWithCallback<SendTestFrameRequest>
{
    public SendTestFrameRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendTestFrame;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[2];

    public static SendTestFrameRequest Create(
        byte nodeId,
        byte powerLevel,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId, powerLevel, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendTestFrameRequest(frame);
    }

    public static SendTestFrameRequest Create(DataFrame frame) => new SendTestFrameRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendTestFrameRequest"/> command.
/// </summary>
public readonly struct SendTestFrameCallback : ICommand<SendTestFrameCallback>
{
    public SendTestFrameCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendTestFrame;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static SendTestFrameCallback Create(DataFrame frame) => new SendTestFrameCallback(frame);
}
