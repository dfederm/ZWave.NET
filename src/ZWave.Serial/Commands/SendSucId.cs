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

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendSucIdRequest Create(
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
        return new SendSucIdRequest(frame);
    }

    public static SendSucIdRequest Create(DataFrame frame, CommandParsingContext context) => new SendSucIdRequest(frame);
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

    public static SendSucIdCallback Create(DataFrame frame, CommandParsingContext context) => new SendSucIdCallback(frame);
}
