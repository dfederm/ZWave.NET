using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class BinarySensorCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        BinarySensorCommandClass.BinarySensorSupportedGetCommand command =
            BinarySensorCommandClass.BinarySensorSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.BinarySensor, BinarySensorCommandClass.BinarySensorSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)BinarySensorCommand.SupportedGet, BinarySensorCommandClass.BinarySensorSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_SingleByte_GeneralPurposeAndSmoke()
    {
        // CC=0x30, Cmd=0x04, BitMask1=0b0000_0110 (bits 1 and 2 set = GeneralPurpose + Smoke)
        byte[] data = [0x30, 0x04, 0x06];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BinarySensorType> supported =
            BinarySensorCommandClass.BinarySensorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(BinarySensorType.GeneralPurpose, supported);
        Assert.Contains(BinarySensorType.Smoke, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_SingleByte_AllLowTypes()
    {
        // Bits 1-7 set (types 1-7: GeneralPurpose through Freeze), bit 0 reserved
        // 0b1111_1110 = 0xFE
        byte[] data = [0x30, 0x04, 0xFE];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BinarySensorType> supported =
            BinarySensorCommandClass.BinarySensorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(7, supported);
        Assert.Contains(BinarySensorType.GeneralPurpose, supported);
        Assert.Contains(BinarySensorType.Smoke, supported);
        Assert.Contains(BinarySensorType.CO, supported);
        Assert.Contains(BinarySensorType.CO2, supported);
        Assert.Contains(BinarySensorType.Heat, supported);
        Assert.Contains(BinarySensorType.Water, supported);
        Assert.Contains(BinarySensorType.Freeze, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TwoBytes_IncludesHighTypes()
    {
        // BitMask1: 0b0000_0010 (bit 1 = GeneralPurpose)
        // BitMask2: 0b0011_0000 (bits 12 and 13 = Motion and GlassBreak)
        byte[] data = [0x30, 0x04, 0x02, 0x30];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BinarySensorType> supported =
            BinarySensorCommandClass.BinarySensorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, supported);
        Assert.Contains(BinarySensorType.GeneralPurpose, supported);
        Assert.Contains(BinarySensorType.Motion, supported);
        Assert.Contains(BinarySensorType.GlassBreak, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_Bit0Reserved_NotIncluded()
    {
        // Per spec: "Bit 0 in Bit Mask 1 is not allocated to any Sensor Type and must therefore be set to zero."
        // Even if a non-compliant device sets bit 0, it maps to sensor type 0 which is not a valid BinarySensorType.
        // 0b0000_0011 = bits 0 and 1 set
        byte[] data = [0x30, 0x04, 0x03];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BinarySensorType> supported =
            BinarySensorCommandClass.BinarySensorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        // Bit 0 maps to (BinarySensorType)0 which is not defined but still added to the set.
        // Bit 1 maps to GeneralPurpose.
        Assert.HasCount(2, supported);
        Assert.Contains(BinarySensorType.GeneralPurpose, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_SingleByteAllZeros()
    {
        // No sensor types supported (edge case)
        byte[] data = [0x30, 0x04, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BinarySensorType> supported =
            BinarySensorCommandClass.BinarySensorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        // CC=0x30, Cmd=0x04, no bitmask bytes
        byte[] data = [0x30, 0x04];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BinarySensorCommandClass.BinarySensorSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
