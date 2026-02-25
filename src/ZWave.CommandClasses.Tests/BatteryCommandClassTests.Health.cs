using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class BatteryCommandClassTests
{
    [TestMethod]
    public void HealthGetCommand_Create_HasCorrectFormat()
    {
        BatteryCommandClass.BatteryHealthGetCommand command = BatteryCommandClass.BatteryHealthGetCommand.Create();

        Assert.AreEqual(CommandClassId.Battery, BatteryCommandClass.BatteryHealthGetCommand.CommandClassId);
        Assert.AreEqual((byte)BatteryCommand.HealthGet, BatteryCommandClass.BatteryHealthGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void HealthReport_Parse_KnownCapacity_1ByteTemperature()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=90,
        // Precision=1, Scale=0 (Celsius), Size=1 => 0b001_00_001 = 0x21
        // Temperature=25 (raw) => 2.5°C with precision 1
        byte[] data = [0x80, 0x05, 90, 0x21, 25];
        CommandClassFrame frame = new(data);

        BatteryHealth report = BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)90, report.MaximumCapacity);
        Assert.AreEqual(BatteryTemperatureScale.Celsius, report.BatteryTemperatureScale);
        Assert.IsNotNull(report.BatteryTemperature);
        Assert.AreEqual(2.5, report.BatteryTemperature.Value, 0.001);
    }

    [TestMethod]
    public void HealthReport_Parse_KnownCapacity_2ByteTemperature()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=75,
        // Precision=2, Scale=0, Size=2 => 0b010_00_010 = 0x42
        // Temperature=0x09C4 (2500 raw) => 25.00°C with precision 2
        byte[] data = [0x80, 0x05, 75, 0x42, 0x09, 0xC4];
        CommandClassFrame frame = new(data);

        BatteryHealth report = BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)75, report.MaximumCapacity);
        Assert.AreEqual(BatteryTemperatureScale.Celsius, report.BatteryTemperatureScale);
        Assert.IsNotNull(report.BatteryTemperature);
        Assert.AreEqual(25.00, report.BatteryTemperature.Value, 0.001);
    }

    [TestMethod]
    public void HealthReport_Parse_KnownCapacity_4ByteTemperature()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=100,
        // Precision=0, Scale=0, Size=4 => 0b000_00_100 = 0x04
        // Temperature=0x00000017 (23) => 23°C with precision 0
        byte[] data = [0x80, 0x05, 100, 0x04, 0x00, 0x00, 0x00, 0x17];
        CommandClassFrame frame = new(data);

        BatteryHealth report = BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)100, report.MaximumCapacity);
        Assert.IsNotNull(report.BatteryTemperature);
        Assert.AreEqual(23.0, report.BatteryTemperature.Value, 0.001);
    }

    [TestMethod]
    public void HealthReport_Parse_UnknownCapacity()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=0xFF (unknown),
        // Precision=0, Scale=0, Size=1 => 0x01
        // Temperature=20
        byte[] data = [0x80, 0x05, 0xFF, 0x01, 20];
        CommandClassFrame frame = new(data);

        BatteryHealth report = BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNull(report.MaximumCapacity);
        Assert.IsNotNull(report.BatteryTemperature);
        Assert.AreEqual(20.0, report.BatteryTemperature.Value, 0.001);
    }

    [TestMethod]
    public void HealthReport_Parse_UnknownTemperature()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=80,
        // Precision=0, Scale=0, Size=0 => 0x00
        // No temperature bytes
        byte[] data = [0x80, 0x05, 80, 0x00];
        CommandClassFrame frame = new(data);

        BatteryHealth report = BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)80, report.MaximumCapacity);
        Assert.IsNull(report.BatteryTemperature);
    }

    [TestMethod]
    public void HealthReport_Parse_NegativeTemperature()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=50,
        // Precision=1, Scale=0, Size=2 => 0b001_00_010 = 0x22
        // Temperature=0xFF9C (-100 signed 16-bit) => -10.0°C with precision 1
        byte[] data = [0x80, 0x05, 50, 0x22, 0xFF, 0x9C];
        CommandClassFrame frame = new(data);

        BatteryHealth report = BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(report.BatteryTemperature);
        Assert.AreEqual(-10.0, report.BatteryTemperature.Value, 0.001);
    }

    [TestMethod]
    public void HealthReport_Parse_InvalidSize_Throws()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=80,
        // Precision=0, Scale=0, Size=3 (invalid) => 0x03
        byte[] data = [0x80, 0x05, 80, 0x03, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void HealthReport_Parse_InvalidSize5_Throws()
    {
        // Size=5 (invalid)
        byte[] data = [0x80, 0x05, 80, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void HealthReport_Parse_TooShort_Throws()
    {
        // CC=0x80, Cmd=0x05, only 1 parameter byte (needs 2)
        byte[] data = [0x80, 0x05, 80];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void HealthReport_Parse_TruncatedTemperature_Throws()
    {
        // CC=0x80, Cmd=0x05,
        // MaxCapacity=80,
        // Precision=0, Scale=0, Size=2 => 0x02
        // Only 1 byte of temperature (needs 2)
        byte[] data = [0x80, 0x05, 80, 0x02, 0x15];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BatteryCommandClass.BatteryHealthReportCommand.Parse(frame, NullLogger.Instance));
    }
}
