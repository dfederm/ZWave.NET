namespace ZWave.Serial.Commands;

/// <summary>
/// Set the active Long Range radio channel.
/// </summary>
public readonly struct SetLongRangeChannelRequest : ICommand<SetLongRangeChannelRequest>
{
    public SetLongRangeChannelRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetLongRangeChannel;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the active Long Range radio channel.
    /// </summary>
    /// <param name="channel">The Long Range channel to set.</param>
    public static SetLongRangeChannelRequest Create(LongRangeChannel channel)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)channel];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetLongRangeChannelRequest(frame);
    }

    public static SetLongRangeChannelRequest Create(DataFrame frame, CommandParsingContext context) => new SetLongRangeChannelRequest(frame);
}

/// <summary>
/// Response to a SetLongRangeChannel request.
/// </summary>
public readonly struct SetLongRangeChannelResponse : ICommand<SetLongRangeChannelResponse>
{
    public SetLongRangeChannelResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetLongRangeChannel;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static SetLongRangeChannelResponse Create(DataFrame frame, CommandParsingContext context) => new SetLongRangeChannelResponse(frame);
}
