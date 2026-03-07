using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class HumidityControlSetpointCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_Humidifier()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand.Create(HumidityControlSetpointType.Humidifier);

        Assert.AreEqual(CommandClassId.HumidityControlSetpoint, HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlSetpointCommand.Get, HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Dehumidifier()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand.Create(HumidityControlSetpointType.Dehumidifier);

        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Auto()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand.Create(HumidityControlSetpointType.Auto);

        Assert.AreEqual(0x03, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_ReservedBitsClear()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointGetCommand.Create(HumidityControlSetpointType.Auto);

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0] & 0xF0);
    }

    [TestMethod]
    public void SetCommand_Create_Percentage_1ByteValue()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand.Create(
                HumidityControlSetpointType.Humidifier,
                HumidityControlSetpointScale.Percentage,
                50.0);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(CommandClassId.HumidityControlSetpoint, HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlSetpointCommand.Set, HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand.CommandId);

        // Type byte
        Assert.AreEqual(0x01, parameters[0] & 0x0F);
        // PSS byte: precision=0, scale=0 (percentage), size=1
        Assert.AreEqual(0x01, parameters[1]);
        // Value: 50 = 0x32
        Assert.AreEqual(0x32, parameters[2]);
    }

    [TestMethod]
    public void SetCommand_Create_AbsoluteHumidity()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand.Create(
                HumidityControlSetpointType.Dehumidifier,
                HumidityControlSetpointScale.AbsoluteHumidity,
                10.0);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Type byte: Dehumidifier = 0x02
        Assert.AreEqual(0x02, parameters[0] & 0x0F);
        // PSS byte: precision=0, scale=1 (absolute humidity), size=1
        Assert.AreEqual(0x09, parameters[1]); // 0b00_001_001
    }

    [TestMethod]
    public void SetCommand_Create_DecimalValue()
    {
        HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand command =
            HumidityControlSetpointCommandClass.HumidityControlSetpointSetCommand.Create(
                HumidityControlSetpointType.Humidifier,
                HumidityControlSetpointScale.Percentage,
                45.5);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Precision should be 1 (one decimal), value = 455
        int precision = (parameters[1] & 0b1110_0000) >> 5;
        int valueSize = parameters[1] & 0b0000_0111;
        Assert.AreEqual(1, precision);

        // 455 fits in 2 bytes (signed)
        Assert.AreEqual(2, valueSize);
        short rawValue = (short)((parameters[2] << 8) | parameters[3]);
        Assert.AreEqual(455, rawValue);
    }

    [TestMethod]
    public void Report_Parse_Percentage_1Byte()
    {
        // CC=0x64, Cmd=0x03, Type=0x01 (Humidifier), PSS=0x01 (prec=0, scale=0, size=1), Value=0x32 (50)
        byte[] data = [0x64, 0x03, 0x01, 0x01, 0x32];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointReport report = HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Humidifier, report.SetpointType);
        Assert.AreEqual(HumidityControlSetpointScale.Percentage, report.Scale);
        Assert.AreEqual(50.0, report.Value);
    }

    [TestMethod]
    public void Report_Parse_AbsoluteHumidity_1Byte()
    {
        // CC=0x64, Cmd=0x03, Type=0x02 (Dehumidifier), PSS=0x09 (prec=0, scale=1, size=1), Value=0x0A (10)
        byte[] data = [0x64, 0x03, 0x02, 0x09, 0x0A];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointReport report = HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Dehumidifier, report.SetpointType);
        Assert.AreEqual(HumidityControlSetpointScale.AbsoluteHumidity, report.Scale);
        Assert.AreEqual(10.0, report.Value);
    }

    [TestMethod]
    public void Report_Parse_Precision2_2ByteValue()
    {
        // Value = 45.50 => raw = 4550 = 0x11C6, precision=2, scale=0, size=2
        // PSS = (2<<5) | (0<<3) | 2 = 0x42
        byte[] data = [0x64, 0x03, 0x01, 0x42, 0x11, 0xC6];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointReport report = HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Humidifier, report.SetpointType);
        Assert.AreEqual(HumidityControlSetpointScale.Percentage, report.Scale);
        Assert.AreEqual(45.50, report.Value, 0.001);
    }

    [TestMethod]
    public void Report_Parse_4ByteValue()
    {
        // Value = 1000 => raw = 1000 = 0x000003E8, precision=0, scale=0, size=4
        // PSS = (0<<5) | (0<<3) | 4 = 0x04
        byte[] data = [0x64, 0x03, 0x03, 0x04, 0x00, 0x00, 0x03, 0xE8];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointReport report = HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Auto, report.SetpointType);
        Assert.AreEqual(1000.0, report.Value);
    }

    [TestMethod]
    public void Report_Parse_NegativeValue()
    {
        // Value = -5 => raw = -5, 1-byte signed = 0xFB, precision=0, scale=0, size=1
        // PSS = (0<<5) | (0<<3) | 1 = 0x01
        byte[] data = [0x64, 0x03, 0x01, 0x01, 0xFB];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointReport report = HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(-5.0, report.Value);
    }

    [TestMethod]
    public void Report_Parse_ReservedBitsIgnored()
    {
        // Upper 4 bits of type byte are reserved
        // CC=0x64, Cmd=0x03, Type=0xF1 (reserved | Humidifier), PSS=0x01, Value=0x32
        byte[] data = [0x64, 0x03, 0xF1, 0x01, 0x32];
        CommandClassFrame frame = new(data);

        HumidityControlSetpointReport report = HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlSetpointType.Humidifier, report.SetpointType);
    }

    [TestMethod]
    public void Report_Parse_TooShort_NoParameters_Throws()
    {
        byte[] data = [0x64, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_TooShort_OnlyType_Throws()
    {
        byte[] data = [0x64, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_TooShort_ValueSizeMismatch_Throws()
    {
        // PSS says size=2 but only 1 value byte provided
        byte[] data = [0x64, 0x03, 0x01, 0x02, 0x32];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_InvalidValueSize_Throws()
    {
        // PSS says size=3 (invalid, only 1/2/4 allowed), provide 3 bytes
        byte[] data = [0x64, 0x03, 0x01, 0x03, 0x01, 0x02, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlSetpointCommandClass.HumidityControlSetpointReportCommand.Parse(frame, NullLogger.Instance));
    }
}
