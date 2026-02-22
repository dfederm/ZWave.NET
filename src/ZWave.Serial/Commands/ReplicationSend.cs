namespace ZWave.Serial.Commands;

/// <summary>
/// Used when the controller is in replication mode. It sends the payload and expects the receiver to respond
/// with a command complete message.
/// </summary>
public readonly struct ReplicationSendRequest : IRequestWithCallback<ReplicationSendRequest>
{
    public ReplicationSendRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ReplicationSend;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static ReplicationSendRequest Create(
        ushort nodeId,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[4 + data.Length];
        commandParameters[0] = (byte)nodeId;
        commandParameters[1] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(2, data.Length));
        commandParameters[2 + data.Length] = (byte)txOptions;
        commandParameters[3 + data.Length] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ReplicationSendRequest(frame);
    }

    public static ReplicationSendRequest Create(DataFrame frame) => new ReplicationSendRequest(frame);
}

/// <summary>
/// Callback for the <see cref="ReplicationSendRequest"/> command.
/// </summary>
public readonly struct ReplicationSendCallback : ICommand<ReplicationSendCallback>
{
    public ReplicationSendCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ReplicationSend;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static ReplicationSendCallback Create(DataFrame frame) => new ReplicationSendCallback(frame);
}
