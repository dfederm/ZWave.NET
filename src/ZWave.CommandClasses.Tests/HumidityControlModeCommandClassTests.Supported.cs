using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class HumidityControlModeCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        HumidityControlModeCommandClass.HumidityControlModeSupportedGetCommand command =
            HumidityControlModeCommandClass.HumidityControlModeSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.HumidityControlMode, HumidityControlModeCommandClass.HumidityControlModeSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlModeCommand.SupportedGet, HumidityControlModeCommandClass.HumidityControlModeSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_HumidifyAndDehumidify()
    {
        // CC=0x6D, Cmd=0x05, BitMask=0x06 (bits 1,2 = Humidify, Dehumidify)
        byte[] data = [0x6D, 0x05, 0x06];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlMode> supported = HumidityControlModeCommandClass.HumidityControlModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(HumidityControlMode.Humidify, supported);
        Assert.Contains(HumidityControlMode.Dehumidify, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_AllModes()
    {
        // CC=0x6D, Cmd=0x05, BitMask=0x0E (bits 1,2,3 = Humidify, Dehumidify, Auto)
        byte[] data = [0x6D, 0x05, 0x0E];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlMode> supported = HumidityControlModeCommandClass.HumidityControlModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, supported);
        Assert.Contains(HumidityControlMode.Humidify, supported);
        Assert.Contains(HumidityControlMode.Dehumidify, supported);
        Assert.Contains(HumidityControlMode.Auto, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_ReservedBit0Ignored()
    {
        // Bit 0 is reserved per spec and should be ignored by the receiver.
        // CC=0x6D, Cmd=0x05, BitMask=0x07 (bits 0,1,2 — bit 0 reserved)
        byte[] data = [0x6D, 0x05, 0x07];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlMode> supported = HumidityControlModeCommandClass.HumidityControlModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(HumidityControlMode.Humidify, supported);
        Assert.Contains(HumidityControlMode.Dehumidify, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_EmptyMask()
    {
        // CC=0x6D, Cmd=0x05, BitMask=0x00
        byte[] data = [0x6D, 0x05, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlMode> supported = HumidityControlModeCommandClass.HumidityControlModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_MultipleMaskBytes()
    {
        // Two mask bytes - forward compatible
        // CC=0x6D, Cmd=0x05, Mask1=0x06, Mask2=0x00
        byte[] data = [0x6D, 0x05, 0x06, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlMode> supported = HumidityControlModeCommandClass.HumidityControlModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(HumidityControlMode.Humidify, supported);
        Assert.Contains(HumidityControlMode.Dehumidify, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x6D, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlModeCommandClass.HumidityControlModeSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
