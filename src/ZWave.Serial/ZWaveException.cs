using System.Runtime.Serialization;

namespace ZWave;

/// <summary>
/// Exception thrown by ZWave.NET for various errors.
/// </summary>
public sealed class ZWaveException : Exception
{
    public ZWaveException(ZWaveErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public ZWaveException(ZWaveErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    // Serialization constructor
    private ZWaveException(SerializationInfo info, StreamingContext context)
    {

    }

    public ZWaveErrorCode ErrorCode { get; }
}
