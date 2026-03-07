using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class HumidityControlSetpointCommandClassTests
{
    [TestMethod]
    public void CapabilitiesGetCommand_Create_HasCorrectFormat()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesGetCommand.Create(HumidityControlSetpointType.Humidifier);

        Assert.AreEqual(CommandClassId.HumidityControlSetpoint, HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesGetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlSetpointCommand.CapabilitiesGet, HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void CapabilitiesGetCommand_Create_Dehumidifier()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesGetCommand.Create(HumidityControlSetpointType.Dehumidifier);

        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_1ByteValues()
    {
        // Type=Humidifier, Min: precision=0, scale=0, size=1, value=20
        // Max: precision=0, scale=0, size=1, value=80
        // CC=0x64, Cmd=0x09, Type=0x01, MinPSS=0x01, MinVal=0x14, MaxPSS=0x01, MaxVal=0x50
        byte[] data = [0x64, 0x09, 0x01, 0x01, 0x14, 0x01, 0x50];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointCapabilities caps = HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Humidifier, caps.SetpointType);
        Assert.AreEqual(HumidityControlSetpointScale.Percentage, caps.MinimumScale);
        Assert.AreEqual(20.0, caps.MinimumValue);
        Assert.AreEqual(HumidityControlSetpointScale.Percentage, caps.MaximumScale);
        Assert.AreEqual(80.0, caps.MaximumValue);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_2ByteValues()
    {
        // Type=Dehumidifier, Min: prec=1, scale=0, size=2, value=150 (15.0)
        // Max: prec=1, scale=0, size=2, value=950 (95.0)
        // MinPSS = (1<<5)|(0<<3)|2 = 0x22, MinVal = 0x0096
        // MaxPSS = (1<<5)|(0<<3)|2 = 0x22, MaxVal = 0x03B6
        byte[] data = [0x64, 0x09, 0x02, 0x22, 0x00, 0x96, 0x22, 0x03, 0xB6];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointCapabilities caps = HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Dehumidifier, caps.SetpointType);
        Assert.AreEqual(15.0, caps.MinimumValue, 0.001);
        Assert.AreEqual(95.0, caps.MaximumValue, 0.001);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_DifferentScales()
    {
        // Min: percentage scale, Max: absolute humidity scale
        // Type=Humidifier, Min: prec=0, scale=0, size=1, value=20
        // Max: prec=0, scale=1, size=1, value=50
        // MinPSS = (0<<5)|(0<<3)|1 = 0x01
        // MaxPSS = (0<<5)|(1<<3)|1 = 0x09
        byte[] data = [0x64, 0x09, 0x01, 0x01, 0x14, 0x09, 0x32];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointCapabilities caps = HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointScale.Percentage, caps.MinimumScale);
        Assert.AreEqual(20.0, caps.MinimumValue);
        Assert.AreEqual(HumidityControlSetpointScale.AbsoluteHumidity, caps.MaximumScale);
        Assert.AreEqual(50.0, caps.MaximumValue);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_4ByteValues()
    {
        // Type=Auto, Min: prec=2, scale=0, size=4, value=1000 (10.00)
        // Max: prec=2, scale=0, size=4, value=9500 (95.00)
        // MinPSS = (2<<5)|(0<<3)|4 = 0x44
        // MaxPSS = (2<<5)|(0<<3)|4 = 0x44
        byte[] data = [0x64, 0x09, 0x03,
            0x44, 0x00, 0x00, 0x03, 0xE8,
            0x44, 0x00, 0x00, 0x25, 0x1C];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointCapabilities caps = HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Auto, caps.SetpointType);
        Assert.AreEqual(10.0, caps.MinimumValue, 0.001);
        Assert.AreEqual(95.0, caps.MaximumValue, 0.001);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_MixedSizes()
    {
        // Min uses 1 byte, Max uses 2 bytes
        // Type=Humidifier, Min: prec=0, scale=0, size=1, value=10
        // Max: prec=0, scale=0, size=2, value=200
        // MinPSS = 0x01, MaxPSS = 0x02
        byte[] data = [0x64, 0x09, 0x01, 0x01, 0x0A, 0x02, 0x00, 0xC8];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointCapabilities caps = HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(10.0, caps.MinimumValue);
        Assert.AreEqual(200.0, caps.MaximumValue);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_TooShort_Throws()
    {
        // Need at least type + min PSS + min value + max PSS + max value
        byte[] data = [0x64, 0x09, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_TooShort_MinValueMissing_Throws()
    {
        // Type + MinPSS(size=1) but no min value byte
        byte[] data = [0x64, 0x09, 0x01, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_TooShort_MaxValueMissing_Throws()
    {
        // Type + MinPSS + MinVal, but no max PSS or value
        byte[] data = [0x64, 0x09, 0x01, 0x01, 0x14];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_TooShort_MaxValueTruncated_Throws()
    {
        // Type + MinPSS + MinVal + MaxPSS(size=2) but only 1 max value byte
        byte[] data = [0x64, 0x09, 0x01, 0x01, 0x14, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }
}
