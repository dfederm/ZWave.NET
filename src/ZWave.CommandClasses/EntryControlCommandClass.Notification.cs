using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents an Entry Control notification received from a device.
/// </summary>
public readonly record struct EntryControlNotification(
    /// <summary>
    /// Gets the sequence number used for duplicate detection.
    /// </summary>
    byte SequenceNumber,

    /// <summary>
    /// Gets the type of data carried in <see cref="EventData"/>.
    /// </summary>
    EntryControlDataType DataType,

    /// <summary>
    /// Gets the event type.
    /// </summary>
    EntryControlEventType EventType,

    /// <summary>
    /// Gets the event data. For ASCII data, trailing 0xFF padding bytes are removed.
    /// </summary>
    ReadOnlyMemory<byte> EventData)
{
    /// <summary>
    /// Gets the event data as a string when <see cref="DataType"/> is <see cref="EntryControlDataType.Ascii"/>,
    /// or <see langword="null"/> otherwise.
    /// </summary>
    public string? EventDataString => DataType == EntryControlDataType.Ascii
        ? Encoding.ASCII.GetString(EventData.Span)
        : null;
}

public sealed partial class EntryControlCommandClass
{
    /// <summary>
    /// Gets the last notification received from the device.
    /// </summary>
    public EntryControlNotification? LastNotification { get; private set; }

    /// <summary>
    /// Occurs when a notification is received from the device.
    /// </summary>
    public event Action<EntryControlNotification>? OnNotificationReceived;

    internal readonly struct EntryControlNotificationCommand : ICommand
    {
        public EntryControlNotificationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.Notification;

        public CommandClassFrame Frame { get; }

        public static EntryControlNotification Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: SequenceNumber(1) + Reserved|DataType(1) + EventType(1) + EventDataLength(1) = 4 bytes
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning(
                    "Entry Control Notification frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Notification frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            byte sequenceNumber = span[0];
            EntryControlDataType dataType = (EntryControlDataType)(span[1] & 0b0000_0011);
            EntryControlEventType eventType = (EntryControlEventType)span[2];
            byte eventDataLength = span[3];

            if (eventDataLength > 32)
            {
                logger.LogWarning(
                    "Entry Control Notification event data length {Length} exceeds maximum of 32",
                    eventDataLength);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Notification event data length exceeds maximum");
            }

            if (frame.CommandParameters.Length < 4 + eventDataLength)
            {
                logger.LogWarning(
                    "Entry Control Notification frame too short for declared event data ({Length} bytes, need {Expected})",
                    frame.CommandParameters.Length,
                    4 + eventDataLength);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Notification frame is too short for event data");
            }

            ReadOnlyMemory<byte> eventData = eventDataLength > 0
                ? frame.CommandParameters.Slice(4, eventDataLength)
                : ReadOnlyMemory<byte>.Empty;

            // For ASCII data, trim trailing 0xFF padding per spec §2.2.43.2
            if (dataType == EntryControlDataType.Ascii && eventData.Length > 0)
            {
                ReadOnlySpan<byte> eventDataSpan = eventData.Span;
                int trimmedLength = eventData.Length;
                while (trimmedLength > 0 && eventDataSpan[trimmedLength - 1] == 0xFF)
                {
                    trimmedLength--;
                }

                eventData = eventData[..trimmedLength];
            }

            return new EntryControlNotification(sequenceNumber, dataType, eventType, eventData);
        }
    }
}
