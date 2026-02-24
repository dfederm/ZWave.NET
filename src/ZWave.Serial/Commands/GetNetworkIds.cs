namespace ZWave.Serial.Commands;

public readonly struct GetNetworkIdsRequest : ICommand<GetNetworkIdsRequest>
{
    public GetNetworkIdsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetNetworkIds;

    public DataFrame Frame { get; }

    public static GetNetworkIdsRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetNetworkIdsRequest(frame);
    }

    public static GetNetworkIdsRequest Create(DataFrame frame, CommandParsingContext context) => new GetNetworkIdsRequest(frame);
}

public readonly struct GetNetworkIdsResponse : ICommand<GetNetworkIdsResponse>
{
    private readonly NodeIdType _nodeIdType;

    public GetNetworkIdsResponse(DataFrame frame, NodeIdType nodeIdType)
    {
        Frame = frame;
        _nodeIdType = nodeIdType;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetNetworkIds;

    public DataFrame Frame { get; }

    public uint HomeId => Frame.CommandParameters.Span[0..4].ToUInt32BE();

    public ushort NodeId => _nodeIdType.ReadNodeId(Frame.CommandParameters.Span, 4);

    public static GetNetworkIdsResponse Create(DataFrame frame, CommandParsingContext context) => new GetNetworkIdsResponse(frame, context.NodeIdType);
}
