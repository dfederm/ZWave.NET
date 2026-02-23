namespace ZWave.Serial.Commands;

public readonly struct RequestNodeInfoRequest : ICommand<RequestNodeInfoRequest>
{
    public RequestNodeInfoRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestNodeInfo;

    public DataFrame Frame { get; }

    public static RequestNodeInfoRequest Create(ushort nodeId, NodeIdType nodeIdType)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize];
        nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RequestNodeInfoRequest(frame);
    }

    public static RequestNodeInfoRequest Create(DataFrame frame, CommandParsingContext context) => new RequestNodeInfoRequest(frame);
}
