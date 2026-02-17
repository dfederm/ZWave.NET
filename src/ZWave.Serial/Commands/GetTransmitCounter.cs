namespace ZWave.Serial.Commands;

/// <summary>
/// Returns the number of transmits that the protocol has done since last reset of the variable.
/// </summary>
public readonly struct GetTransmitCounterRequest : ICommand<GetTransmitCounterRequest>
{
    public GetTransmitCounterRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetTransmitCounter;

    public DataFrame Frame { get; }

    public static GetTransmitCounterRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetTransmitCounterRequest(frame);
    }

    public static GetTransmitCounterRequest Create(DataFrame frame) => new GetTransmitCounterRequest(frame);
}

public readonly struct GetTransmitCounterResponse : ICommand<GetTransmitCounterResponse>
{
    public GetTransmitCounterResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetTransmitCounter;

    public DataFrame Frame { get; }

    /// <summary>
    /// The transmit counter value.
    /// </summary>
    public byte Counter => Frame.CommandParameters.Span[0];

    public static GetTransmitCounterResponse Create(DataFrame frame) => new GetTransmitCounterResponse(frame);
}
