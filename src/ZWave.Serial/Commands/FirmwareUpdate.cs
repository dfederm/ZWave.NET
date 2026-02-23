namespace ZWave.Serial.Commands;

/// <summary>
/// Firmware update sub-commands for the legacy 500-series Firmware Update API (0x78).
/// </summary>
public enum FirmwareUpdateSubCommand : byte
{
    /// <summary>
    /// Initialize the Firmware Update functionality.
    /// </summary>
    Init = 0x00,

    /// <summary>
    /// Set the NEWIMAGE marker in NVM.
    /// </summary>
    SetNewImage = 0x01,

    /// <summary>
    /// Get the NEWIMAGE marker from NVM.
    /// </summary>
    GetNewImage = 0x02,

    /// <summary>
    /// Calculate CRC16 for a specified NVM block of data.
    /// </summary>
    UpdateCRC16 = 0x03,

    /// <summary>
    /// Check if firmware present in NVM is valid using CRC16.
    /// </summary>
    IsValidCRC16 = 0x04,

    /// <summary>
    /// Write a firmware image block to NVM.
    /// </summary>
    Write = 0x05,
}

/// <summary>
/// The legacy 500-series Firmware Update API provides functionality for implementing
/// firmware update via external NVM.
/// </summary>
/// <remarks>
/// All sub-commands are sent using a single Serial API function ID (0x78).
/// The application MUST call Init prior to calling any other sub-command.
/// </remarks>
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
        Span<byte> commandParameters = stackalloc byte[1 + subCommandParameters.Length];
        commandParameters[0] = (byte)subCommand;
        subCommandParameters.CopyTo(commandParameters[1..]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new FirmwareUpdateRequest(frame);
    }

    /// <summary>
    /// Create a request to initialize the Firmware Update functionality.
    /// </summary>
    /// <remarks>
    /// Returns whether the attached NVM supports firmware update.
    /// </remarks>
    public static FirmwareUpdateRequest Init()
        => Create(FirmwareUpdateSubCommand.Init, []);

    /// <summary>
    /// Create a request to set the NEWIMAGE marker in NVM.
    /// </summary>
    /// <param name="value">The value to set the NEWIMAGE marker to.</param>
    public static FirmwareUpdateRequest SetNewImage(byte value)
        => Create(FirmwareUpdateSubCommand.SetNewImage, [value]);

    /// <summary>
    /// Create a request to get the NEWIMAGE marker from NVM.
    /// </summary>
    public static FirmwareUpdateRequest GetNewImage()
        => Create(FirmwareUpdateSubCommand.GetNewImage, []);

    /// <summary>
    /// Create a request to calculate CRC16 for a block of NVM data.
    /// </summary>
    /// <param name="offset">24-bit offset into NVM (full address space).</param>
    /// <param name="length">Size of the block to calculate CRC16 on.</param>
    /// <param name="seedCrc16">Seed CRC16 value to start calculation with.</param>
    public static FirmwareUpdateRequest UpdateCRC16(uint offset, ushort length, ushort seedCrc16)
    {
        Span<byte> subCommandParameters = stackalloc byte[7];
        // 3-byte big-endian offset
        subCommandParameters[0] = (byte)(offset >> 16);
        subCommandParameters[1] = (byte)(offset >> 8);
        subCommandParameters[2] = (byte)offset;
        length.WriteBytesBE(subCommandParameters[3..5]);
        seedCrc16.WriteBytesBE(subCommandParameters[5..7]);
        return Create(FirmwareUpdateSubCommand.UpdateCRC16, subCommandParameters);
    }

    /// <summary>
    /// Create a request to check if firmware in NVM is valid using CRC16.
    /// </summary>
    public static FirmwareUpdateRequest IsValidCRC16()
        => Create(FirmwareUpdateSubCommand.IsValidCRC16, []);

    /// <summary>
    /// Create a request to write a firmware image block to NVM.
    /// </summary>
    /// <param name="offset">24-bit offset in firmware where the data should be written.</param>
    /// <param name="length">Size of the block to write.</param>
    /// <param name="data">The firmware data to write.</param>
    public static FirmwareUpdateRequest Write(uint offset, ushort length, ReadOnlySpan<byte> data)
    {
        Span<byte> subCommandParameters = stackalloc byte[5 + data.Length];
        // 3-byte big-endian offset
        subCommandParameters[0] = (byte)(offset >> 16);
        subCommandParameters[1] = (byte)(offset >> 8);
        subCommandParameters[2] = (byte)offset;
        length.WriteBytesBE(subCommandParameters[3..5]);
        data.CopyTo(subCommandParameters[5..]);
        return Create(FirmwareUpdateSubCommand.Write, subCommandParameters);
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
    /// The return value byte. Meaning depends on sub-command.
    /// </summary>
    public byte ReturnValue => Frame.CommandParameters.Span[1];

    /// <summary>
    /// For <see cref="FirmwareUpdateSubCommand.UpdateCRC16"/>: the resulting CRC16 value (big-endian).
    /// </summary>
    public ushort CRC16 => (ushort)((Frame.CommandParameters.Span[1] << 8) | Frame.CommandParameters.Span[2]);

    /// <summary>
    /// For <see cref="FirmwareUpdateSubCommand.IsValidCRC16"/>: the resulting CRC16 value (big-endian).
    /// Available when <see cref="ReturnValue"/> is TRUE.
    /// </summary>
    public ushort ValidCRC16 => (ushort)((Frame.CommandParameters.Span[2] << 8) | Frame.CommandParameters.Span[3]);

    public static FirmwareUpdateResponse Create(DataFrame frame, CommandParsingContext context) => new FirmwareUpdateResponse(frame);
}
