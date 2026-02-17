namespace ZWave.Serial.Commands;

/// <summary>
/// Transmit the data buffer to a single Z-Wave Node or all Z-Wave Nodes (broadcast) via Bridge Controller.
/// </summary>
public readonly struct SendDataBridgeRequest : IRequestWithCallback<SendDataBridgeRequest>
{
    public SendDataBridgeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataBridge;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    public static SendDataBridgeRequest Create(
        byte sourceNodeId,
        byte destinationNodeId,
        ReadOnlySpan<byte> data,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[5 + data.Length];
        commandParameters[0] = sourceNodeId;
        commandParameters[1] = destinationNodeId;
        commandParameters[2] = (byte)data.Length;
        data.CopyTo(commandParameters.Slice(3, data.Length));
        commandParameters[3 + data.Length] = (byte)txOptions;
        commandParameters[4 + data.Length] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendDataBridgeRequest(frame);
    }

    public static SendDataBridgeRequest Create(DataFrame frame) => new SendDataBridgeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendDataBridgeRequest"/> command.
/// </summary>
public readonly struct SendDataBridgeCallback : ICommand<SendDataBridgeCallback>
{
    public SendDataBridgeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendDataBridge;

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

    public static SendDataBridgeCallback Create(DataFrame frame) => new SendDataBridgeCallback(frame);
}
