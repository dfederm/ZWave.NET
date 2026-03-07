using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the supported events, data types, and configuration ranges of an Entry Control device.
/// </summary>
public readonly record struct EntryControlEventSupportedReport(
    /// <summary>
    /// Gets the data types supported by the device.
    /// </summary>
    IReadOnlySet<EntryControlDataType> SupportedDataTypes,

    /// <summary>
    /// Gets the event types supported by the device.
    /// </summary>
    IReadOnlySet<EntryControlEventType> SupportedEventTypes,

    /// <summary>
    /// Gets the minimum configurable key cache size.
    /// </summary>
    byte KeyCachedSizeMinimum,

    /// <summary>
    /// Gets the maximum configurable key cache size.
    /// </summary>
    byte KeyCachedSizeMaximum,

    /// <summary>
    /// Gets the minimum configurable key cache timeout in seconds.
    /// </summary>
    byte KeyCachedTimeoutMinimum,

    /// <summary>
    /// Gets the maximum configurable key cache timeout in seconds.
    /// </summary>
    byte KeyCachedTimeoutMaximum);

public sealed partial class EntryControlCommandClass
{
    /// <summary>
    /// Gets the supported events, data types, and configuration ranges reported by the device.
    /// </summary>
    public EntryControlEventSupportedReport? EventCapabilities { get; private set; }

    /// <summary>
    /// Request the supported events, data types, and configuration ranges from the device.
    /// </summary>
    public async Task<EntryControlEventSupportedReport> GetEventSupportedAsync(CancellationToken cancellationToken)
    {
        EntryControlEventSupportedGetCommand command = EntryControlEventSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<EntryControlEventSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        EntryControlEventSupportedReport report = EntryControlEventSupportedReportCommand.Parse(reportFrame, Logger);
        EventCapabilities = report;
        return report;
    }

    internal readonly struct EntryControlEventSupportedGetCommand : ICommand
    {
        public EntryControlEventSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.EventSupportedGet;

        public CommandClassFrame Frame { get; }

        public static EntryControlEventSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new EntryControlEventSupportedGetCommand(frame);
        }
    }

    internal readonly struct EntryControlEventSupportedReportCommand : ICommand
    {
        public EntryControlEventSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.EventSupportedReport;

        public CommandClassFrame Frame { get; }

        public static EntryControlEventSupportedReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: Reserved|DataTypeBitmaskLength(1)
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Entry Control Event Supported Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Event Supported Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            int offset = 0;

            // Byte 0: Reserved[7:2] | DataTypeSupportedBitmaskLength[1:0]
            byte dataTypeBitmaskLength = (byte)(span[offset] & 0b0000_0011);
            offset++;

            // Validate we have enough bytes for the data type bitmask
            if (span.Length < offset + dataTypeBitmaskLength)
            {
                logger.LogWarning(
                    "Entry Control Event Supported Report too short for data type bitmask ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Event Supported Report frame is too short for data type bitmask");
            }

            // Parse data type supported bitmask
            HashSet<EntryControlDataType> supportedDataTypes = BitMaskHelper.ParseBitMask<EntryControlDataType>(span.Slice(offset, dataTypeBitmaskLength));

            offset += dataTypeBitmaskLength;

            // Event Type Supported Bitmask Length (1 byte)
            if (span.Length < offset + 1)
            {
                logger.LogWarning(
                    "Entry Control Event Supported Report too short for event type bitmask length ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Event Supported Report frame is too short for event type bitmask length");
            }

            byte eventTypeBitmaskLength = span[offset];
            offset++;

            // Validate we have enough bytes for the event type bitmask + 4 trailing config bytes
            if (span.Length < offset + eventTypeBitmaskLength + 4)
            {
                logger.LogWarning(
                    "Entry Control Event Supported Report too short for event bitmask and config ({Length} bytes, need {Expected})",
                    frame.CommandParameters.Length,
                    offset + eventTypeBitmaskLength + 4);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Event Supported Report frame is too short for event bitmask and configuration");
            }

            // Parse event type supported bitmask
            HashSet<EntryControlEventType> supportedEventTypes = BitMaskHelper.ParseBitMask<EntryControlEventType>(span.Slice(offset, eventTypeBitmaskLength));

            offset += eventTypeBitmaskLength;

            // Configuration range fields
            byte keyCachedSizeMin = span[offset];
            byte keyCachedSizeMax = span[offset + 1];
            byte keyCachedTimeoutMin = span[offset + 2];
            byte keyCachedTimeoutMax = span[offset + 3];

            return new EntryControlEventSupportedReport(
                supportedDataTypes,
                supportedEventTypes,
                keyCachedSizeMin,
                keyCachedSizeMax,
                keyCachedTimeoutMin,
                keyCachedTimeoutMax);
        }
    }
}
