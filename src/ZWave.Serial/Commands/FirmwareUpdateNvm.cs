namespace ZWave.Serial.Commands;

/// <summary>
/// Firmware update NVM sub-commands.
/// </summary>
public enum FirmwareUpdateNvmSubCommand : byte
{
    Prepare = 0x00,
    WriteChunk = 0x01,
    PerformUpdate = 0x02,
}

/// <summary>
/// Status of a firmware update NVM operation.
/// </summary>
public enum FirmwareUpdateNvmStatus : byte
{
    OK = 0x00,
    ErrorTooBig = 0x01,
    ErrorFirmwareUpdateNotSupported = 0x02,
    ErrorBootloaderUpdateNotSupported = 0x03,
    ErrorWrongChecksum = 0x04,
    ErrorInvalidFileHeader = 0x05,
    ErrorInvalidSignature = 0x06,
    ErrorFirmwareDoesNotMatch = 0x07,
    ErrorHardwareVersionDoesNotMatch = 0x08,
    ErrorDowngradeNotSupported = 0x09,
    ErrorUnsupportedTarget = 0x0A,
    ErrorFailedToPrepare = 0x0B,
    ErrorSubCommandNotSupported = 0xFF,
}

/// <summary>
/// Firmware update target.
/// </summary>
public enum FirmwareUpdateTarget : byte
{
    Firmware = 0x01,
    Bootloader = 0x02,
}

/// <summary>
/// Perform Z-Wave API Module firmware and bootloader update.
/// </summary>
public readonly partial struct FirmwareUpdateNvmRequest : ICommand<FirmwareUpdateNvmRequest>
{
    public FirmwareUpdateNvmRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.FirmwareUpdateNvm;

    public DataFrame Frame { get; }

    private static FirmwareUpdateNvmRequest Create(FirmwareUpdateNvmSubCommand subCommand, ReadOnlySpan<byte> subCommandParameters)
    {
        Span<byte> commandParameters = stackalloc byte[subCommandParameters.Length + 1];
        commandParameters[0] = (byte)subCommand;
        subCommandParameters.CopyTo(commandParameters[1..]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new FirmwareUpdateNvmRequest(frame);
    }

    /// <summary>
    /// Create a request to prepare for a firmware update.
    /// </summary>
    public static FirmwareUpdateNvmRequest Prepare()
        => Create(FirmwareUpdateNvmSubCommand.Prepare, []);

    /// <summary>
    /// Create a request to write a firmware chunk.
    /// </summary>
    /// <param name="offset">The 32-bit offset for the chunk.</param>
    /// <param name="data">The firmware data chunk.</param>
    public static FirmwareUpdateNvmRequest WriteChunk(uint offset, ReadOnlySpan<byte> data)
    {
        Span<byte> subCommandParameters = stackalloc byte[6 + data.Length];
        offset.WriteBytesBE(subCommandParameters[..4]);
        ((ushort)data.Length).WriteBytesBE(subCommandParameters[4..6]);
        data.CopyTo(subCommandParameters[6..]);
        return Create(FirmwareUpdateNvmSubCommand.WriteChunk, subCommandParameters);
    }

    /// <summary>
    /// Create a request to perform the firmware update.
    /// </summary>
    /// <param name="target">The update target (firmware or bootloader).</param>
    public static FirmwareUpdateNvmRequest PerformUpdate(FirmwareUpdateTarget target)
    {
        ReadOnlySpan<byte> subCommandParameters = [(byte)target];
        return Create(FirmwareUpdateNvmSubCommand.PerformUpdate, subCommandParameters);
    }

    public static FirmwareUpdateNvmRequest Create(DataFrame frame) => new FirmwareUpdateNvmRequest(frame);
}

/// <summary>
/// Response to a <see cref="FirmwareUpdateNvmRequest"/> command.
/// </summary>
public readonly struct FirmwareUpdateNvmResponse : ICommand<FirmwareUpdateNvmResponse>
{
    public FirmwareUpdateNvmResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.FirmwareUpdateNvm;

    public DataFrame Frame { get; }

    /// <summary>
    /// The sub-command this response corresponds to.
    /// </summary>
    public FirmwareUpdateNvmSubCommand SubCommand => (FirmwareUpdateNvmSubCommand)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the firmware update operation.
    /// </summary>
    public FirmwareUpdateNvmStatus Status => (FirmwareUpdateNvmStatus)Frame.CommandParameters.Span[1];

    public static FirmwareUpdateNvmResponse Create(DataFrame frame) => new FirmwareUpdateNvmResponse(frame);
}
