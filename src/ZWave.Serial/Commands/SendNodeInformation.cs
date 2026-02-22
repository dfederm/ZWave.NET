namespace ZWave.Serial.Commands;

/// <summary>
/// Create and transmit a "Node Information" frame.
/// </summary>
public readonly struct SendNodeInformationRequest : IRequestWithCallback<SendNodeInformationRequest>
{
    public SendNodeInformationRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendNodeInformation;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[2];

    public static SendNodeInformationRequest Create(
        ushort destinationNodeId,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)destinationNodeId, (byte)txOptions, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendNodeInformationRequest(frame);
    }

    public static SendNodeInformationRequest Create(DataFrame frame) => new SendNodeInformationRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendNodeInformationRequest"/> command.
/// </summary>
public readonly struct SendNodeInformationCallback : ICommand<SendNodeInformationCallback>
{
    public SendNodeInformationCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendNodeInformation;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static SendNodeInformationCallback Create(DataFrame frame) => new SendNodeInformationCallback(frame);
}
