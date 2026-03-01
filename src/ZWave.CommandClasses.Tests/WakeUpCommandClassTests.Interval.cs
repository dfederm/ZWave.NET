using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class WakeUpCommandClassTests
{
    [TestMethod]
    public void IntervalGetCommand_Create_HasCorrectFormat()
    {
        WakeUpCommandClass.WakeUpIntervalGetCommand command = WakeUpCommandClass.WakeUpIntervalGetCommand.Create();

        Assert.AreEqual(CommandClassId.WakeUp, WakeUpCommandClass.WakeUpIntervalGetCommand.CommandClassId);
        Assert.AreEqual((byte)WakeUpCommand.IntervalGet, WakeUpCommandClass.WakeUpIntervalGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void IntervalSetCommand_Create_HasCorrectFormat()
    {
        // 300 seconds = 0x00012C, NodeID = 1
        WakeUpCommandClass.WakeUpIntervalSetCommand command =
            WakeUpCommandClass.WakeUpIntervalSetCommand.Create(300, 1);

        Assert.AreEqual(CommandClassId.WakeUp, WakeUpCommandClass.WakeUpIntervalSetCommand.CommandClassId);
        Assert.AreEqual((byte)WakeUpCommand.IntervalSet, WakeUpCommandClass.WakeUpIntervalSetCommand.CommandId);
        Assert.AreEqual(6, command.Frame.Data.Length); // CC + Cmd + 3 bytes seconds + 1 byte NodeID

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 300 = 0x00012C → bytes: 0x00, 0x01, 0x2C
        Assert.AreEqual((byte)0x00, parameters[0]);
        Assert.AreEqual((byte)0x01, parameters[1]);
        Assert.AreEqual((byte)0x2C, parameters[2]);
        Assert.AreEqual((byte)1, parameters[3]);
    }

    [TestMethod]
    public void IntervalSetCommand_Create_MaxValue()
    {
        // Maximum 24-bit value: 16777215 = 0xFFFFFF
        WakeUpCommandClass.WakeUpIntervalSetCommand command =
            WakeUpCommandClass.WakeUpIntervalSetCommand.Create(16777215, 0xFF);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0xFF, parameters[0]);
        Assert.AreEqual((byte)0xFF, parameters[1]);
        Assert.AreEqual((byte)0xFF, parameters[2]);
        Assert.AreEqual((byte)0xFF, parameters[3]);
    }

    [TestMethod]
    public void IntervalSetCommand_Create_ZeroInterval()
    {
        // 0 = event-based wake up
        WakeUpCommandClass.WakeUpIntervalSetCommand command =
            WakeUpCommandClass.WakeUpIntervalSetCommand.Create(0, 5);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0x00, parameters[0]);
        Assert.AreEqual((byte)0x00, parameters[1]);
        Assert.AreEqual((byte)0x00, parameters[2]);
        Assert.AreEqual((byte)5, parameters[3]);
    }

    [TestMethod]
    public void IntervalSetCommand_Create_ExceedsMaxValue_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => WakeUpCommandClass.WakeUpIntervalSetCommand.Create(16777216, 1));
    }

    [TestMethod]
    public void IntervalReport_Parse_ValidFrame()
    {
        // CC=0x84, Cmd=0x06, Seconds=0x000258 (600), NodeID=0x01
        byte[] data = [0x84, 0x06, 0x00, 0x02, 0x58, 0x01];
        CommandClassFrame frame = new(data);

        WakeUpInterval report = WakeUpCommandClass.WakeUpIntervalReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(600u, report.WakeUpIntervalInSeconds);
        Assert.AreEqual((byte)1, report.WakeUpDestinationNodeId);
    }

    [TestMethod]
    public void IntervalReport_Parse_ZeroInterval()
    {
        // Seconds=0x000000 (event-based), NodeID=0x05
        byte[] data = [0x84, 0x06, 0x00, 0x00, 0x00, 0x05];
        CommandClassFrame frame = new(data);

        WakeUpInterval report = WakeUpCommandClass.WakeUpIntervalReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(0u, report.WakeUpIntervalInSeconds);
        Assert.AreEqual((byte)5, report.WakeUpDestinationNodeId);
    }

    [TestMethod]
    public void IntervalReport_Parse_MaxValues()
    {
        // Seconds=0xFFFFFF (max), NodeID=0xFF
        byte[] data = [0x84, 0x06, 0xFF, 0xFF, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        WakeUpInterval report = WakeUpCommandClass.WakeUpIntervalReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(16777215u, report.WakeUpIntervalInSeconds);
        Assert.AreEqual((byte)0xFF, report.WakeUpDestinationNodeId);
    }

    [TestMethod]
    public void IntervalReport_Parse_LargeInterval()
    {
        // Seconds=0x015180 (86400 = 24 hours), NodeID=0x01
        byte[] data = [0x84, 0x06, 0x01, 0x51, 0x80, 0x01];
        CommandClassFrame frame = new(data);

        WakeUpInterval report = WakeUpCommandClass.WakeUpIntervalReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(86400u, report.WakeUpIntervalInSeconds);
        Assert.AreEqual((byte)1, report.WakeUpDestinationNodeId);
    }

    [TestMethod]
    public void IntervalReport_Parse_TooShort_Throws()
    {
        // Only 3 parameter bytes, need 4
        byte[] data = [0x84, 0x06, 0x00, 0x01, 0x2C];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => WakeUpCommandClass.WakeUpIntervalReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void NoMoreInformationCommand_Create_HasCorrectFormat()
    {
        WakeUpCommandClass.WakeUpNoMoreInformationCommand command =
            WakeUpCommandClass.WakeUpNoMoreInformationCommand.Create();

        Assert.AreEqual(CommandClassId.WakeUp, WakeUpCommandClass.WakeUpNoMoreInformationCommand.CommandClassId);
        Assert.AreEqual((byte)WakeUpCommand.NoMoreInformation, WakeUpCommandClass.WakeUpNoMoreInformationCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void NotificationCommand_HasCorrectIds()
    {
        Assert.AreEqual(CommandClassId.WakeUp, WakeUpCommandClass.WakeUpNotificationCommand.CommandClassId);
        Assert.AreEqual((byte)WakeUpCommand.Notification, WakeUpCommandClass.WakeUpNotificationCommand.CommandId);
    }
}
