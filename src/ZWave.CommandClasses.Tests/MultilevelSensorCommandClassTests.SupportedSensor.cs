using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultilevelSensorCommandClassTests
{
    [TestMethod]
    public void SupportedSensorGetCommand_Create_HasCorrectFormat()
    {
        var command = MultilevelSensorCommandClass.MultilevelSensorSupportedSensorGetCommand.Create();

        Assert.AreEqual(CommandClassId.MultilevelSensor, MultilevelSensorCommandClass.MultilevelSensorSupportedSensorGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSensorCommand.SupportedSensorGet, MultilevelSensorCommandClass.MultilevelSensorSupportedSensorGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedSensorReport_Parse_SingleByte_AirTemperatureAndIlluminance()
    {
        // Bit 0 → AirTemperature (0x01), Bit 2 → Illuminance (0x03)
        // Bitmask = 0b0000_0101 = 0x05
        byte[] data = [0x31, 0x02, 0x05];
        CommandClassFrame frame = new(data);

        IReadOnlySet<MultilevelSensorType> supported =
            MultilevelSensorCommandClass.MultilevelSensorSupportedSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(MultilevelSensorType.AirTemperature, supported);
        Assert.Contains(MultilevelSensorType.Illuminance, supported);
    }

    [TestMethod]
    public void SupportedSensorReport_Parse_SingleByte_AllLowTypes()
    {
        // Bitmask = 0xFF → sensor types 1 through 8
        byte[] data = [0x31, 0x02, 0xFF];
        CommandClassFrame frame = new(data);

        IReadOnlySet<MultilevelSensorType> supported =
            MultilevelSensorCommandClass.MultilevelSensorSupportedSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(8, supported);
        Assert.Contains(MultilevelSensorType.AirTemperature, supported);  // 0x01
        Assert.Contains(MultilevelSensorType.GeneralPurpose, supported);  // 0x02
        Assert.Contains(MultilevelSensorType.Illuminance, supported);     // 0x03
        Assert.Contains(MultilevelSensorType.Power, supported);           // 0x04
    }

    [TestMethod]
    public void SupportedSensorReport_Parse_TwoBytes_IncludesHighTypes()
    {
        // Byte 0 = 0x01 → bit 0 → type 0x01 (AirTemperature)
        // Byte 1 = 0x01 → bit 0 → type 0x09 (8 + 0 + 1)
        byte[] data = [0x31, 0x02, 0x01, 0x01];
        CommandClassFrame frame = new(data);

        IReadOnlySet<MultilevelSensorType> supported =
            MultilevelSensorCommandClass.MultilevelSensorSupportedSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(MultilevelSensorType.AirTemperature, supported);         // type 0x01
        Assert.Contains((MultilevelSensorType)0x09, supported);                  // type 0x09
    }

    [TestMethod]
    public void SupportedSensorReport_Parse_AllZeros_EmptySet()
    {
        byte[] data = [0x31, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<MultilevelSensorType> supported =
            MultilevelSensorCommandClass.MultilevelSensorSupportedSensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SupportedSensorReport_Parse_TooShort_Throws()
    {
        // No bitmask bytes
        byte[] data = [0x31, 0x02];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => MultilevelSensorCommandClass.MultilevelSensorSupportedSensorReportCommand.Parse(frame, NullLogger.Instance));
    }
}
