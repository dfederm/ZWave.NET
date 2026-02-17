namespace ZWave.Serial.Commands;

/// <summary>
/// Request the status of the protocol.
/// </summary>
public readonly struct GetProtocolStatusRequest : ICommand<GetProtocolStatusRequest>
{
    public GetProtocolStatusRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetProtocolStatus;

    public DataFrame Frame { get; }

    public static GetProtocolStatusRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetProtocolStatusRequest(frame);
    }

    public static GetProtocolStatusRequest Create(DataFrame frame) => new GetProtocolStatusRequest(frame);
}

public readonly struct GetProtocolStatusResponse : ICommand<GetProtocolStatusResponse>
{
    public GetProtocolStatusResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetProtocolStatus;

    public DataFrame Frame { get; }

    /// <summary>
    /// The current protocol status.
    /// </summary>
    public byte Status => Frame.CommandParameters.Span[0];

    public static GetProtocolStatusResponse Create(DataFrame frame) => new GetProtocolStatusResponse(frame);
}
