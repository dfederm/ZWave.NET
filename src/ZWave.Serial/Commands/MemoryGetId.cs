namespace ZWave.Serial.Commands;

public readonly struct MemoryGetIdRequest : ICommand<MemoryGetIdRequest>
{
    public MemoryGetIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryGetId;

    public DataFrame Frame { get; }

    public static MemoryGetIdRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new MemoryGetIdRequest(frame);
    }

    public static MemoryGetIdRequest Create(DataFrame frame, CommandParsingContext context) => new MemoryGetIdRequest(frame);
}

public readonly struct MemoryGetIdResponse : ICommand<MemoryGetIdResponse>
{
    private readonly NodeIdType _nodeIdType;

    public MemoryGetIdResponse(DataFrame frame, NodeIdType nodeIdType)
    {
        Frame = frame;
        _nodeIdType = nodeIdType;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryGetId;

    public DataFrame Frame { get; }

    public uint HomeId => Frame.CommandParameters.Span[0..4].ToUInt32BE();

    public ushort NodeId => _nodeIdType.ReadNodeId(Frame.CommandParameters.Span, 4);

    public static MemoryGetIdResponse Create(DataFrame frame, CommandParsingContext context) => new MemoryGetIdResponse(frame, context.NodeIdType);
}
