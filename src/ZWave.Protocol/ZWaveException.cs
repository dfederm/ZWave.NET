using System.Diagnostics.CodeAnalysis;

namespace ZWave;

/// <summary>
/// Exception thrown by ZWave.NET for various errors.
/// </summary>
public sealed class ZWaveException : Exception
{
    private ZWaveException(ZWaveErrorCode errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the error code identifying the class of error.
    /// </summary>
    public ZWaveErrorCode ErrorCode { get; }

    /// <summary>
    /// Throws a <see cref="ZWaveException"/> with the specified error code and message.
    /// </summary>
    [DoesNotReturn]
    public static void Throw(ZWaveErrorCode errorCode, string message)
        => throw new ZWaveException(errorCode, message);

    /// <summary>
    /// Throws a <see cref="ZWaveException"/> with the specified error code, message, and inner exception.
    /// </summary>
    [DoesNotReturn]
    public static void Throw(ZWaveErrorCode errorCode, string message, Exception innerException)
        => throw new ZWaveException(errorCode, message, innerException);

    /// <summary>
    /// Creates a <see cref="ZWaveException"/> without throwing it.
    /// For use with <see cref="TaskCompletionSource.SetException(Exception)"/> and similar APIs.
    /// </summary>
    internal static ZWaveException Create(ZWaveErrorCode errorCode, string message)
        => new(errorCode, message);
}
