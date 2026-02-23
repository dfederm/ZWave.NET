namespace ZWave.Serial.Commands;

/// <summary>
/// Send a NOP to a node. Used to check if a node is reachable.
/// </summary>
public readonly struct SendNopRequest : IRequestWithCallback<SendNopRequest>
{
    public SendNopRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendNop;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendNopRequest Create(
        ushort nodeId,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            (byte)nodeId, // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
            (byte)txOptions,
            sessionId,
        ];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendNopRequest(frame);
    }

    public static SendNopRequest Create(DataFrame frame) => new SendNopRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendNopRequest"/> command.
/// </summary>
public readonly struct SendNopCallback : ICommand<SendNopCallback>
{
    public SendNopCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendNop;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The optional transmission status report.
    /// </summary>
    public TransmissionStatusReport? TransmissionStatusReport
        => Frame.CommandParameters.Length > 2
            ? new TransmissionStatusReport(Frame.CommandParameters[2..])
            : null;

    public static SendNopCallback Create(DataFrame frame) => new SendNopCallback(frame);
}
