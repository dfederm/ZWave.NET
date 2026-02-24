namespace ZWave.Serial.Commands;

/// <summary>
/// Firmware update sub-commands.
/// </summary>
public enum FirmwareUpdateSubCommand : byte
{
    Prepare = 0x00,
    WriteChunk = 0x01,
    PerformUpdate = 0x02,
}

/// <summary>
/// Status of a firmware update operation.
/// </summary>
public enum FirmwareUpdateStatus : byte
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
public readonly partial struct FirmwareUpdateRequest : ICommand<FirmwareUpdateRequest>
{
    public FirmwareUpdateRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.FirmwareUpdate;

    public DataFrame Frame { get; }

    private static FirmwareUpdateRequest Create(FirmwareUpdateSubCommand subCommand, ReadOnlySpan<byte> subCommandParameters)
    {
        Span<byte> commandParameters = stackalloc byte[subCommandParameters.Length + 1];
        commandParameters[0] = (byte)subCommand;
        subCommandParameters.CopyTo(commandParameters[1..]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new FirmwareUpdateRequest(frame);
    }

    /// <summary>
    /// Create a request to prepare for a firmware update.
    /// </summary>
    public static FirmwareUpdateRequest Prepare()
        => Create(FirmwareUpdateSubCommand.Prepare, []);

    /// <summary>
    /// Create a request to write a firmware chunk.
    /// </summary>
    /// <param name="offset">The 32-bit offset for the chunk.</param>
    /// <param name="data">The firmware data chunk.</param>
    public static FirmwareUpdateRequest WriteChunk(uint offset, ReadOnlySpan<byte> data)
    {
        Span<byte> subCommandParameters = stackalloc byte[6 + data.Length];
        offset.WriteBytesBE(subCommandParameters[..4]);
        ((ushort)data.Length).WriteBytesBE(subCommandParameters[4..6]);
        data.CopyTo(subCommandParameters[6..]);
        return Create(FirmwareUpdateSubCommand.WriteChunk, subCommandParameters);
    }

    /// <summary>
    /// Create a request to perform the firmware update.
    /// </summary>
    /// <param name="target">The update target (firmware or bootloader).</param>
    public static FirmwareUpdateRequest PerformUpdate(FirmwareUpdateTarget target)
    {
        ReadOnlySpan<byte> subCommandParameters = [(byte)target];
        return Create(FirmwareUpdateSubCommand.PerformUpdate, subCommandParameters);
    }

    public static FirmwareUpdateRequest Create(DataFrame frame, CommandParsingContext context) => new FirmwareUpdateRequest(frame);
}

/// <summary>
/// Response to a <see cref="FirmwareUpdateRequest"/> command.
/// </summary>
public readonly struct FirmwareUpdateResponse : ICommand<FirmwareUpdateResponse>
{
    public FirmwareUpdateResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.FirmwareUpdate;

    public DataFrame Frame { get; }

    /// <summary>
    /// The sub-command this response corresponds to.
    /// </summary>
    public FirmwareUpdateSubCommand SubCommand => (FirmwareUpdateSubCommand)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the firmware update operation.
    /// </summary>
    public FirmwareUpdateStatus Status => (FirmwareUpdateStatus)Frame.CommandParameters.Span[1];

    public static FirmwareUpdateResponse Create(DataFrame frame, CommandParsingContext context) => new FirmwareUpdateResponse(frame);
}
