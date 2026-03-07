using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class BarrierOperatorCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_Open()
    {
        var command = BarrierOperatorCommandClass.BarrierOperatorSetCommand.Create(BarrierOperatorTargetValue.Open);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(CommandClassId.BarrierOperator, command.Frame.CommandClassId);
        Assert.AreEqual((byte)BarrierOperatorCommand.Set, command.Frame.CommandId);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Close()
    {
        var command = BarrierOperatorCommandClass.BarrierOperatorSetCommand.Create(BarrierOperatorTargetValue.Close);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create()
    {
        var command = BarrierOperatorCommandClass.BarrierOperatorGetCommand.Create();

        Assert.AreEqual(2, command.Frame.Data.Length);
        Assert.AreEqual(CommandClassId.BarrierOperator, command.Frame.CommandClassId);
        Assert.AreEqual((byte)BarrierOperatorCommand.Get, command.Frame.CommandId);
    }

    [TestMethod]
    public void ReportCommand_Parse_Closed()
    {
        byte[] data = [0x66, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.StateValue);
        Assert.AreEqual(BarrierOperatorState.Closed, report.State);
        Assert.IsNull(report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_Open()
    {
        byte[] data = [0x66, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFF, report.StateValue);
        Assert.AreEqual(BarrierOperatorState.Open, report.State);
        Assert.IsNull(report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_Opening()
    {
        byte[] data = [0x66, 0x03, 0xFE];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorState.Opening, report.State);
        Assert.IsNull(report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_Closing()
    {
        byte[] data = [0x66, 0x03, 0xFC];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorState.Closing, report.State);
        Assert.IsNull(report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_Stopped()
    {
        byte[] data = [0x66, 0x03, 0xFD];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorState.Stopped, report.State);
        Assert.IsNull(report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_StoppedAtPosition_50Percent()
    {
        byte[] data = [0x66, 0x03, 0x32];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorState.Stopped, report.State);
        Assert.AreEqual((byte)50, report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_StoppedAtPosition_1Percent()
    {
        byte[] data = [0x66, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorState.Stopped, report.State);
        Assert.AreEqual((byte)1, report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_StoppedAtPosition_99Percent()
    {
        byte[] data = [0x66, 0x03, 0x63];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorState.Stopped, report.State);
        Assert.AreEqual((byte)99, report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_ReservedValue()
    {
        byte[] data = [0x66, 0x03, 0xAA];
        CommandClassFrame frame = new(data);

        BarrierOperatorReport report = BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xAA, report.StateValue);
        Assert.IsNull(report.State);
        Assert.IsNull(report.Position);
    }

    [TestMethod]
    public void ReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x66, 0x03];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => BarrierOperatorCommandClass.BarrierOperatorReportCommand.Parse(frame, NullLogger.Instance));
    }
}
