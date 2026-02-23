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

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendNodeInformationRequest Create(
        ushort destinationNodeId,
        NodeIdType nodeIdType,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize + 2];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, destinationNodeId);
        commandParameters[offset] = (byte)txOptions;
        commandParameters[offset + 1] = sessionId;
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendNodeInformationRequest(frame);
    }

    public static SendNodeInformationRequest Create(DataFrame frame, CommandParsingContext context) => new SendNodeInformationRequest(frame);
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

    public static SendNodeInformationCallback Create(DataFrame frame, CommandParsingContext context) => new SendNodeInformationCallback(frame);
}
