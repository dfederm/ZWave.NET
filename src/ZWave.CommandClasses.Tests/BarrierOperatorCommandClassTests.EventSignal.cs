using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class BarrierOperatorCommandClassTests
{
    [TestMethod]
    public void EventSignalSetCommand_Create_On()
    {
        var command = BarrierOperatorCommandClass.EventSignalSetCommand.Create(
            BarrierOperatorSignalingSubsystemType.AudibleNotification,
            on: true);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(CommandClassId.BarrierOperator, command.Frame.CommandClassId);
        Assert.AreEqual((byte)BarrierOperatorCommand.EventSignalSet, command.Frame.CommandId);
        Assert.AreEqual((byte)BarrierOperatorSignalingSubsystemType.AudibleNotification, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void EventSignalSetCommand_Create_Off()
    {
        var command = BarrierOperatorCommandClass.EventSignalSetCommand.Create(
            BarrierOperatorSignalingSubsystemType.VisualNotification,
            on: false);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)BarrierOperatorSignalingSubsystemType.VisualNotification, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void EventSignalingGetCommand_Create()
    {
        var command = BarrierOperatorCommandClass.EventSignalingGetCommand.Create(
            BarrierOperatorSignalingSubsystemType.AudibleNotification);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(CommandClassId.BarrierOperator, command.Frame.CommandClassId);
        Assert.AreEqual((byte)BarrierOperatorCommand.EventSignalingGet, command.Frame.CommandId);
        Assert.AreEqual((byte)BarrierOperatorSignalingSubsystemType.AudibleNotification, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void EventSignalingReportCommand_Parse_On()
    {
        byte[] data = [0x66, 0x08, 0x01, 0xFF];
        CommandClassFrame frame = new(data);

        BarrierOperatorEventSignalReport report =
            BarrierOperatorCommandClass.EventSignalingReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorSignalingSubsystemType.AudibleNotification, report.SubsystemType);
        Assert.AreEqual((byte)0xFF, report.SubsystemState);
        Assert.IsTrue(report.IsOn);
    }

    [TestMethod]
    public void EventSignalingReportCommand_Parse_Off()
    {
        byte[] data = [0x66, 0x08, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        BarrierOperatorEventSignalReport report =
            BarrierOperatorCommandClass.EventSignalingReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorSignalingSubsystemType.VisualNotification, report.SubsystemType);
        Assert.AreEqual((byte)0x00, report.SubsystemState);
        Assert.IsFalse(report.IsOn);
    }

    [TestMethod]
    public void EventSignalingReportCommand_Parse_ReservedState()
    {
        byte[] data = [0x66, 0x08, 0x01, 0x55];
        CommandClassFrame frame = new(data);

        BarrierOperatorEventSignalReport report =
            BarrierOperatorCommandClass.EventSignalingReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BarrierOperatorSignalingSubsystemType.AudibleNotification, report.SubsystemType);
        Assert.AreEqual((byte)0x55, report.SubsystemState);
        Assert.IsNull(report.IsOn);
    }

    [TestMethod]
    public void EventSignalingReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x66, 0x08, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => BarrierOperatorCommandClass.EventSignalingReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void EventSignalingReportCommand_Parse_Empty_Throws()
    {
        byte[] data = [0x66, 0x08];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => BarrierOperatorCommandClass.EventSignalingReportCommand.Parse(frame, NullLogger.Instance));
    }
}
