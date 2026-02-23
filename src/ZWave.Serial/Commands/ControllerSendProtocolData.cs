namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit the contents of an encrypted data buffer to a single node (NLS only).
/// This command is only supported by controller Z-Wave library types.
/// </summary>
public readonly struct ControllerSendProtocolDataRequest : IRequestWithCallback<ControllerSendProtocolDataRequest>
{
    public ControllerSendProtocolDataRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ControllerSendProtocolData;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static ControllerSendProtocolDataRequest Create(
        ushort destinationNodeId,
        NodeIdType nodeIdType,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> payloadMetaData,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[3 + nodeIdSize + data.Length + payloadMetaData.Length];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, destinationNodeId);
        commandParameters[offset] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(offset + 1, data.Length));
        offset += 1 + data.Length;
        commandParameters[offset] = (byte)payloadMetaData.Length;
        payloadMetaData.CopyTo(commandParameters.Slice(offset + 1, payloadMetaData.Length));
        offset += 1 + payloadMetaData.Length;
        commandParameters[offset] = sessionId;

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ControllerSendProtocolDataRequest(frame);
    }

    public static ControllerSendProtocolDataRequest Create(DataFrame frame, CommandParsingContext context) => new ControllerSendProtocolDataRequest(frame);
}

/// <summary>
/// Callback for the <see cref="ControllerSendProtocolDataRequest"/> command.
/// </summary>
public readonly struct ControllerSendProtocolDataCallback : ICommand<ControllerSendProtocolDataCallback>
{
    public ControllerSendProtocolDataCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ControllerSendProtocolData;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// Provides details about the transmission that was carried out.
    /// </summary>
    public TransmissionStatusReport? TransmissionStatusReport
        => Frame.CommandParameters.Length > 2
            ? new TransmissionStatusReport(Frame.CommandParameters[2..])
            : null;

    public static ControllerSendProtocolDataCallback Create(DataFrame frame, CommandParsingContext context) => new ControllerSendProtocolDataCallback(frame);
}
