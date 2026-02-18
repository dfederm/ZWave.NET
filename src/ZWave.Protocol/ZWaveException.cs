using System.Runtime.Serialization;

namespace ZWave;

/// <summary>
/// Exception thrown by ZWave.NET for various errors.
/// </summary>
public sealed class ZWaveException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZWaveException"/> class with the specified error code, message, and inner exception.
    /// </summary>
    public ZWaveException(ZWaveErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZWaveException"/> class with the specified error code and message.
    /// </summary>
    public ZWaveException(ZWaveErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    // Serialization constructor
    private ZWaveException(SerializationInfo info, StreamingContext context)
    {

    }

    /// <summary>
    /// Gets the error code identifying the class of error.
    /// </summary>
    public ZWaveErrorCode ErrorCode { get; }
}
