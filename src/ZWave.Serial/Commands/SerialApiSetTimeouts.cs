namespace ZWave.Serial.Commands;

/// <summary>
/// Set the timeout in the Serial API.
/// </summary>
public readonly struct SerialApiSetTimeoutsRequest : ICommand<SerialApiSetTimeoutsRequest>
{
    public SerialApiSetTimeoutsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiSetTimeouts;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the Serial API timeouts.
    /// </summary>
    /// <param name="rxAckTimeout">The ACK timeout in multiples of 10ms.</param>
    /// <param name="rxByteTimeout">The byte timeout in multiples of 10ms.</param>
    public static SerialApiSetTimeoutsRequest Create(byte rxAckTimeout, byte rxByteTimeout)
    {
        ReadOnlySpan<byte> commandParameters = [rxAckTimeout, rxByteTimeout];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SerialApiSetTimeoutsRequest(frame);
    }

    public static SerialApiSetTimeoutsRequest Create(DataFrame frame) => new SerialApiSetTimeoutsRequest(frame);
}

public readonly struct SerialApiSetTimeoutsResponse : ICommand<SerialApiSetTimeoutsResponse>
{
    public SerialApiSetTimeoutsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiSetTimeouts;

    public DataFrame Frame { get; }

    /// <summary>
    /// The previous ACK timeout in multiples of 10ms.
    /// </summary>
    public byte PreviousRxAckTimeout => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The previous byte timeout in multiples of 10ms.
    /// </summary>
    public byte PreviousRxByteTimeout => Frame.CommandParameters.Span[1];

    public static SerialApiSetTimeoutsResponse Create(DataFrame frame) => new SerialApiSetTimeoutsResponse(frame);
}
