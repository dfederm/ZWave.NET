using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class PowerlevelCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_HasCorrectFormat()
    {
        PowerlevelCommandClass.PowerlevelSetCommand command =
            PowerlevelCommandClass.PowerlevelSetCommand.Create(Powerlevel.Minus3dBm, 30);

        Assert.AreEqual(CommandClassId.Powerlevel, PowerlevelCommandClass.PowerlevelSetCommand.CommandClassId);
        Assert.AreEqual((byte)PowerlevelCommand.Set, PowerlevelCommandClass.PowerlevelSetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length); // CC + Cmd + PowerLevel + Timeout
        Assert.AreEqual(0x03, command.Frame.CommandParameters.Span[0]); // Minus3dBm
        Assert.AreEqual(30, command.Frame.CommandParameters.Span[1]); // Timeout
    }

    [TestMethod]
    public void SetCommand_Create_NormalPower()
    {
        PowerlevelCommandClass.PowerlevelSetCommand command =
            PowerlevelCommandClass.PowerlevelSetCommand.Create(Powerlevel.Normal, 0);

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]); // Normal
        Assert.AreEqual(0, command.Frame.CommandParameters.Span[1]); // Timeout
    }

    [TestMethod]
    public void SetCommand_Create_AllPowerlevels()
    {
        for (byte level = 0; level <= 9; level++)
        {
            PowerlevelCommandClass.PowerlevelSetCommand command =
                PowerlevelCommandClass.PowerlevelSetCommand.Create((Powerlevel)level, 10);

            Assert.AreEqual(level, command.Frame.CommandParameters.Span[0]);
        }
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        PowerlevelCommandClass.PowerlevelGetCommand command =
            PowerlevelCommandClass.PowerlevelGetCommand.Create();

        Assert.AreEqual(CommandClassId.Powerlevel, PowerlevelCommandClass.PowerlevelGetCommand.CommandClassId);
        Assert.AreEqual((byte)PowerlevelCommand.Get, PowerlevelCommandClass.PowerlevelGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void Report_Parse_NormalPower()
    {
        // CC=0x73, Cmd=0x03, PowerLevel=0x00 (Normal), Timeout=0x00
        byte[] data = [0x73, 0x03, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        PowerlevelReport report = PowerlevelCommandClass.PowerlevelReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(Powerlevel.Normal, report.Powerlevel);
        Assert.IsNull(report.TimeoutInSeconds);
    }

    [TestMethod]
    public void Report_Parse_NonNormalPower()
    {
        // CC=0x73, Cmd=0x03, PowerLevel=0x05 (Minus5dBm), Timeout=120
        byte[] data = [0x73, 0x03, 0x05, 120];
        CommandClassFrame frame = new(data);

        PowerlevelReport report = PowerlevelCommandClass.PowerlevelReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(Powerlevel.Minus5dBm, report.Powerlevel);
        Assert.IsNotNull(report.TimeoutInSeconds);
        Assert.AreEqual((byte)120, report.TimeoutInSeconds.Value);
    }

    [TestMethod]
    public void Report_Parse_Minus9dBm()
    {
        // CC=0x73, Cmd=0x03, PowerLevel=0x09 (Minus9dBm), Timeout=255
        byte[] data = [0x73, 0x03, 0x09, 0xFF];
        CommandClassFrame frame = new(data);

        PowerlevelReport report = PowerlevelCommandClass.PowerlevelReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(Powerlevel.Minus9dBm, report.Powerlevel);
        Assert.IsNotNull(report.TimeoutInSeconds);
        Assert.AreEqual((byte)255, report.TimeoutInSeconds.Value);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x73, Cmd=0x03, only 1 parameter byte (need 2)
        byte[] data = [0x73, 0x03, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => PowerlevelCommandClass.PowerlevelReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_NoParameters_Throws()
    {
        // CC=0x73, Cmd=0x03, no parameters
        byte[] data = [0x73, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => PowerlevelCommandClass.PowerlevelReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_ReservedPowerlevel_Preserved()
    {
        // CC=0x73, Cmd=0x03, PowerLevel=0x0A (reserved), Timeout=10
        byte[] data = [0x73, 0x03, 0x0A, 10];
        CommandClassFrame frame = new(data);

        PowerlevelReport report = PowerlevelCommandClass.PowerlevelReportCommand.Parse(frame, NullLogger.Instance);

        // Reserved value is preserved (forward compatibility)
        Assert.AreEqual((Powerlevel)0x0A, report.Powerlevel);
        Assert.IsNotNull(report.TimeoutInSeconds);
        Assert.AreEqual((byte)10, report.TimeoutInSeconds.Value);
    }
}
