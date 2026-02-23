namespace ZWave.Serial.Commands;

public readonly struct GetSucNodeIdRequest : ICommand<GetSucNodeIdRequest>
{
    public GetSucNodeIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetSucNodeId;

    public DataFrame Frame { get; }

    public static GetSucNodeIdRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetSucNodeIdRequest(frame);
    }

    public static GetSucNodeIdRequest Create(DataFrame frame, CommandParsingContext context) => new GetSucNodeIdRequest(frame);
}

public readonly struct GetSucNodeIdResponse : ICommand<GetSucNodeIdResponse>
{
    private readonly NodeIdType _nodeIdType;

    public GetSucNodeIdResponse(DataFrame frame, NodeIdType nodeIdType)
    {
        Frame = frame;
        _nodeIdType = nodeIdType;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetSucNodeId;

    public DataFrame Frame { get; }

    public ushort SucNodeId => _nodeIdType.ReadNodeId(Frame.CommandParameters.Span, 0);

    public static GetSucNodeIdResponse Create(DataFrame frame, CommandParsingContext context) => new GetSucNodeIdResponse(frame, context.NodeIdType);
}
