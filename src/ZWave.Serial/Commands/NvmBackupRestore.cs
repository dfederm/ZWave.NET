namespace ZWave.Serial.Commands;

/// <summary>
/// Read and write the firmware data of the Z-Wave API Module using 16-bit addresses.
/// </summary>
public readonly partial struct NvmBackupRestoreRequest : ICommand<NvmBackupRestoreRequest>
{
    public NvmBackupRestoreRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NvmBackupRestore;

    public DataFrame Frame { get; }

    private static NvmBackupRestoreRequest Create(NvmOperationSubCommand subCommand, ReadOnlySpan<byte> subCommandParameters)
    {
        Span<byte> commandParameters = stackalloc byte[subCommandParameters.Length + 1];
        commandParameters[0] = (byte)subCommand;
        subCommandParameters.CopyTo(commandParameters[1..]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NvmBackupRestoreRequest(frame);
    }

    /// <summary>
    /// Create a request to open the NVM for backup or restore.
    /// </summary>
    public static NvmBackupRestoreRequest Open()
        => Create(NvmOperationSubCommand.Open, []);

    /// <summary>
    /// Create a request to read firmware data from the NVM.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <param name="offset">The 16-bit address offset.</param>
    public static NvmBackupRestoreRequest Read(byte length, ushort offset)
    {
        Span<byte> subCommandParameters = stackalloc byte[3];
        subCommandParameters[0] = length;
        offset.WriteBytesBE(subCommandParameters[1..]);
        return Create(NvmOperationSubCommand.Read, subCommandParameters);
    }

    /// <summary>
    /// Create a request to write firmware data to the NVM.
    /// </summary>
    /// <param name="offset">The 16-bit address offset.</param>
    /// <param name="data">The data to write.</param>
    public static NvmBackupRestoreRequest Write(ushort offset, ReadOnlySpan<byte> data)
    {
        Span<byte> subCommandParameters = stackalloc byte[3 + data.Length];
        subCommandParameters[0] = (byte)data.Length;
        offset.WriteBytesBE(subCommandParameters[1..3]);
        data.CopyTo(subCommandParameters[3..]);
        return Create(NvmOperationSubCommand.Write, subCommandParameters);
    }

    /// <summary>
    /// Create a request to close the NVM backup or restore operation.
    /// </summary>
    public static NvmBackupRestoreRequest Close()
        => Create(NvmOperationSubCommand.Close, []);

    public static NvmBackupRestoreRequest Create(DataFrame frame) => new NvmBackupRestoreRequest(frame);
}

/// <summary>
/// Response to a <see cref="NvmBackupRestoreRequest"/> command.
/// </summary>
public readonly struct NvmBackupRestoreResponse : ICommand<NvmBackupRestoreResponse>
{
    public NvmBackupRestoreResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NvmBackupRestore;

    public DataFrame Frame { get; }

    /// <summary>
    /// The status of the NVM operation.
    /// </summary>
    public NvmOperationStatus Status => (NvmOperationStatus)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The address offset or NVM size (16-bit big-endian).
    /// For Open responses, this is the total NVM size.
    /// For Read/Write responses, this is the address offset.
    /// </summary>
    public ushort AddressOffsetOrNvmSize => Frame.CommandParameters.Span[2..4].ToUInt16BE();

    /// <summary>
    /// The firmware data returned by the operation.
    /// </summary>
    public ReadOnlyMemory<byte> FirmwareData
    {
        get
        {
            byte length = Frame.CommandParameters.Span[1];
            return length > 0
                ? Frame.CommandParameters.Slice(4, length)
                : ReadOnlyMemory<byte>.Empty;
        }
    }

    public static NvmBackupRestoreResponse Create(DataFrame frame) => new NvmBackupRestoreResponse(frame);
}
