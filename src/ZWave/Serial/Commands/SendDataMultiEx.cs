namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit the data buffer using S2 multicast to a list of Z-Wave Nodes.
/// </summary>
public readonly struct SendDataMultiExRequest : IRequestWithCallback<SendDataMultiExRequest>
{
    public SendDataMultiExRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataMultiEx;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendDataMultiExRequest Create(
        ReadOnlySpan<byte> nodeList,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId,
        byte groupId)
    {
        byte nodeCount = (byte)nodeList.Length;
        int total = 1 + nodeCount + 1 + data.Length + 1 + 1 + 1;
        Span<byte> commandParameters = stackalloc byte[total];
        commandParameters[0] = nodeCount;
        nodeList.CopyTo(commandParameters.Slice(1, nodeCount));
        commandParameters[1 + nodeCount] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(2 + nodeCount, data.Length));
        commandParameters[2 + nodeCount + data.Length] = (byte)txOptions;
        commandParameters[3 + nodeCount + data.Length] = groupId;
        commandParameters[4 + nodeCount + data.Length] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendDataMultiExRequest(frame);
    }

    public static SendDataMultiExRequest Create(DataFrame frame) => new SendDataMultiExRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendDataMultiExRequest"/> command.
/// </summary>
public readonly struct SendDataMultiExCallback : ICommand<SendDataMultiExCallback>
{
    public SendDataMultiExCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataMultiEx;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static SendDataMultiExCallback Create(DataFrame frame) => new SendDataMultiExCallback(frame);
}
