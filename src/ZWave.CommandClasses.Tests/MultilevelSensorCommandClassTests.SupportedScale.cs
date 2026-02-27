using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultilevelSensorCommandClassTests
{
    [TestMethod]
    public void SupportedScaleGetCommand_Create_HasCorrectFormat()
    {
        var command = MultilevelSensorCommandClass.MultilevelSensorSupportedScaleGetCommand.Create(
            MultilevelSensorType.AirTemperature);

        Assert.AreEqual(CommandClassId.MultilevelSensor, MultilevelSensorCommandClass.MultilevelSensorSupportedScaleGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSensorCommand.SupportedScaleGet, MultilevelSensorCommandClass.MultilevelSensorSupportedScaleGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)MultilevelSensorType.AirTemperature, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SupportedScaleReport_Parse_CelsiusAndFahrenheit()
    {
        // Sensor Type = AirTemperature (0x01)
        // Scale Bit Mask = 0b0000_0011 → scales 0 (Celsius) and 1 (Fahrenheit)
        byte[] data = [0x31, 0x06, 0x01, 0x03];
        CommandClassFrame frame = new(data);

        (MultilevelSensorType sensorType, IReadOnlySet<MultilevelSensorScale> scales) =
            MultilevelSensorCommandClass.MultilevelSensorSupportedScaleReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.AirTemperature, sensorType);
        Assert.HasCount(2, scales);
        Assert.Contains(MultilevelSensorType.AirTemperature.GetScale(0), scales);
        Assert.Contains(MultilevelSensorType.AirTemperature.GetScale(1), scales);
    }

    [TestMethod]
    public void SupportedScaleReport_Parse_SingleScale()
    {
        // Sensor Type = Illuminance (0x03)
        // Scale Bit Mask = 0b0000_0010 → scale 1 (Lux) only
        byte[] data = [0x31, 0x06, 0x03, 0x02];
        CommandClassFrame frame = new(data);

        (MultilevelSensorType sensorType, IReadOnlySet<MultilevelSensorScale> scales) =
            MultilevelSensorCommandClass.MultilevelSensorSupportedScaleReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.Illuminance, sensorType);
        Assert.HasCount(1, scales);
        Assert.Contains(MultilevelSensorType.Illuminance.GetScale(1), scales);
    }

    [TestMethod]
    public void SupportedScaleReport_Parse_AllFourScales()
    {
        // Sensor Type = AirTemperature (0x01)
        // Scale Bit Mask = 0b0000_1111 → all 4 scales
        byte[] data = [0x31, 0x06, 0x01, 0x0F];
        CommandClassFrame frame = new(data);

        (MultilevelSensorType sensorType, IReadOnlySet<MultilevelSensorScale> scales) =
            MultilevelSensorCommandClass.MultilevelSensorSupportedScaleReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.AirTemperature, sensorType);
        Assert.HasCount(4, scales);
    }

    [TestMethod]
    public void SupportedScaleReport_Parse_NoScales()
    {
        // Sensor Type = AirTemperature (0x01)
        // Scale Bit Mask = 0x00 → no scales (unsupported sensor type per spec)
        byte[] data = [0x31, 0x06, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        (MultilevelSensorType sensorType, IReadOnlySet<MultilevelSensorScale> scales) =
            MultilevelSensorCommandClass.MultilevelSensorSupportedScaleReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.AirTemperature, sensorType);
        Assert.IsEmpty(scales);
    }

    [TestMethod]
    public void SupportedScaleReport_Parse_ReservedBitsIgnored()
    {
        // Sensor Type = AirTemperature (0x01)
        // Byte = 0xF3 → reserved bits 7-4 set, scale bits 0 and 1 set
        // Should only parse lower 4 bits: 0b0011 → scales 0 and 1
        byte[] data = [0x31, 0x06, 0x01, 0xF3];
        CommandClassFrame frame = new(data);

        (MultilevelSensorType sensorType, IReadOnlySet<MultilevelSensorScale> scales) =
            MultilevelSensorCommandClass.MultilevelSensorSupportedScaleReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSensorType.AirTemperature, sensorType);
        Assert.HasCount(2, scales);
    }

    [TestMethod]
    public void SupportedScaleReport_Parse_TooShort_Throws()
    {
        // Only sensor type, no scale bitmask
        byte[] data = [0x31, 0x06, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => MultilevelSensorCommandClass.MultilevelSensorSupportedScaleReportCommand.Parse(frame, NullLogger.Instance));
    }
}
