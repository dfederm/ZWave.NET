using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class HumidityControlSetpointCommandClassTests
{
    [TestMethod]
    public void ScaleSupportedGetCommand_Create_HasCorrectFormat()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedGetCommand.Create(HumidityControlSetpointType.Humidifier);

        Assert.AreEqual(CommandClassId.HumidityControlSetpoint, HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlSetpointCommand.ScaleSupportedGet, HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ScaleSupportedReport_Parse_PercentageOnly()
    {
        // CC=0x64, Cmd=0x07, ScaleBitMask=0x01 (bit 0 = Percentage)
        byte[] data = [0x64, 0x07, 0x01];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointScale> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, supported);
        Assert.Contains(HumidityControlSetpointScale.Percentage, supported);
    }

    [TestMethod]
    public void ScaleSupportedReport_Parse_BothScales()
    {
        // CC=0x64, Cmd=0x07, ScaleBitMask=0x03 (bits 0,1 = Percentage, AbsoluteHumidity)
        byte[] data = [0x64, 0x07, 0x03];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointScale> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(HumidityControlSetpointScale.Percentage, supported);
        Assert.Contains(HumidityControlSetpointScale.AbsoluteHumidity, supported);
    }

    [TestMethod]
    public void ScaleSupportedReport_Parse_AbsoluteHumidityOnly()
    {
        // CC=0x64, Cmd=0x07, ScaleBitMask=0x02 (bit 1 = AbsoluteHumidity)
        byte[] data = [0x64, 0x07, 0x02];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointScale> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, supported);
        Assert.Contains(HumidityControlSetpointScale.AbsoluteHumidity, supported);
    }

    [TestMethod]
    public void ScaleSupportedReport_Parse_ReservedUpperBitsIgnored()
    {
        // Upper 4 bits are reserved; only lower 4 bits matter
        // CC=0x64, Cmd=0x07, 0xF3 = reserved upper bits | both scales
        byte[] data = [0x64, 0x07, 0xF3];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointScale> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(HumidityControlSetpointScale.Percentage, supported);
        Assert.Contains(HumidityControlSetpointScale.AbsoluteHumidity, supported);
    }

    [TestMethod]
    public void ScaleSupportedReport_Parse_EmptyMask()
    {
        byte[] data = [0x64, 0x07, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<HumidityControlSetpointScale> supported = HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void ScaleSupportedReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x64, 0x07];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointScaleSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
