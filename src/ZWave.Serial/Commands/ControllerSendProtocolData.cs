namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit the contents of an encrypted data buffer to a single node (NLS only).
/// This command is only supported by controller Z-Wave library types.
/// </summary>
public readonly struct ControllerSendProtocolDataRequest : IRequestWithCallback<ControllerSendProtocolDataRequest>
{
    public ControllerSendProtocolDataRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ControllerSendProtocolData;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static ControllerSendProtocolDataRequest Create(
        ushort destinationNodeId,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> payloadMetaData,
        byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[4 + data.Length + payloadMetaData.Length];
        commandParameters[0] = (byte)destinationNodeId; // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
        commandParameters[1] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(2, data.Length));
        commandParameters[2 + data.Length] = (byte)payloadMetaData.Length;
        payloadMetaData.CopyTo(commandParameters.Slice(3 + data.Length, payloadMetaData.Length));
        commandParameters[3 + data.Length + payloadMetaData.Length] = sessionId;

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ControllerSendProtocolDataRequest(frame);
    }

    public static ControllerSendProtocolDataRequest Create(DataFrame frame) => new ControllerSendProtocolDataRequest(frame);
}

/// <summary>
/// Callback for the <see cref="ControllerSendProtocolDataRequest"/> command.
/// </summary>
public readonly struct ControllerSendProtocolDataCallback : ICommand<ControllerSendProtocolDataCallback>
{
    public ControllerSendProtocolDataCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ControllerSendProtocolData;

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
    /// Provides details about the transmission that was carried out.
    /// </summary>
    public TransmissionStatusReport? TransmissionStatusReport
        => Frame.CommandParameters.Length > 2
            ? new TransmissionStatusReport(Frame.CommandParameters[2..])
            : null;

    public static ControllerSendProtocolDataCallback Create(DataFrame frame) => new ControllerSendProtocolDataCallback(frame);
}
