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

    public static GetSucNodeIdRequest Create(DataFrame frame) => new GetSucNodeIdRequest(frame);
}

public readonly struct GetSucNodeIdResponse : ICommand<GetSucNodeIdResponse>
{
    public GetSucNodeIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetSucNodeId;

    public DataFrame Frame { get; }

    // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
    public ushort SucNodeId => Frame.CommandParameters.Span[0];

    public static GetSucNodeIdResponse Create(DataFrame frame) => new GetSucNodeIdResponse(frame);
}
