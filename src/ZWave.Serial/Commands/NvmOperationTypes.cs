namespace ZWave.Serial.Commands;

/// <summary>
/// NVM operation sub-commands.
/// </summary>
public enum NvmOperationSubCommand : byte
{
    Open = 0x00,
    Read = 0x01,
    Write = 0x02,
    Close = 0x03,
}

/// <summary>
/// Status of an NVM operation.
/// </summary>
public enum NvmOperationStatus : byte
{
    OK = 0x00,
    Error = 0x01,
    OperationMismatch = 0x02,
    OperationInterference = 0x03,
    SubCommandNotSupported = 0x04,
    EndOfFile = 0xFF,
}
