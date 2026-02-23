namespace ZWave.Serial.Commands;

/// <summary>
/// Security transmission options.
/// </summary>
[Flags]
public enum TxSecurityOptions : byte
{
    None = 0x00,

    /// <summary>
    /// Request the destination to verify delivery.
    /// </summary>
    VerifyDelivery = 0x01,

    /// <summary>
    /// Indicates this is a singlecast followup to a multicast frame.
    /// </summary>
    SinglecastFollowup = 0x02,

    /// <summary>
    /// Indicates this is the first singlecast followup to a multicast frame.
    /// </summary>
    FirstSinglecastFollowup = 0x04,
}

/// <summary>
/// Transmit contents of a data buffer to a single node or all nodes (broadcast).
/// This command is only supported by End Node library types.
/// </summary>
public readonly struct EndNodeSendDataRequest : IRequestWithCallback<EndNodeSendDataRequest>
{
    public EndNodeSendDataRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.EndNodeSendData;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static EndNodeSendDataRequest Create(
        ushort destinationNodeId,
        NodeIdType nodeIdType,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        TxSecurityOptions txSecurityOptions,
        SecurityKey securityKey,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[6 + nodeIdSize + data.Length];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, destinationNodeId);
        commandParameters[offset] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(offset + 1, data.Length));
        offset += 1 + data.Length;
        commandParameters[offset] = (byte)txOptions;
        commandParameters[offset + 1] = (byte)txSecurityOptions;
        commandParameters[offset + 2] = (byte)securityKey;
        commandParameters[offset + 3] = 0x00; // TxOptions2 (reserved)
        commandParameters[offset + 4] = sessionId;

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new EndNodeSendDataRequest(frame);
    }

    public static EndNodeSendDataRequest Create(DataFrame frame, CommandParsingContext context) => new EndNodeSendDataRequest(frame);
}

/// <summary>
/// Callback for the <see cref="EndNodeSendDataRequest"/> command.
/// </summary>
public readonly struct EndNodeSendDataCallback : ICommand<EndNodeSendDataCallback>
{
    public EndNodeSendDataCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.EndNodeSendData;

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

    public static EndNodeSendDataCallback Create(DataFrame frame, CommandParsingContext context) => new EndNodeSendDataCallback(frame);
}
