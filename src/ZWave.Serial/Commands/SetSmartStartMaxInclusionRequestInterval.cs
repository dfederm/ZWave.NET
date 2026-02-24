namespace ZWave.Serial.Commands;

/// <summary>
/// Set the maximum interval between SmartStart inclusion requests.
/// </summary>
public readonly struct SetSmartStartMaxInclusionRequestIntervalRequest : ICommand<SetSmartStartMaxInclusionRequestIntervalRequest>
{
    public SetSmartStartMaxInclusionRequestIntervalRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSmartStartMaxInclusionRequestInterval;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the maximum inclusion request intervals.
    /// </summary>
    /// <param name="intervals">The maximum interval value.</param>
    public static SetSmartStartMaxInclusionRequestIntervalRequest Create(byte intervals)
    {
        ReadOnlySpan<byte> commandParameters = [intervals];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetSmartStartMaxInclusionRequestIntervalRequest(frame);
    }

    public static SetSmartStartMaxInclusionRequestIntervalRequest Create(DataFrame frame, CommandParsingContext context) => new SetSmartStartMaxInclusionRequestIntervalRequest(frame);
}

public readonly struct SetSmartStartMaxInclusionRequestIntervalResponse : ICommand<SetSmartStartMaxInclusionRequestIntervalResponse>
{
    public SetSmartStartMaxInclusionRequestIntervalResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetSmartStartMaxInclusionRequestInterval;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the command was accepted.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static SetSmartStartMaxInclusionRequestIntervalResponse Create(DataFrame frame, CommandParsingContext context) => new SetSmartStartMaxInclusionRequestIntervalResponse(frame);
}
