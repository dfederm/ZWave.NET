namespace ZWave.Serial.Commands;

/// <summary>
/// Request the Z-Wave API module to encrypt a Z-Wave frame payload using AES-128 ECB mode.
/// </summary>
public readonly struct EncryptDataWithAesRequest : ICommand<EncryptDataWithAesRequest>
{
    public EncryptDataWithAesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.EncryptDataWithAes;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to encrypt data using AES-128 ECB mode.
    /// </summary>
    /// <param name="key">The 16-byte AES key.</param>
    /// <param name="inputData">The 16-byte input data to encrypt.</param>
    public static EncryptDataWithAesRequest Create(ReadOnlySpan<byte> key, ReadOnlySpan<byte> inputData)
    {
        Span<byte> commandParameters = stackalloc byte[32];
        key.CopyTo(commandParameters[..16]);
        inputData.CopyTo(commandParameters[16..32]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new EncryptDataWithAesRequest(frame);
    }

    public static EncryptDataWithAesRequest Create(DataFrame frame) => new EncryptDataWithAesRequest(frame);
}

public readonly struct EncryptDataWithAesResponse : ICommand<EncryptDataWithAesResponse>
{
    public EncryptDataWithAesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.EncryptDataWithAes;

    public DataFrame Frame { get; }

    /// <summary>
    /// The 16-byte encrypted output data.
    /// </summary>
    public ReadOnlyMemory<byte> OutputData => Frame.CommandParameters[..16];

    public static EncryptDataWithAesResponse Create(DataFrame frame) => new EncryptDataWithAesResponse(frame);
}
