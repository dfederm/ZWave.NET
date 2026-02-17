namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit SUC/SIS node ID from a primary controller or static controller to the controller node ID specified.
/// </summary>
public readonly struct SendSucIdRequest : IRequestWithCallback<SendSucIdRequest>
{
    public SendSucIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendSucId;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[2];

    public static SendSucIdRequest Create(
        byte destinationNodeId,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [destinationNodeId, (byte)txOptions, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendSucIdRequest(frame);
    }

    public static SendSucIdRequest Create(DataFrame frame) => new SendSucIdRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendSucIdRequest"/> command.
/// </summary>
public readonly struct SendSucIdCallback : ICommand<SendSucIdCallback>
{
    public SendSucIdCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendSucId;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static SendSucIdCallback Create(DataFrame frame) => new SendSucIdCallback(frame);
}
