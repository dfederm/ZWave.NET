using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class UserCodeCommandClassTests
{
    [TestMethod]
    public void CapabilitiesGetCommand_Create_HasCorrectFormat()
    {
        UserCodeCommandClass.CapabilitiesGetCommand command =
            UserCodeCommandClass.CapabilitiesGetCommand.Create();

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.CapabilitiesGetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.CapabilitiesGet, UserCodeCommandClass.CapabilitiesGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_AllFeatures()
    {
        // Byte 0: AC=1, ACD=1, Reserved=0, StatusBitmaskLen=1
        //   = 0b1100_0001 = 0xC1
        // Byte 1 (status bitmask): bits 0,1,2,3,4 set (Available, EnabledGrantAccess, Disabled, Messaging, PassageMode)
        //   = 0b0001_1111 = 0x1F
        // Byte 2: UCC=1, MUCR=1, MUCS=1, KeypadModesBitmaskLen=1
        //   = 0b1110_0001 = 0xE1
        // Byte 3 (keypad modes bitmask): bits 0,1,2,3 set (Normal, Vacation, Privacy, LockedOut)
        //   = 0b0000_1111 = 0x0F
        // Byte 4: Reserved=0, KeysBitmaskLen=2
        //   = 0x02
        // Bytes 5-6 (keys bitmask): support ASCII 0x30-0x39 (digits 0-9)
        //   Byte 5 covers ASCII 0-7, byte 6 covers ASCII 8-15
        //   Digit 0x30=48: byte 48/8=6, bit 48%8=0 → but we only have 2 bitmask bytes covering 0-15
        //   Let's support ASCII 0x00-0x03 for simplicity
        //   Byte 5: bits 0,1,2,3 set = 0x0F
        //   Byte 6: bit 0 set (ASCII 8) = 0x01
        byte[] data = [0x63, 0x07, 0xC1, 0x1F, 0xE1, 0x0F, 0x02, 0x0F, 0x01];
        CommandClassFrame frame = new(data);

        UserCodeCapabilities capabilities =
            UserCodeCommandClass.CapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(capabilities.AdminCodeSupport);
        Assert.IsTrue(capabilities.AdminCodeDeactivationSupport);
        Assert.IsTrue(capabilities.ChecksumSupport);
        Assert.IsTrue(capabilities.MultipleReportSupport);
        Assert.IsTrue(capabilities.MultipleSetSupport);

        // 5 statuses: Available, EnabledGrantAccess, Disabled, Messaging, PassageMode
        Assert.HasCount(5, capabilities.SupportedStatuses);
        Assert.Contains(UserIdStatus.Available, capabilities.SupportedStatuses);
        Assert.Contains(UserIdStatus.EnabledGrantAccess, capabilities.SupportedStatuses);
        Assert.Contains(UserIdStatus.Disabled, capabilities.SupportedStatuses);
        Assert.Contains(UserIdStatus.Messaging, capabilities.SupportedStatuses);
        Assert.Contains(UserIdStatus.PassageMode, capabilities.SupportedStatuses);

        // 4 keypad modes
        Assert.HasCount(4, capabilities.SupportedKeypadModes);
        Assert.Contains(UserCodeKeypadMode.Normal, capabilities.SupportedKeypadModes);
        Assert.Contains(UserCodeKeypadMode.Vacation, capabilities.SupportedKeypadModes);
        Assert.Contains(UserCodeKeypadMode.Privacy, capabilities.SupportedKeypadModes);
        Assert.Contains(UserCodeKeypadMode.LockedOut, capabilities.SupportedKeypadModes);

        // Keys: ASCII 0,1,2,3 from byte 5, and ASCII 8 from byte 6
        Assert.HasCount(5, capabilities.SupportedKeys);
        Assert.Contains((byte)0, capabilities.SupportedKeys);
        Assert.Contains((byte)1, capabilities.SupportedKeys);
        Assert.Contains((byte)2, capabilities.SupportedKeys);
        Assert.Contains((byte)3, capabilities.SupportedKeys);
        Assert.Contains((byte)8, capabilities.SupportedKeys);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_MinimalFeatures()
    {
        // AC=0, ACD=0, StatusBitmaskLen=1
        //   = 0b0000_0001 = 0x01
        // Status bitmask: bits 0,1,2 set (Available, EnabledGrantAccess, Disabled — mandatory per spec)
        //   = 0b0000_0111 = 0x07
        // UCC=0, MUCR=0, MUCS=0, KeypadModesBitmaskLen=1
        //   = 0b0000_0001 = 0x01
        // Keypad modes bitmask: bit 0 set (Normal — mandatory per spec)
        //   = 0x01
        // KeysBitmaskLen=0
        //   = 0x00
        byte[] data = [0x63, 0x07, 0x01, 0x07, 0x01, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        UserCodeCapabilities capabilities =
            UserCodeCommandClass.CapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(capabilities.AdminCodeSupport);
        Assert.IsFalse(capabilities.AdminCodeDeactivationSupport);
        Assert.IsFalse(capabilities.ChecksumSupport);
        Assert.IsFalse(capabilities.MultipleReportSupport);
        Assert.IsFalse(capabilities.MultipleSetSupport);

        Assert.HasCount(3, capabilities.SupportedStatuses);
        Assert.HasCount(1, capabilities.SupportedKeypadModes);
        Assert.IsEmpty(capabilities.SupportedKeys);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_WithDigitKeys()
    {
        // Test parsing with actual digit keys (0x30-0x39)
        // AC=0, ACD=0, StatusBitmaskLen=1
        byte[] data = new byte[2 + 1 + 1 + 1 + 1 + 1 + 7];
        data[0] = 0x63; // CC
        data[1] = 0x07; // Cmd
        data[2] = 0x01; // StatusBitmaskLen=1
        data[3] = 0x07; // Status bits 0,1,2
        data[4] = 0x01; // KeypadModesBitmaskLen=1
        data[5] = 0x01; // Keypad mode bit 0
        data[6] = 0x07; // KeysBitmaskLen=7 (to cover ASCII 0x30-0x39, need bytes 0-6, since 0x39/8=7.125)
        // ASCII 0x30=48: byte 6, bit 0; 0x31=49: byte 6, bit 1; ... 0x37=55: byte 6, bit 7
        // ASCII 0x38=56: byte 7, bit 0; 0x39=57: byte 7, bit 1
        // But we have bytes starting at index 7-13 (7 bytes: covering ASCII 0-55)
        // We need at least 8 bytes to cover up to ASCII 63
        // Actually let's simplify: KeysBitmaskLen=7 covers ASCII 0..55
        // 0x30=48 is in byte 6 (48/8=6), bit 0 (48%8=0)
        // 0x37=55 is in byte 6 (55/8=6), bit 7 (55%8=7)
        // So byte 6 of the bitmask (index 7+6=13) should be 0xFF for digits 0-7
        // But that's only if bitmask length >= 7
        data[7] = 0x00; // ASCII 0-7
        data[8] = 0x00; // ASCII 8-15
        data[9] = 0x00; // ASCII 16-23
        data[10] = 0x00; // ASCII 24-31
        data[11] = 0x00; // ASCII 32-39
        data[12] = 0x00; // ASCII 40-47
        data[13] = 0xFF; // ASCII 48-55 (digits '0' through '7')
        CommandClassFrame frame = new(data);

        UserCodeCapabilities capabilities =
            UserCodeCommandClass.CapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        // Should have ASCII 48-55 (0x30-0x37)
        Assert.HasCount(8, capabilities.SupportedKeys);
        for (byte i = 0x30; i <= 0x37; i++)
        {
            Assert.Contains(i, capabilities.SupportedKeys);
        }
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x63, 0x07, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.CapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_TooShortForStatusBitmask_Throws()
    {
        // StatusBitmaskLen=2 but only 1 byte of bitmask follows
        byte[] data = [0x63, 0x07, 0x02, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.CapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }
}
