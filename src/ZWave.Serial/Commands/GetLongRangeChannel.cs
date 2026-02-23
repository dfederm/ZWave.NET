namespace ZWave.Serial.Commands;

/// <summary>
/// Request the active Long Range radio channel.
/// </summary>
public readonly struct GetLongRangeChannelRequest : ICommand<GetLongRangeChannelRequest>
{
    public GetLongRangeChannelRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetLongRangeChannel;

    public DataFrame Frame { get; }

    public static GetLongRangeChannelRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId, []);
        return new GetLongRangeChannelRequest(frame);
    }

    public static GetLongRangeChannelRequest Create(DataFrame frame, CommandParsingContext context) => new GetLongRangeChannelRequest(frame);
}

/// <summary>
/// Response to a GetLongRangeChannel request.
/// </summary>
public readonly struct GetLongRangeChannelResponse : ICommand<GetLongRangeChannelResponse>
{
    public GetLongRangeChannelResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetLongRangeChannel;

    public DataFrame Frame { get; }

    /// <summary>
    /// Gets the active Long Range channel.
    /// </summary>
    public LongRangeChannel Channel => (LongRangeChannel)Frame.CommandParameters.Span[0];

    /// <summary>
    /// Gets a value indicating whether the controller supports automatic channel selection.
    /// </summary>
    public bool SupportsAutoChannelSelection
        => Frame.CommandParameters.Length >= 2 && (Frame.CommandParameters.Span[1] & 0b0001_0000) != 0;

    /// <summary>
    /// Gets a value indicating whether automatic channel selection is currently active.
    /// </summary>
    public bool AutoChannelSelectionActive
        => Frame.CommandParameters.Length >= 2 && (Frame.CommandParameters.Span[1] & 0b0010_0000) != 0;

    public static GetLongRangeChannelResponse Create(DataFrame frame, CommandParsingContext context) => new GetLongRangeChannelResponse(frame);
}
