namespace ZWave.Serial.Commands;

/// <summary>
/// Gets the protocol's internal tx timer for the specified channel.
/// </summary>
public readonly struct GetTxTimerRequest : ICommand<GetTxTimerRequest>
{
    public GetTxTimerRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetTxTimer;

    public DataFrame Frame { get; }

    /// <param name="channel">The channel number (0-2).</param>
    public static GetTxTimerRequest Create(byte channel)
    {
        ReadOnlySpan<byte> commandParameters = [channel];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetTxTimerRequest(frame);
    }

    public static GetTxTimerRequest Create(DataFrame frame) => new GetTxTimerRequest(frame);
}

public readonly struct GetTxTimerResponse : ICommand<GetTxTimerResponse>
{
    public GetTxTimerResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetTxTimer;

    public DataFrame Frame { get; }

    /// <summary>
    /// The timer value in ticks.
    /// </summary>
    public uint TimerTicks => Frame.CommandParameters.Span[0..4].ToUInt32BE();

    public static GetTxTimerResponse Create(DataFrame frame) => new GetTxTimerResponse(frame);
}
