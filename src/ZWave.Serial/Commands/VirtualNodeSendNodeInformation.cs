namespace ZWave.Serial.Commands;

/// <summary>
/// Create and transmit a Virtual Node "Node Information" frame.
/// </summary>
public readonly struct VirtualNodeSendNodeInformationRequest : IRequestWithCallback<VirtualNodeSendNodeInformationRequest>
{
    public VirtualNodeSendNodeInformationRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.VirtualNodeSendNodeInformation;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[3];

    public static VirtualNodeSendNodeInformationRequest Create(
        ushort destinationNodeId,
        ushort virtualNodeId,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            (byte)destinationNodeId,
            (byte)virtualNodeId,
            (byte)txOptions,
            sessionId,
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new VirtualNodeSendNodeInformationRequest(frame);
    }

    public static VirtualNodeSendNodeInformationRequest Create(DataFrame frame, CommandParsingContext context) => new VirtualNodeSendNodeInformationRequest(frame);
}

/// <summary>
/// Callback for the <see cref="VirtualNodeSendNodeInformationRequest"/> command.
/// </summary>
public readonly struct VirtualNodeSendNodeInformationCallback : ICommand<VirtualNodeSendNodeInformationCallback>
{
    public VirtualNodeSendNodeInformationCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.VirtualNodeSendNodeInformation;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static VirtualNodeSendNodeInformationCallback Create(DataFrame frame, CommandParsingContext context) => new VirtualNodeSendNodeInformationCallback(frame);
}
