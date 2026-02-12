namespace ZWave.Serial.Commands;

/// <summary>
/// Sets the "Listen Before Talk" threshold that controls at what RSSI level a Z-Wave node
/// will refuse to transmit because of noise.
/// </summary>
public readonly struct SetListenBeforeTalkThresholdRequest : ICommand<SetListenBeforeTalkThresholdRequest>
{
    public SetListenBeforeTalkThresholdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetListenBeforeTalkThreshold;

    public DataFrame Frame { get; }

    /// <param name="channel">The channel number.</param>
    /// <param name="threshold">The RSSI threshold level in dBm.</param>
    public static SetListenBeforeTalkThresholdRequest Create(byte channel, sbyte threshold)
    {
        ReadOnlySpan<byte> commandParameters = [channel, unchecked((byte)threshold)];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetListenBeforeTalkThresholdRequest(frame);
    }

    public static SetListenBeforeTalkThresholdRequest Create(DataFrame frame) => new SetListenBeforeTalkThresholdRequest(frame);
}

public readonly struct SetListenBeforeTalkThresholdResponse : ICommand<SetListenBeforeTalkThresholdResponse>
{
    public SetListenBeforeTalkThresholdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetListenBeforeTalkThreshold;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the threshold was successfully set.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static SetListenBeforeTalkThresholdResponse Create(DataFrame frame) => new SetListenBeforeTalkThresholdResponse(frame);
}
