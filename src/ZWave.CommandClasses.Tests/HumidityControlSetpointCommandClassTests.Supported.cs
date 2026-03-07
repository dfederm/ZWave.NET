using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class HumidityControlSetpointCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.HumidityControlSetpoint, HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlSetpointCommand.SupportedGet, HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_HumidifierAndDehumidifier()
    {
        // CC=0x64, Cmd=0x05, BitMask=0x06 (bits 1,2 = Humidifier, Dehumidifier)
        byte[] data = [0x64, 0x05, 0x06];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointType> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(HumidityControlSetpointType.Humidifier, supported);
        Assert.Contains(HumidityControlSetpointType.Dehumidifier, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_AllTypes()
    {
        // CC=0x64, Cmd=0x05, BitMask=0x0E (bits 1,2,3 = Humidifier, Dehumidifier, Auto)
        byte[] data = [0x64, 0x05, 0x0E];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointType> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, supported);
        Assert.Contains(HumidityControlSetpointType.Humidifier, supported);
        Assert.Contains(HumidityControlSetpointType.Dehumidifier, supported);
        Assert.Contains(HumidityControlSetpointType.Auto, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_ReservedBit0Ignored()
    {
        // Bit 0 is reserved per spec. If a device sets it, it should be ignored.
        // CC=0x64, Cmd=0x05, BitMask=0x07 (bits 0,1,2)
        byte[] data = [0x64, 0x05, 0x07];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointType> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedReportCommand.Parse(frame, NullLogger.Instance);

        // Bit 0 should not produce a result since it's reserved (type 0 is invalid)
        Assert.HasCount(2, supported);
        Assert.Contains(HumidityControlSetpointType.Humidifier, supported);
        Assert.Contains(HumidityControlSetpointType.Dehumidifier, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_EmptyMask()
    {
        byte[] data = [0x64, 0x05, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointType> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_MultipleMaskBytes()
    {
        // CC=0x64, Cmd=0x05, Mask1=0x02, Mask2=0x00
        byte[] data = [0x64, 0x05, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointType> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, supported);
        Assert.Contains(HumidityControlSetpointType.Humidifier, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x64, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
