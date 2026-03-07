using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class EntryControlCommandClassTests
{
    [TestMethod]
    public void Notification_Parse_BasicNotification_NoEventData()
    {
        // CC=0x6F, Cmd=0x01, SeqNum=0x05, Reserved|DataType=0x00(None), EventType=0x0F(Bell), Length=0
        byte[] data = [0x6F, 0x01, 0x05, 0x00, 0x0F, 0x00];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x05, notification.SequenceNumber);
        Assert.AreEqual(EntryControlDataType.None, notification.DataType);
        Assert.AreEqual(EntryControlEventType.Bell, notification.EventType);
        Assert.AreEqual(0, notification.EventData.Length);
        Assert.IsNull(notification.EventDataString);
    }

    [TestMethod]
    public void Notification_Parse_RawData()
    {
        // CC=0x6F, Cmd=0x01, SeqNum=0x01, DataType=0x01(Raw), EventType=0x0E(RFID), Length=3, Data=0xAA,0xBB,0xCC
        byte[] data = [0x6F, 0x01, 0x01, 0x01, 0x0E, 0x03, 0xAA, 0xBB, 0xCC];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x01, notification.SequenceNumber);
        Assert.AreEqual(EntryControlDataType.Raw, notification.DataType);
        Assert.AreEqual(EntryControlEventType.Rfid, notification.EventType);
        Assert.AreEqual(3, notification.EventData.Length);
        Assert.AreEqual((byte)0xAA, notification.EventData.Span[0]);
        Assert.AreEqual((byte)0xBB, notification.EventData.Span[1]);
        Assert.AreEqual((byte)0xCC, notification.EventData.Span[2]);
        Assert.IsNull(notification.EventDataString);
    }

    [TestMethod]
    public void Notification_Parse_AsciiData_PaddingTrimmed()
    {
        // ASCII "1234" padded with 0xFF to 16 bytes
        byte[] data = [0x6F, 0x01, 0x0A, 0x02, 0x02, 0x10,
            0x31, 0x32, 0x33, 0x34, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(EntryControlDataType.Ascii, notification.DataType);
        Assert.AreEqual(EntryControlEventType.Enter, notification.EventType);
        // Padding should be trimmed, leaving only "1234"
        Assert.AreEqual(4, notification.EventData.Length);
        Assert.AreEqual((byte)'1', notification.EventData.Span[0]);
        Assert.AreEqual((byte)'2', notification.EventData.Span[1]);
        Assert.AreEqual((byte)'3', notification.EventData.Span[2]);
        Assert.AreEqual((byte)'4', notification.EventData.Span[3]);
        Assert.AreEqual("1234", notification.EventDataString);
    }

    [TestMethod]
    public void Notification_Parse_AsciiData_AllPadding_ResultsInEmptyData()
    {
        // All 0xFF padding (edge case)
        byte[] data = [0x6F, 0x01, 0x00, 0x02, 0x00, 0x10,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(EntryControlDataType.Ascii, notification.DataType);
        Assert.AreEqual(0, notification.EventData.Length);
        Assert.AreEqual(string.Empty, notification.EventDataString);
    }

    [TestMethod]
    public void Notification_Parse_Md5Data_NotTrimmed()
    {
        // MD5 data should not be trimmed even if it contains 0xFF bytes
        byte[] data = [0x6F, 0x01, 0x00, 0x03, 0x0E, 0x10,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0xFF];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(EntryControlDataType.Md5, notification.DataType);
        Assert.AreEqual(16, notification.EventData.Length);
        Assert.AreEqual((byte)0xFF, notification.EventData.Span[15]);
    }

    [TestMethod]
    public void Notification_Parse_DataTypeExtractedFrom2Bits()
    {
        // Byte 1 has reserved bits 7-2 set to non-zero (e.g. 0xFE = 11111110, DataType = 10 = Ascii)
        byte[] data = [0x6F, 0x01, 0x00, 0xFE, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        // Only lower 2 bits (0b10 = 2) should be used for DataType
        Assert.AreEqual(EntryControlDataType.Ascii, notification.DataType);
    }

    [TestMethod]
    public void Notification_Parse_EventTypeFullByte()
    {
        // Event type 0x19 (Cancel) requires more than 4 bits
        byte[] data = [0x6F, 0x01, 0x00, 0x00, 0x19, 0x00];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(EntryControlEventType.Cancel, notification.EventType);
    }

    [TestMethod]
    public void Notification_Parse_TooShort_Throws()
    {
        // Only 3 command parameter bytes (need at least 4)
        byte[] data = [0x6F, 0x01, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Notification_Parse_EventDataLengthExceedsMax_Throws()
    {
        // Event data length = 33 (exceeds max of 32)
        byte[] data = [0x6F, 0x01, 0x00, 0x01, 0x01, 0x21];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Notification_Parse_FrameTooShortForDeclaredData_Throws()
    {
        // Declared length = 5 but only 2 data bytes present
        byte[] data = [0x6F, 0x01, 0x00, 0x01, 0x01, 0x05, 0xAA, 0xBB];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Notification_Parse_CachingEvent_NoData()
    {
        // Caching event with no data
        byte[] data = [0x6F, 0x01, 0x42, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        EntryControlNotification notification =
            EntryControlCommandClass.EntryControlNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x42, notification.SequenceNumber);
        Assert.AreEqual(EntryControlDataType.None, notification.DataType);
        Assert.AreEqual(EntryControlEventType.Caching, notification.EventType);
        Assert.AreEqual(0, notification.EventData.Length);
    }
}
