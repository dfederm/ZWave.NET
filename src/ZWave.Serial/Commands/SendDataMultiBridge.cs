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
        byte sourceNodeId,
        ReadOnlySpan<byte> nodeList,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        byte nodeCount = (byte)nodeList.Length;
        Span<byte> commandParameters = stackalloc byte[3 + nodeCount + 1 + data.Length + 1 + 1];
        commandParameters[0] = sourceNodeId;
        commandParameters[1] = nodeCount;
        nodeList.CopyTo(commandParameters.Slice(2, nodeCount));
        commandParameters[2 + nodeCount] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(3 + nodeCount, data.Length));
        commandParameters[3 + nodeCount + data.Length] = (byte)txOptions;
        commandParameters[4 + nodeCount + data.Length] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendDataMultiBridgeRequest(frame);
    }

    public static SendDataMultiBridgeRequest Create(DataFrame frame) => new SendDataMultiBridgeRequest(frame);
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

    public static SendDataMultiBridgeCallback Create(DataFrame frame) => new SendDataMultiBridgeCallback(frame);
}
