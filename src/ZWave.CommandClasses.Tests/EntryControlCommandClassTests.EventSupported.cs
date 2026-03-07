using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class EntryControlCommandClassTests
{
    [TestMethod]
    public void EventSupportedGetCommand_Create_HasCorrectFormat()
    {
        EntryControlCommandClass.EntryControlEventSupportedGetCommand command =
            EntryControlCommandClass.EntryControlEventSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.EntryControl, EntryControlCommandClass.EntryControlEventSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)EntryControlCommand.EventSupportedGet, EntryControlCommandClass.EntryControlEventSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_TypicalKeypad()
    {
        // A typical keypad supporting:
        //   Data Types: ASCII (0x02) → bit 2 in bitmask byte 0
        //   Event Types: CachedKeys(0x01), Enter(0x02), ArmAll(0x04), DisarmAll(0x03)
        //     → byte 0: bits 1,2,3,4 = 0b00011110 = 0x1E
        //   KeyCachedSize Min=1, Max=32, Timeout Min=1, Max=10
        byte[] data =
        [
            0x6F, 0x05,         // CC + Cmd
            0x01,               // Reserved[7:2]=0 | DataTypeBitmaskLength[1:0]=1
            0x04,               // DataType bitmask: bit 2 set (Ascii)
            0x01,               // EventTypeBitmaskLength=1
            0x1E,               // EventType bitmask: bits 1,2,3,4
            0x01,               // KeyCachedSizeMin
            0x20,               // KeyCachedSizeMax
            0x01,               // KeyCachedTimeoutMin
            0x0A,               // KeyCachedTimeoutMax
        ];
        CommandClassFrame frame = new(data);

        EntryControlEventSupportedReport report =
            EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, report.SupportedDataTypes);
        Assert.Contains(EntryControlDataType.Ascii, report.SupportedDataTypes);

        Assert.HasCount(4, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.CachedKeys, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.Enter, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.DisarmAll, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.ArmAll, report.SupportedEventTypes);

        Assert.AreEqual((byte)1, report.KeyCachedSizeMinimum);
        Assert.AreEqual((byte)32, report.KeyCachedSizeMaximum);
        Assert.AreEqual((byte)1, report.KeyCachedTimeoutMinimum);
        Assert.AreEqual((byte)10, report.KeyCachedTimeoutMaximum);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_MultipleDataTypes()
    {
        // Supports Raw(0x01) and Ascii(0x02): bits 1 and 2 → 0x06
        byte[] data =
        [
            0x6F, 0x05,
            0x01,               // DataTypeBitmaskLength=1
            0x06,               // Raw + Ascii
            0x01,               // EventTypeBitmaskLength=1
            0x01,               // CachedKeys only (bit 0 = Caching, bit 1 = CachedKeys... wait)
            0x04, 0x20, 0x02, 0x05,
        ];
        CommandClassFrame frame = new(data);

        EntryControlEventSupportedReport report =
            EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, report.SupportedDataTypes);
        Assert.Contains(EntryControlDataType.Raw, report.SupportedDataTypes);
        Assert.Contains(EntryControlDataType.Ascii, report.SupportedDataTypes);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_MultiByteEventBitmask()
    {
        // Event types up to 0x19 (Cancel) need 4 bytes of bitmask
        // Set bits for Caching(0x00), RFID(0x0E), Cancel(0x19)
        // Byte 0: bit 0 = Caching → 0x01
        // Byte 1: bit 6 = RFID (0x0E = 14, 14-8=6) → 0x40
        // Byte 2: nothing → 0x00
        // Byte 3: bit 1 = Cancel (0x19 = 25, 25-24=1) → 0x02
        byte[] data =
        [
            0x6F, 0x05,
            0x01,               // DataTypeBitmaskLength=1
            0x01,               // None data type
            0x04,               // EventTypeBitmaskLength=4
            0x01, 0x40, 0x00, 0x02,  // Event bitmask
            0x01, 0x20, 0x01, 0x0A,
        ];
        CommandClassFrame frame = new(data);

        EntryControlEventSupportedReport report =
            EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.Caching, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.Rfid, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.Cancel, report.SupportedEventTypes);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_ZeroDataTypeBitmask()
    {
        // DataTypeBitmaskLength = 0 (no data type bitmask bytes)
        byte[] data =
        [
            0x6F, 0x05,
            0x00,               // DataTypeBitmaskLength=0
            0x01,               // EventTypeBitmaskLength=1
            0x04,               // Enter event
            0x01, 0x20, 0x01, 0x0A,
        ];
        CommandClassFrame frame = new(data);

        EntryControlEventSupportedReport report =
            EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(report.SupportedDataTypes);
        Assert.HasCount(1, report.SupportedEventTypes);
        Assert.Contains(EntryControlEventType.Enter, report.SupportedEventTypes);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_DataTypeBitmaskLengthMasked()
    {
        // Reserved bits in byte 0 should be ignored; only lower 2 bits = length
        // 0xFD = 11111101 → lower 2 bits = 01 = length 1
        byte[] data =
        [
            0x6F, 0x05,
            0xFD,               // Reserved=0x3F | Length=1
            0x04,               // Ascii data type
            0x01,               // EventTypeBitmaskLength=1
            0x02,               // CachedKeys
            0x04, 0x20, 0x02, 0x0A,
        ];
        CommandClassFrame frame = new(data);

        EntryControlEventSupportedReport report =
            EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, report.SupportedDataTypes);
        Assert.Contains(EntryControlDataType.Ascii, report.SupportedDataTypes);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x6F, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void EventSupportedReport_Parse_TooShortForDataTypeBitmask_Throws()
    {
        // Declares 2 bitmask bytes but none present
        byte[] data = [0x6F, 0x05, 0x02];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void EventSupportedReport_Parse_TooShortForConfigFields_Throws()
    {
        // Has data type bitmask and event type bitmask, but missing config fields
        byte[] data =
        [
            0x6F, 0x05,
            0x01, 0x04,         // DataType bitmask
            0x01, 0x02,         // EventType bitmask
            // Missing 4 config bytes
        ];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlEventSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
