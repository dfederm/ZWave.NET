namespace ZWave.Serial.Commands;

/// <summary>
/// Create and transmit a Virtual Slave node "Node Information" frame.
/// </summary>
public readonly struct SendSlaveNodeInformationRequest : IRequestWithCallback<SendSlaveNodeInformationRequest>
{
    public SendSlaveNodeInformationRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendSlaveNodeInformation;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[3];

    public static SendSlaveNodeInformationRequest Create(
        byte destinationNodeId,
        byte slaveNodeId,
        TransmissionOptions txOptions,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            destinationNodeId,
            slaveNodeId,
            (byte)txOptions,
            sessionId,
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SendSlaveNodeInformationRequest(frame);
    }

    public static SendSlaveNodeInformationRequest Create(DataFrame frame) => new SendSlaveNodeInformationRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SendSlaveNodeInformationRequest"/> command.
/// </summary>
public readonly struct SendSlaveNodeInformationCallback : ICommand<SendSlaveNodeInformationCallback>
{
    public SendSlaveNodeInformationCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SendSlaveNodeInformation;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static SendSlaveNodeInformationCallback Create(DataFrame frame) => new SendSlaveNodeInformationCallback(frame);
}
