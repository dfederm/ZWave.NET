namespace ZWave.Serial.Commands;

public readonly struct SendDataRequest : IRequestWithCallback<SendDataRequest>
{
    public SendDataRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendData;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendDataRequest Create(
        ushort nodeId,
        NodeIdType nodeIdType,
        ReadOnlySpan<byte> data,
        TransmissionOptions transmissionOptions,
        byte sessionId)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[3 + nodeIdSize + data.Length];
        int offset = nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        commandParameters[offset] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(offset + 1, data.Length));
        offset += 1 + data.Length;
        commandParameters[offset] = (byte)transmissionOptions;
        commandParameters[offset + 1] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendDataRequest(frame);
    }

    public static SendDataRequest Create(DataFrame frame, CommandParsingContext context) => new SendDataRequest(frame);
}

public readonly struct SendDataCallback : ICommand<SendDataCallback>
{
    public SendDataCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendData;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public TransmissionStatusReport? TransmissionStatusReport
        => Frame.CommandParameters.Length > 2
            ? new TransmissionStatusReport(Frame.CommandParameters[2..])
            : null;

    public static SendDataCallback Create(DataFrame frame, CommandParsingContext context) => new SendDataCallback(frame);
}