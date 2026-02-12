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
        ReadOnlySpan<byte> nodeList,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        byte nodeCount = (byte)nodeList.Length;
        int total = 1 + nodeCount + 1 + data.Length + 1 + 1;
        Span<byte> commandParameters = stackalloc byte[total];
        commandParameters[0] = nodeCount;
        nodeList.CopyTo(commandParameters.Slice(1, nodeCount));
        commandParameters[1 + nodeCount] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(2 + nodeCount, data.Length));
        commandParameters[2 + nodeCount + data.Length] = (byte)txOptions;
        commandParameters[3 + nodeCount + data.Length] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendDataMultiRequest(frame);
    }

    public static SendDataMultiRequest Create(DataFrame frame) => new SendDataMultiRequest(frame);
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

    public static SendDataMultiCallback Create(DataFrame frame) => new SendDataMultiCallback(frame);
}
