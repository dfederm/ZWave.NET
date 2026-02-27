using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class BinarySensorCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_Version1_NoSensorType_HasCorrectFormat()
    {
        BinarySensorCommandClass.BinarySensorGetCommand command =
            BinarySensorCommandClass.BinarySensorGetCommand.Create(version: 1, sensorType: null);

        Assert.AreEqual(CommandClassId.BinarySensor, BinarySensorCommandClass.BinarySensorGetCommand.CommandClassId);
        Assert.AreEqual((byte)BinarySensorCommand.Get, BinarySensorCommandClass.BinarySensorGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void GetCommand_Create_Version1_SensorTypeIgnored()
    {
        // V1 does not support sensor type parameter, even if one is provided
        BinarySensorCommandClass.BinarySensorGetCommand command =
            BinarySensorCommandClass.BinarySensorGetCommand.Create(version: 1, sensorType: BinarySensorType.Smoke);

        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void GetCommand_Create_Version2_WithSensorType_HasCorrectFormat()
    {
        BinarySensorCommandClass.BinarySensorGetCommand command =
            BinarySensorCommandClass.BinarySensorGetCommand.Create(version: 2, sensorType: BinarySensorType.Motion);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)BinarySensorType.Motion, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version2_FirstSupported()
    {
        BinarySensorCommandClass.BinarySensorGetCommand command =
            BinarySensorCommandClass.BinarySensorGetCommand.Create(version: 2, sensorType: BinarySensorType.FirstSupported);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)BinarySensorType.FirstSupported, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version2_NullSensorType_NoParameter()
    {
        BinarySensorCommandClass.BinarySensorGetCommand command =
            BinarySensorCommandClass.BinarySensorGetCommand.Create(version: 2, sensorType: null);

        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Version1_EventDetected()
    {
        // CC=0x30, Cmd=0x03, SensorValue=0xFF
        byte[] data = [0x30, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        BinarySensorReport report = BinarySensorCommandClass.BinarySensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.Value);
        Assert.IsNull(report.SensorType);
    }

    [TestMethod]
    public void Report_Parse_Version1_Idle()
    {
        // CC=0x30, Cmd=0x03, SensorValue=0x00
        byte[] data = [0x30, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        BinarySensorReport report = BinarySensorCommandClass.BinarySensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.Value);
        Assert.IsNull(report.SensorType);
    }

    [TestMethod]
    public void Report_Parse_Version2_EventDetected_WithSensorType()
    {
        // CC=0x30, Cmd=0x03, SensorValue=0xFF, SensorType=0x0C (Motion)
        byte[] data = [0x30, 0x03, 0xFF, 0x0C];
        CommandClassFrame frame = new(data);

        BinarySensorReport report = BinarySensorCommandClass.BinarySensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.Value);
        Assert.AreEqual(BinarySensorType.Motion, report.SensorType);
    }

    [TestMethod]
    public void Report_Parse_Version2_Idle_WithSensorType()
    {
        // CC=0x30, Cmd=0x03, SensorValue=0x00, SensorType=0x0A (Door/Window)
        byte[] data = [0x30, 0x03, 0x00, 0x0A];
        CommandClassFrame frame = new(data);

        BinarySensorReport report = BinarySensorCommandClass.BinarySensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.Value);
        Assert.AreEqual(BinarySensorType.DoorWindow, report.SensorType);
    }

    [TestMethod]
    public void Report_Parse_NonStandardSensorValue_TreatedAsIdle()
    {
        // Spec only defines 0x00 (idle) and 0xFF (detected).
        // Any other value should be treated as not-detected per forward compatibility.
        byte[] data = [0x30, 0x03, 0x7F, 0x01];
        CommandClassFrame frame = new(data);

        BinarySensorReport report = BinarySensorCommandClass.BinarySensorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.Value);
        Assert.AreEqual(BinarySensorType.GeneralPurpose, report.SensorType);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x30, Cmd=0x03, no parameters
        byte[] data = [0x30, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BinarySensorCommandClass.BinarySensorReportCommand.Parse(frame, NullLogger.Instance));
    }
}
