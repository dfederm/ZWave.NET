namespace ZWave.Serial.Commands;

/// <summary>
/// Returns the most recent background RSSI levels detected.
/// </summary>
public readonly struct GetBackgroundRSSIRequest : ICommand<GetBackgroundRSSIRequest>
{
    public GetBackgroundRSSIRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetBackgroundRSSI;

    public DataFrame Frame { get; }

    public static GetBackgroundRSSIRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetBackgroundRSSIRequest(frame);
    }

    public static GetBackgroundRSSIRequest Create(DataFrame frame) => new GetBackgroundRSSIRequest(frame);
}

public readonly struct GetBackgroundRSSIResponse : ICommand<GetBackgroundRSSIResponse>
{
    public GetBackgroundRSSIResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetBackgroundRSSI;

    public DataFrame Frame { get; }

    /// <summary>
    /// The RSSI measurement for channel 0.
    /// </summary>
    public RssiMeasurement RssiChannel0 => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The RSSI measurement for channel 1.
    /// </summary>
    public RssiMeasurement RssiChannel1 => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The RSSI measurement for channel 2 (if present in the response).
    /// </summary>
    public RssiMeasurement? RssiChannel2
        => Frame.CommandParameters.Length > 2
            ? Frame.CommandParameters.Span[2]
            : null;

    public static GetBackgroundRSSIResponse Create(DataFrame frame) => new GetBackgroundRSSIResponse(frame);
}
