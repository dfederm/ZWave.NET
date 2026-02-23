namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit the data buffer to a list of Z-Wave Nodes (multicast frame).
/// </summary>
public readonly struct SendDataMultiRequest : IRequestWithCallback<SendDataMultiRequest>
{
    public SendDataMultiRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataMulti;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendDataMultiRequest Create(
        ReadOnlySpan<ushort> nodeIds,
        NodeIdType nodeIdType,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        byte nodeCount = (byte)nodeIds.Length;
        int nodeListBytes = nodeCount * nodeIdSize;
        int total = 1 + nodeListBytes + 1 + data.Length + 1 + 1;
        Span<byte> commandParameters = stackalloc byte[total];
        commandParameters[0] = nodeCount;
        int offset = 1;
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
        return new SendDataMultiRequest(frame);
    }

    public static SendDataMultiRequest Create(DataFrame frame, CommandParsingContext context) => new SendDataMultiRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendDataMultiRequest"/> command.
/// </summary>
public readonly struct SendDataMultiCallback : ICommand<SendDataMultiCallback>
{
    public SendDataMultiCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataMulti;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static SendDataMultiCallback Create(DataFrame frame, CommandParsingContext context) => new SendDataMultiCallback(frame);
}
