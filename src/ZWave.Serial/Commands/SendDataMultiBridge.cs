namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit the data buffer to a list of Z-Wave Nodes (multicast frame) via Bridge Controller.
/// </summary>
public readonly struct SendDataMultiBridgeRequest : IRequestWithCallback<SendDataMultiBridgeRequest>
{
    public SendDataMultiBridgeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataMultiBridge;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendDataMultiBridgeRequest Create(
        ushort sourceNodeId,
        ReadOnlySpan<ushort> nodeIds,
        NodeIdType nodeIdType,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        byte nodeCount = (byte)nodeIds.Length;
        int nodeListBytes = nodeCount * nodeIdSize;
        Span<byte> commandParameters = stackalloc byte[nodeIdSize + 1 + nodeListBytes + 1 + data.Length + 1 + 1];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, sourceNodeId);
        commandParameters[offset] = nodeCount;
        offset++;
        for (int i = 0; i < nodeCount; i++)
        {
            offset = nodeIdType.WriteNodeId(commandParameters, offset, nodeIds[i]);
        }

        commandParameters[offset] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(offset + 1, data.Length));
        offset += 1 + data.Length;
        commandParameters[offset] = (byte)txOptions;
        commandParameters[offset + 1] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendDataMultiBridgeRequest(frame);
    }

    public static SendDataMultiBridgeRequest Create(DataFrame frame, CommandParsingContext context) => new SendDataMultiBridgeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendDataMultiBridgeRequest"/> command.
/// </summary>
public readonly struct SendDataMultiBridgeCallback : ICommand<SendDataMultiBridgeCallback>
{
    public SendDataMultiBridgeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataMultiBridge;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static SendDataMultiBridgeCallback Create(DataFrame frame, CommandParsingContext context) => new SendDataMultiBridgeCallback(frame);
}
