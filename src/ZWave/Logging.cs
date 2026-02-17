using Microsoft.Extensions.Logging;
using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave;

internal static partial class Logging
{
    /* Driver: 200-299 */

    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Information,
        Message = "Driver initialization sequence starting")]
    public static partial void LogDriverInitializing(this ILogger logger);

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Information,
        Message = "Performing soft reset")]
    public static partial void LogSoftReset(this ILogger logger);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Information,
        Message = "Driver initialization sequence complete")]
    public static partial void LogDriverInitialized(this ILogger logger);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Debug,
        Message = "Identifying controller")]
    public static partial void LogControllerIdentifying(this ILogger logger);

    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Information,
        Message = "Controller identity:\n" +
        "Home ID = {homeId}\n" +
        "Node ID = {nodeId}")]
    public static partial void LogControllerIdentity(this ILogger logger, uint homeId, byte nodeId);

    [LoggerMessage(
        EventId = 205,
        Level = LogLevel.Information,
        Message = "Serial API capabilities:\n" +
        "Serial API Version = {serialApiVersion}\n" +
        "Manufacturer ID = {manufacturerId}\n" +
        "Product type = {productType}\n" +
        "Product ID = {productId}\n" +
        "Supported Commands = {supportedCommands}")]
    public static partial void LogSerialApiCapabilities(
        this ILogger logger,
        Version serialApiVersion,
        ushort manufacturerId,
        ushort productType,
        ushort productId,
        string supportedCommands);

    [LoggerMessage(
        EventId = 206,
        Level = LogLevel.Information,
        Message = "Controller library:\n" +
        "Library version = {libraryVersion}\n" +
        "Library type = {libraryType}")]
    public static partial void LogControllerLibraryVersion(this ILogger logger, string libraryVersion, LibraryType libraryType);

    [LoggerMessage(
        EventId = 207,
        Level = LogLevel.Information,
        Message = "Controller capabilities: {controllerCapabilities}")]
    public static partial void LogControllerCapabilities(this ILogger logger, ControllerCapabilities controllerCapabilities);

    [LoggerMessage(
        EventId = 208,
        Level = LogLevel.Information,
        Message = "Supported Serial API Setup subcommands: {supportedSubcommands}")]
    public static partial void LogControllerSupportedSerialApiSetupSubcommands(this ILogger logger, string supportedSubcommands);

    [LoggerMessage(
        EventId = 209,
        Level = LogLevel.Debug,
        Message = "Enabling TX status report success: {success}")]
    public static partial void LogEnableTxStatusReport(this ILogger logger, bool success);

    [LoggerMessage(
        EventId = 210,
        Level = LogLevel.Debug,
        Message = "SUC Node Id: {sucNodeId}")]
    public static partial void LogControllerSucNodeId(this ILogger logger, byte sucNodeId);

    [LoggerMessage(
        EventId = 211,
        Level = LogLevel.Information,
        Message = "Init data:\n" +
        "API Version = {apiVersion}\n" +
        "API Capabilities = {apiCapabilities}\n" +
        "Chip type = {chipType}\n" +
        "Chip version = {chipVersion}\n" +
        "Node IDs = {nodeIds}")]
    public static partial void LogInitData(
        this ILogger logger,
        byte apiVersion,
        GetInitDataCapabilities apiCapabilities,
        byte chipType,
        byte chipVersion,
        string nodeIds);

    [LoggerMessage(
        EventId = 212,
        Level = LogLevel.Error,
        Message = "Data frame processing exception")]
    public static partial void LogDataFrameProcessingException(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 213,
        Level = LogLevel.Warning,
        Message = "Unexpected SerialApiStarted command")]
        public static partial void LogUnexpectedSerialApiStarted(this ILogger logger);

    [LoggerMessage(
        EventId = 214,
        Level = LogLevel.Warning,
        Message = "Unsolicited request for unknown node id {nodeId}")]
    public static partial void LogUnknownNodeId(this ILogger logger, int nodeId);

    [LoggerMessage(
        EventId = 215,
        Level = LogLevel.Warning,
        Message = "Unexpected response frame: {frame}")]
    public static partial void LogUnexpectedResponseFrame(this ILogger logger, DataFrame frame);

    [LoggerMessage(
        EventId = 216,
        Level = LogLevel.Warning,
        Message = "Unexpected command id in response frame. Expected command id: {expectedCommandId}. Recieved frame: {frame}")]
    public static partial void LogUnexpectedCommandIdResponseFrame(this ILogger logger, CommandId expectedCommandId, DataFrame frame);

    [LoggerMessage(
        EventId = 217,
        Level = LogLevel.Trace,
        Message = "Received Serial API data frame with unknown type `{dataFrameType}`")]
    public static partial void LogSerialApiDataFrameUnknownType(this ILogger logger, DataFrameType dataFrameType);
}
