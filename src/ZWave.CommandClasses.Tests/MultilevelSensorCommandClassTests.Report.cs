using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultilevelSensorCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_Version1_NoParams_HasCorrectFormat()
    {
        var command = MultilevelSensorCommandClass.MultilevelSensorGetCommand.Create(
            version: 1,
            sensorType: null,
            scaleId: null);

        Assert.AreEqual(CommandClassId.MultilevelSensor, MultilevelSensorCommandClass.MultilevelSensorGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSensorCommand.Get, MultilevelSensorCommandClass.MultilevelSensorGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void GetCommand_Create_Version4_SensorTypeIgnored_HasCorrectFormat()
    {
        // Version < 5 should not include sensor type/scale even if provided
        var command = MultilevelSensorCommandClass.MultilevelSensorGetCommand.Create(
            version: 4,
            sensorType: MultilevelSensorType.AirTemperature,
            scaleId: 0);

        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void GetCommand_Create_Version5_WithSensorTypeAndScale_HasCorrectFormat()
    {
        var command = MultilevelSensorCommandClass.MultilevelSensorGetCommand.Create(
            version: 5,
            sensorType: MultilevelSensorType.AirTemperature,
            scaleId: 1);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)MultilevelSensorType.AirTemperature, command.Frame.CommandParameters.Span[0]);

        // Scale 1 should be in bits 4-3: (1 << 3) = 0b0000_1000
        Assert.AreEqual(0b0000_1000, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_Version5_Scale0_HasCorrectFormat()
    {
        var command = MultilevelSensorCommandClass.MultilevelSensorGetCommand.Create(
            version: 5,
            sensorType: MultilevelSensorType.Illuminance,
            scaleId: 0);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)MultilevelSensorType.Illuminance, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0b0000_0000, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_Version5_Scale3_HasCorrectFormat()
    {
        var command = MultilevelSensorCommandClass.MultilevelSensorGetCommand.Create(
            version: 5,
            sensorType: MultilevelSensorType.AirTemperature,
            scaleId: 3);

        Assert.AreEqual(4, command.Frame.Data.Length);

        // Scale 3 should be in bits 4-3: (3 << 3) = 0b0001_1000
        Assert.AreEqual(0b0001_1000, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_Version5_NullSensorType_NoParams()
    {
        // V5 but no sensor type specified should create command without parameters
        var command = MultilevelSensorCommandClass.MultilevelSensorGetCommand.Create(
            version: 5,
            sensorType: null,
            scaleId: null);

        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void GetCommand_Create_Version5_NullScaleId_NoParams()
    {
        // V5 but no scaleId should create command without parameters
        var command = MultilevelSensorCommandClass.MultilevelSensorGetCommand.Create(
            version: 5,
            sensorType: MultilevelSensorType.AirTemperature,
            scaleId: null);

        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Size1_AirTemperature_Celsius()
    {
        // Sensor Type = AirTemperature (0x01)
        // Precision = 1 (0b001), Scale = 0 (Celsius, 0b00), Size = 1 (0b001)
        // Level byte = 0b001_00_001 = 0x21
        // Value = 0xE7 = -25 (signed), with precision 1 → -2.5
        byte[] data = [0x31, 0x05, 0x01, 0x21, 0xE7];
        CommandClassFrame frame = new(data);

        MultilevelSensorReport report = MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.AirTemperature, report.SensorType);
        Assert.AreEqual(MultilevelSensorType.AirTemperature.GetScale(0), report.Scale);
        Assert.AreEqual(-2.5, report.Value, 0.001);
    }

    [TestMethod]
    public void Report_Parse_Size2_AirTemperature_Fahrenheit()
    {
        // Sensor Type = AirTemperature (0x01)
        // Precision = 2 (0b010), Scale = 1 (Fahrenheit, 0b01), Size = 2 (0b010)
        // Level byte = 0b010_01_010 = 0x4A
        // Value = 0x04_01 = 1025 (signed), with precision 2 → 10.25
        byte[] data = [0x31, 0x05, 0x01, 0x4A, 0x04, 0x01];
        CommandClassFrame frame = new(data);

        MultilevelSensorReport report = MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.AirTemperature, report.SensorType);
        Assert.AreEqual(MultilevelSensorType.AirTemperature.GetScale(1), report.Scale);
        Assert.AreEqual(10.25, report.Value, 0.001);
    }

    [TestMethod]
    public void Report_Parse_Size4_Illuminance_Lux()
    {
        // Sensor Type = Illuminance (0x03)
        // Precision = 0 (0b000), Scale = 1 (Lux, 0b01), Size = 4 (0b100)
        // Level byte = 0b000_01_100 = 0x0C
        // Value = 0x00_00_03_E8 = 1000, with precision 0 → 1000.0
        byte[] data = [0x31, 0x05, 0x03, 0x0C, 0x00, 0x00, 0x03, 0xE8];
        CommandClassFrame frame = new(data);

        MultilevelSensorReport report = MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.Illuminance, report.SensorType);
        Assert.AreEqual(MultilevelSensorType.Illuminance.GetScale(1), report.Scale);
        Assert.AreEqual(1000.0, report.Value, 0.001);
    }

    [TestMethod]
    public void Report_Parse_NegativeValue_Size2()
    {
        // Sensor Type = AirTemperature (0x01)
        // Precision = 1 (0b001), Scale = 0 (Celsius, 0b00), Size = 2 (0b010)
        // Level byte = 0b001_00_010 = 0x22
        // Value = 0xFF_38 = -200 (signed 16-bit), with precision 1 → -20.0
        byte[] data = [0x31, 0x05, 0x01, 0x22, 0xFF, 0x38];
        CommandClassFrame frame = new(data);

        MultilevelSensorReport report = MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.AirTemperature, report.SensorType);
        Assert.AreEqual(-20.0, report.Value, 0.001);
    }

    [TestMethod]
    public void Report_Parse_ZeroValue()
    {
        // Sensor Type = AirTemperature (0x01)
        // Precision = 0, Scale = 0, Size = 1
        // Level byte = 0b000_00_001 = 0x01
        // Value = 0x00 = 0
        byte[] data = [0x31, 0x05, 0x01, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        MultilevelSensorReport report = MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(0.0, report.Value, 0.001);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // Only 1 command parameter byte (need at least 3: sensor type + level + value)
        byte[] data = [0x31, 0x05, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_ValueSizeExceedsPayload_Throws()
    {
        // Sensor Type = AirTemperature (0x01)
        // Size = 4 but only 2 value bytes provided
        // Level byte = 0b000_00_100 = 0x04
        byte[] data = [0x31, 0x05, 0x01, 0x04, 0x00, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_EmptyCommandParameters_Throws()
    {
        byte[] data = [0x31, 0x05];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => MultilevelSensorCommandClass.MultilevelSensorReportCommand.Parse(frame, NullLogger.Instance));
    }
}
