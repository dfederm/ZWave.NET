namespace ZWave.Serial.Commands;

/// <summary>
/// Transfer a protocol command class to the Z-Wave API module.
/// </summary>
public readonly struct TransferProtocolCcRequest : ICommand<TransferProtocolCcRequest>
{
    public TransferProtocolCcRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.TransferProtocolCc;

    public DataFrame Frame { get; }

    public static TransferProtocolCcRequest Create(
        ushort sourceNodeId,
        SecurityKey decryptionKey,
        ReadOnlySpan<byte> payload)
    {
        Span<byte> commandParameters = stackalloc byte[3 + payload.Length];
        commandParameters[0] = (byte)sourceNodeId; // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
        commandParameters[1] = (byte)decryptionKey;
        commandParameters[2] = (byte)payload.Length;
        payload.CopyTo(commandParameters.Slice(3, payload.Length));

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new TransferProtocolCcRequest(frame);
    }

    public static TransferProtocolCcRequest Create(DataFrame frame) => new TransferProtocolCcRequest(frame);
}

/// <summary>
/// Response to the <see cref="TransferProtocolCcRequest"/> command.
/// </summary>
public readonly struct TransferProtocolCcResponse : ICommand<TransferProtocolCcResponse>
{
    public TransferProtocolCcResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.TransferProtocolCc;

    public DataFrame Frame { get; }

    /// <summary>
    /// The command status (0x01 = success, 0x00 = failure).
    /// </summary>
    public byte CommandStatus => Frame.CommandParameters.Span[0];

    public static TransferProtocolCcResponse Create(DataFrame frame) => new TransferProtocolCcResponse(frame);
}
