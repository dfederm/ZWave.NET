using Microsoft.Extensions.Logging;

namespace ZWave.Serial;

internal static partial class Logging
{
    /* SerialApi: 100-199 */

    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Trace,
        Message = "ZWave Serial port `{portName}` opened")]
    public static partial void LogSerialApiPortOpened(this ILogger logger, string portName);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Trace,
        Message = "ZWave Serial port `{portName}` closed")]
    public static partial void LogSerialApiPortClosed(this ILogger logger, string portName);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Trace,
        Message = "ZWave Serial port `{portName}` re-opened")]
    public static partial void LogSerialApiPortReopened(this ILogger logger, string portName);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Trace,
        Message = "Skipped {numBytes} bytes of invalid data from the Serial port")]
    public static partial void LogSerialApiSkippedBytes(this ILogger logger, long numBytes);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Trace,
        Message = "Received Serial API frame: {frame}")]
    public static partial void LogSerialApiFrameReceived(this ILogger logger, Frame frame);

    [LoggerMessage(
        EventId = 105,
        Level = LogLevel.Trace,
        Message = "Received Serial API data frame: {frame}")]
    public static partial void LogSerialApiDataFrameReceived(this ILogger logger, DataFrame frame);

    [LoggerMessage(
        EventId = 106,
        Level = LogLevel.Trace,
        Message = "Received invalid Serial API data frame: {frame}")]
    public static partial void LogSerialApiInvalidDataFrameReceived(this ILogger logger, DataFrame frame);

    [LoggerMessage(
        EventId = 107,
        Level = LogLevel.Trace,
        Message = "Sent Serial API frame: {frame}")]
    public static partial void LogSerialApiFrameSent(this ILogger logger, Frame frame);

    [LoggerMessage(
        EventId = 108,
        Level = LogLevel.Trace,
        Message = "Sent Serial API data frame: {frame}")]
    public static partial void LogSerialApiDataFrameSent(this ILogger logger, DataFrame frame);

    [LoggerMessage(
        EventId = 109,
        Level = LogLevel.Trace,
        Message = "Received frame transmission reply unexpectedly: {frame}")]
    public static partial void LogSerialApiUnexpectedFrame(this ILogger logger, Frame frame);

    [LoggerMessage(
        EventId = 110,
        Level = LogLevel.Trace,
        Message = "Received Serial API frame with unknown type `{frameType}`")]
    public static partial void LogSerialApiFrameUnknownType(this ILogger logger, FrameType frameType);

    [LoggerMessage(
        EventId = 112,
        Level = LogLevel.Trace,
        Message = "Serial API read was cancelled")]
    public static partial void LogSerialApiReadCancellation(this ILogger logger);

    [LoggerMessage(
        EventId = 113,
        Level = LogLevel.Warning,
        Message = "Serial API read exception")]
    public static partial void LogSerialApiReadException(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 114,
        Level = LogLevel.Warning,
        Message = "Serial API write exception")]
    public static partial void LogSerialApiWriteException(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 115,
        Level = LogLevel.Warning,
        Message = "Serial API frame transmission did not receive an ACK before the timeout period")]
    public static partial void LogSerialApiFrameDeliveryAckTimeout(this ILogger logger);

    [LoggerMessage(
        EventId = 116,
        Level = LogLevel.Warning,
        Message = "Serial API frame transmission failed (attempt #{attempt})")]
    public static partial void LogSerialApiFrameTransmissionRetry(this ILogger logger, int attempt);
}
