namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit a data buffer to a list of Z-Wave nodes (S2 Multicast frame).
/// This command is only supported by End Node library types.
/// </summary>
public readonly struct EndNodeSendDataMulticastRequest : IRequestWithCallback<EndNodeSendDataMulticastRequest>
{
    public EndNodeSendDataMulticastRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.EndNodeSendDataMulticast;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static EndNodeSendDataMulticastRequest Create(
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        SecurityKey securityKey,
        byte multicastGroupId,
        byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[5 + data.Length];
        commandParameters[0] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(1, data.Length));
        commandParameters[1 + data.Length] = (byte)txOptions;
        commandParameters[2 + data.Length] = (byte)securityKey;
        commandParameters[3 + data.Length] = multicastGroupId;
        commandParameters[4 + data.Length] = sessionId;

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new EndNodeSendDataMulticastRequest(frame);
    }

    public static EndNodeSendDataMulticastRequest Create(DataFrame frame) => new EndNodeSendDataMulticastRequest(frame);
}

/// <summary>
/// Callback for the <see cref="EndNodeSendDataMulticastRequest"/> command.
/// </summary>
public readonly struct EndNodeSendDataMulticastCallback : ICommand<EndNodeSendDataMulticastCallback>
{
    public EndNodeSendDataMulticastCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.EndNodeSendDataMulticast;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus TransmissionStatus => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static EndNodeSendDataMulticastCallback Create(DataFrame frame) => new EndNodeSendDataMulticastCallback(frame);
}
