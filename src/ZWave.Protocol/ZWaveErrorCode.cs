namespace ZWave;

/// <summary>
/// Identifies a class of error in ZWave.NET
/// </summary>
public enum ZWaveErrorCode
{
    /// <summary>
    /// The driver failed to initialize.
    /// </summary>
    DriverInitializationFailed,

    /// <summary>
    /// The controller failed to initialize.
    /// </summary>
    ControllerInitializationFailed,

    /// <summary>
    /// A ZWave command failed to send.
    /// </summary>
    CommandSendFailed,

    /// <summary>
    /// A ZWave command failed
    /// </summary>
    CommandFailed,

    /// <summary>
    /// The command class is not supported by the node.
    /// </summary>
    CommandClassNotImplemented,

    /// <summary>
    /// The command is not supported by the node.
    /// </summary>
    CommandNotSupported,

    /// <summary>
    /// The command is not ready to execute yet as it relies on the interview process.
    /// </summary>
    CommandNotReady,

    /// <summary>
    /// The command was called with an invalid argument.
    /// </summary>
    CommandInvalidArgument,

    /// <summary>
    /// A received command frame had an invalid or malformed payload.
    /// </summary>
    InvalidPayload,
}
