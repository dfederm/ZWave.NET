namespace ZWave.Serial.Commands;

/// <summary>
/// Used by the Z-Wave API module to request a host application to encrypt a protocol frame payload.
/// This is an unsolicited request from the Z-Wave module to the host.
/// </summary>
public readonly struct RequestProtocolCcEncryption : ICommand<RequestProtocolCcEncryption>
{
    public RequestProtocolCcEncryption(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestProtocolCcEncryption;

    public DataFrame Frame { get; }

    /// <summary>
    /// The destination node ID.
    /// </summary>
    public ushort DestinationNodeId => Frame.CommandParameters.Span[0]; // TODO: This may be 16 bits if the node base type is set to 16 bit mode.

    private byte PayloadLength => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The payload to encrypt.
    /// </summary>
    public ReadOnlyMemory<byte> Payload => Frame.CommandParameters.Slice(2, PayloadLength);

    private byte PayloadMetaDataLength => Frame.CommandParameters.Span[2 + PayloadLength];

    /// <summary>
    /// The payload metadata.
    /// </summary>
    public ReadOnlyMemory<byte> PayloadMetaData => Frame.CommandParameters.Slice(3 + PayloadLength, PayloadMetaDataLength);

    /// <summary>
    /// Whether to use supervision for the encrypted frame.
    /// </summary>
    public bool UseSupervision => (Frame.CommandParameters.Span[3 + PayloadLength + PayloadMetaDataLength] & 0x80) != 0;

    /// <summary>
    /// The session ID for correlating with the encryption callback.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[4 + PayloadLength + PayloadMetaDataLength];

    public static RequestProtocolCcEncryption Create(DataFrame frame) => new RequestProtocolCcEncryption(frame);
}

/// <summary>
/// Callback received by host after encryption and transmission via <see cref="ControllerSendProtocolDataRequest"/>.
/// </summary>
public readonly struct RequestProtocolCcEncryptionCallback : ICommand<RequestProtocolCcEncryptionCallback>
{
    public RequestProtocolCcEncryptionCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RequestProtocolCcEncryption;

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

    public static RequestProtocolCcEncryptionCallback Create(DataFrame frame) => new RequestProtocolCcEncryptionCallback(frame);
}
