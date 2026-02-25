using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class BasicCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_HasCorrectFormat()
    {
        GenericValue value = new GenericValue(50);
        BasicCommandClass.BasicSetCommand command = BasicCommandClass.BasicSetCommand.Create(value);

        Assert.AreEqual(CommandClassId.Basic, BasicCommandClass.BasicSetCommand.CommandClassId);
        Assert.AreEqual((byte)BasicCommand.Set, BasicCommandClass.BasicSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(50, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_OnValue()
    {
        GenericValue value = new GenericValue(true);
        BasicCommandClass.BasicSetCommand command = BasicCommandClass.BasicSetCommand.Create(value);

        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_OffValue()
    {
        GenericValue value = new GenericValue(false);
        BasicCommandClass.BasicSetCommand command = BasicCommandClass.BasicSetCommand.Create(value);

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        BasicCommandClass.BasicGetCommand command = BasicCommandClass.BasicGetCommand.Create();

        Assert.AreEqual(CommandClassId.Basic, BasicCommandClass.BasicGetCommand.CommandClassId);
        Assert.AreEqual((byte)BasicCommand.Get, BasicCommandClass.BasicGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Version1_CurrentValueOnly()
    {
        // CC=0x20, Cmd=0x03, CurrentValue=50
        byte[] data = [0x20, 0x03, 50];
        CommandClassFrame frame = new(data);

        BasicReport report = BasicCommandClass.BasicReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)50, report.CurrentValue.Value);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version2_WithTargetAndDuration()
    {
        // CC=0x20, Cmd=0x03, CurrentValue=0x00, TargetValue=0xFF, Duration=0x05 (5 seconds)
        byte[] data = [0x20, 0x03, 0x00, 0xFF, 0x05];
        CommandClassFrame frame = new(data);

        BasicReport report = BasicCommandClass.BasicReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.CurrentValue.Value);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0xFF, report.TargetValue.Value.Value);
        Assert.IsNotNull(report.Duration);
        Assert.AreEqual((byte)0x05, report.Duration.Value.Value);
    }

    [TestMethod]
    public void Report_Parse_OnState()
    {
        // CC=0x20, Cmd=0x03, CurrentValue=0xFF (ON)
        byte[] data = [0x20, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        BasicReport report = BasicCommandClass.BasicReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(100, report.CurrentValue.Level);
        Assert.IsTrue(report.CurrentValue.State);
    }

    [TestMethod]
    public void Report_Parse_OffState()
    {
        // CC=0x20, Cmd=0x03, CurrentValue=0x00 (OFF)
        byte[] data = [0x20, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        BasicReport report = BasicCommandClass.BasicReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(0, report.CurrentValue.Level);
        Assert.IsFalse(report.CurrentValue.State);
    }

    [TestMethod]
    public void Report_Parse_UnknownValue()
    {
        // CC=0x20, Cmd=0x03, CurrentValue=0xFE (Unknown)
        byte[] data = [0x20, 0x03, 0xFE];
        CommandClassFrame frame = new(data);

        BasicReport report = BasicCommandClass.BasicReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNull(report.CurrentValue.Level);
        Assert.IsNull(report.CurrentValue.State);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x20, Cmd=0x03, no parameters
        byte[] data = [0x20, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BasicCommandClass.BasicReportCommand.Parse(frame, NullLogger.Instance));
    }
}
