namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit the data buffer to a single Z-Wave Node or all Z-Wave Nodes (broadcast) via Bridge Controller.
/// </summary>
public readonly struct SendDataBridgeRequest : IRequestWithCallback<SendDataBridgeRequest>
{
    public SendDataBridgeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataBridge;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendDataBridgeRequest Create(
        ushort sourceNodeId,
        ushort destinationNodeId,
        NodeIdType nodeIdType,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[3 + 2 * nodeIdSize + data.Length];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, sourceNodeId);
        offset = nodeIdType.WriteNodeId(commandParameters, offset, destinationNodeId);
        commandParameters[offset] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(offset + 1, data.Length));
        offset += 1 + data.Length;
        commandParameters[offset] = (byte)txOptions;
        commandParameters[offset + 1] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendDataBridgeRequest(frame);
    }

    public static SendDataBridgeRequest Create(DataFrame frame, CommandParsingContext context) => new SendDataBridgeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendDataBridgeRequest"/> command.
/// </summary>
public readonly struct SendDataBridgeCallback : ICommand<SendDataBridgeCallback>
{
    public SendDataBridgeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataBridge;

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

    public static SendDataBridgeCallback Create(DataFrame frame, CommandParsingContext context) => new SendDataBridgeCallback(frame);
}
