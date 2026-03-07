using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class HumidityControlModeCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        HumidityControlModeCommandClass.HumidityControlModeGetCommand command =
            HumidityControlModeCommandClass.HumidityControlModeGetCommand.Create();

        Assert.AreEqual(CommandClassId.HumidityControlMode, HumidityControlModeCommandClass.HumidityControlModeGetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlModeCommand.Get, HumidityControlModeCommandClass.HumidityControlModeGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SetCommand_Create_Off()
    {
        HumidityControlModeCommandClass.HumidityControlModeSetCommand command =
            HumidityControlModeCommandClass.HumidityControlModeSetCommand.Create(HumidityControlMode.Off);

        Assert.AreEqual(CommandClassId.HumidityControlMode, HumidityControlModeCommandClass.HumidityControlModeSetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlModeCommand.Set, HumidityControlModeCommandClass.HumidityControlModeSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Humidify()
    {
        HumidityControlModeCommandClass.HumidityControlModeSetCommand command =
            HumidityControlModeCommandClass.HumidityControlModeSetCommand.Create(HumidityControlMode.Humidify);

        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Dehumidify()
    {
        HumidityControlModeCommandClass.HumidityControlModeSetCommand command =
            HumidityControlModeCommandClass.HumidityControlModeSetCommand.Create(HumidityControlMode.Dehumidify);

        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Auto()
    {
        HumidityControlModeCommandClass.HumidityControlModeSetCommand command =
            HumidityControlModeCommandClass.HumidityControlModeSetCommand.Create(HumidityControlMode.Auto);

        Assert.AreEqual(0x03, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_ReservedBitsClear()
    {
        // Ensure upper 4 bits are always zero per spec
        HumidityControlModeCommandClass.HumidityControlModeSetCommand command =
            HumidityControlModeCommandClass.HumidityControlModeSetCommand.Create(HumidityControlMode.Auto);

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0] & 0xF0);
    }

    [TestMethod]
    public void Report_Parse_Off()
    {
        // CC=0x6D, Cmd=0x03, Mode=0x00 (Off)
        byte[] data = [0x6D, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        HumidityControlModeReport report = HumidityControlModeCommandClass.HumidityControlModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlMode.Off, report.Mode);
    }

    [TestMethod]
    public void Report_Parse_Humidify()
    {
        byte[] data = [0x6D, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        HumidityControlModeReport report = HumidityControlModeCommandClass.HumidityControlModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlMode.Humidify, report.Mode);
    }

    [TestMethod]
    public void Report_Parse_Dehumidify()
    {
        byte[] data = [0x6D, 0x03, 0x02];
        CommandClassFrame frame = new(data);

        HumidityControlModeReport report = HumidityControlModeCommandClass.HumidityControlModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlMode.Dehumidify, report.Mode);
    }

    [TestMethod]
    public void Report_Parse_Auto()
    {
        byte[] data = [0x6D, 0x03, 0x03];
        CommandClassFrame frame = new(data);

        HumidityControlModeReport report = HumidityControlModeCommandClass.HumidityControlModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlMode.Auto, report.Mode);
    }

    [TestMethod]
    public void Report_Parse_ReservedBitsIgnored()
    {
        // Upper 4 bits are reserved and should be ignored
        byte[] data = [0x6D, 0x03, 0xF2];
        CommandClassFrame frame = new(data);

        HumidityControlModeReport report = HumidityControlModeCommandClass.HumidityControlModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlMode.Dehumidify, report.Mode);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        byte[] data = [0x6D, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlModeCommandClass.HumidityControlModeReportCommand.Parse(frame, NullLogger.Instance));
    }
}
