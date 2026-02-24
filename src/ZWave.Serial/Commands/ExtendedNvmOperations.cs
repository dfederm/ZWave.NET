namespace ZWave.Serial.Commands;

/// <summary>
/// Status of an extended NVM operation.
/// </summary>
public enum ExtendedNvmOperationStatus : byte
{
    OK = 0x00,
    Error = 0x01,
    OperationMismatch = 0x02,
    OperationInterference = 0x03,
    SubCommandNotSupported = 0x04,
    EndOfFile = 0xFF,
}

/// <summary>
/// Read and write the NVM data of the Z-Wave API Module using 32-bit addresses.
/// </summary>
public readonly partial struct ExtendedNvmOperationsRequest : ICommand<ExtendedNvmOperationsRequest>
{
    public ExtendedNvmOperationsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ExtendedNvmOperations;

    public DataFrame Frame { get; }

    private static ExtendedNvmOperationsRequest Create(NvmOperationSubCommand subCommand, ReadOnlySpan<byte> subCommandParameters)
    {
        Span<byte> commandParameters = stackalloc byte[subCommandParameters.Length + 1];
        commandParameters[0] = (byte)subCommand;
        subCommandParameters.CopyTo(commandParameters[1..]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ExtendedNvmOperationsRequest(frame);
    }

    /// <summary>
    /// Create a request to open the NVM for read or write.
    /// </summary>
    public static ExtendedNvmOperationsRequest Open()
        => Create(NvmOperationSubCommand.Open, []);

    /// <summary>
    /// Create a request to read NVM data from the NVM.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <param name="offset">The 32-bit address offset.</param>
    public static ExtendedNvmOperationsRequest Read(byte length, uint offset)
    {
        Span<byte> subCommandParameters = stackalloc byte[5];
        subCommandParameters[0] = length;
        offset.WriteBytesBE(subCommandParameters[1..]);
        return Create(NvmOperationSubCommand.Read, subCommandParameters);
    }

    /// <summary>
    /// Create a request to write NVM data to the NVM.
    /// </summary>
    /// <param name="offset">The 32-bit address offset.</param>
    /// <param name="data">The data to write.</param>
    public static ExtendedNvmOperationsRequest Write(uint offset, ReadOnlySpan<byte> data)
    {
        Span<byte> subCommandParameters = stackalloc byte[5 + data.Length];
        subCommandParameters[0] = (byte)data.Length;
        offset.WriteBytesBE(subCommandParameters[1..5]);
        data.CopyTo(subCommandParameters[5..]);
        return Create(NvmOperationSubCommand.Write, subCommandParameters);
    }

    /// <summary>
    /// Create a request to close the NVM read or write operation.
    /// </summary>
    public static ExtendedNvmOperationsRequest Close()
        => Create(NvmOperationSubCommand.Close, []);

    public static ExtendedNvmOperationsRequest Create(DataFrame frame, CommandParsingContext context) => new ExtendedNvmOperationsRequest(frame);
}

/// <summary>
/// Response to an <see cref="ExtendedNvmOperationsRequest"/> command.
/// </summary>
public readonly struct ExtendedNvmOperationsResponse : ICommand<ExtendedNvmOperationsResponse>
{
    public ExtendedNvmOperationsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.ExtendedNvmOperations;

    public DataFrame Frame { get; }

    /// <summary>
    /// The status of the NVM operation.
    /// </summary>
    public ExtendedNvmOperationStatus Status => (ExtendedNvmOperationStatus)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The address offset or NVM size (32-bit big-endian).
    /// For Open responses, this is the total NVM size.
    /// For Read/Write responses, this is the address offset.
    /// </summary>
    public uint AddressOffsetOrNvmSize => Frame.CommandParameters.Span[2..6].ToUInt32BE();

    /// <summary>
    /// The NVM data returned by the operation.
    /// For Open responses, this contains the supported sub-command bitmask.
    /// </summary>
    public ReadOnlyMemory<byte> FirmwareData
    {
        get
        {
            byte length = Frame.CommandParameters.Span[1];
            return length > 0
                ? Frame.CommandParameters.Slice(6, length)
                : ReadOnlyMemory<byte>.Empty;
        }
    }

    public static ExtendedNvmOperationsResponse Create(DataFrame frame, CommandParsingContext context) => new ExtendedNvmOperationsResponse(frame);
}
