using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class EntryControlCommandClassTests
{
    [TestMethod]
    public void KeySupportedGetCommand_Create_HasCorrectFormat()
    {
        EntryControlCommandClass.EntryControlKeySupportedGetCommand command =
            EntryControlCommandClass.EntryControlKeySupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.EntryControl, EntryControlCommandClass.EntryControlKeySupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)EntryControlCommand.KeySupportedGet, EntryControlCommandClass.EntryControlKeySupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void KeySupportedReport_Parse_NumericKeys()
    {
        // Bitmask for ASCII codes '0' (0x30) through '9' (0x39)
        // '0' = 0x30 = bit 0 in byte 6, '1' = 0x31 = bit 1 in byte 6, ...
        // '8' = 0x38 = bit 0 in byte 7, '9' = 0x39 = bit 1 in byte 7
        // We need 8 bitmask bytes (byte 0-7) to cover ASCII codes up to 0x3F
        byte[] data = new byte[2 + 1 + 8]; // CC + Cmd + Length + 8 bitmask bytes
        data[0] = 0x6F; // CC
        data[1] = 0x03; // Cmd (Key Supported Report)
        data[2] = 0x08; // Bitmask length = 8 bytes
        // Byte 6 (codes 0x30-0x37): bits 0-7 all set = '0' through '7'
        data[2 + 1 + 6] = 0xFF;
        // Byte 7 (codes 0x38-0x3F): bits 0-1 set = '8' and '9'
        data[2 + 1 + 7] = 0x03;

        CommandClassFrame frame = new(data);

        IReadOnlySet<char> keys =
            EntryControlCommandClass.EntryControlKeySupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(10, keys);
        for (char k = '0'; k <= '9'; k++)
        {
            Assert.Contains(k, keys);
        }
    }

    [TestMethod]
    public void KeySupportedReport_Parse_SingleByte()
    {
        // Bitmask length = 1, bitmask = 0b00000110 (codes 1 and 2)
        byte[] data = [0x6F, 0x03, 0x01, 0x06];
        CommandClassFrame frame = new(data);

        IReadOnlySet<char> keys =
            EntryControlCommandClass.EntryControlKeySupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, keys);
        Assert.Contains((char)1, keys);
        Assert.Contains((char)2, keys);
    }

    [TestMethod]
    public void KeySupportedReport_Parse_EmptyBitmask()
    {
        // Bitmask length = 1, all zeros
        byte[] data = [0x6F, 0x03, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<char> keys =
            EntryControlCommandClass.EntryControlKeySupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(keys);
    }

    [TestMethod]
    public void KeySupportedReport_Parse_ZeroLengthBitmask()
    {
        // Bitmask length = 0
        byte[] data = [0x6F, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<char> keys =
            EntryControlCommandClass.EntryControlKeySupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(keys);
    }

    [TestMethod]
    public void KeySupportedReport_Parse_TooShort_Throws()
    {
        // No command parameters
        byte[] data = [0x6F, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlKeySupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void KeySupportedReport_Parse_BitmaskShorterThanDeclared_Throws()
    {
        // Declared length = 5 but only 2 bitmask bytes present
        byte[] data = [0x6F, 0x03, 0x05, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlKeySupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
