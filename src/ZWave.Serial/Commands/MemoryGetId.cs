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

    public static MemoryGetIdRequest Create(DataFrame frame) => new MemoryGetIdRequest(frame);
}

public readonly struct MemoryGetIdResponse : ICommand<MemoryGetIdResponse>
{
    public MemoryGetIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryGetId;

    public DataFrame Frame { get; }

    public uint HomeId => Frame.CommandParameters.Span[0..4].ToUInt32BE();

    public ushort NodeId => Frame.CommandParameters.Span[4];

    public static MemoryGetIdResponse Create(DataFrame frame) => new MemoryGetIdResponse(frame);
}
