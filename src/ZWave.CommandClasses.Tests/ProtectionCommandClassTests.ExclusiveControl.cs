using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ProtectionCommandClassTests
{
    [TestMethod]
    public void ExclusiveControlSetCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionExclusiveControlSetCommand command =
            ProtectionCommandClass.ProtectionExclusiveControlSetCommand.Create(5);

        Assert.AreEqual(CommandClassId.Protection, ProtectionCommandClass.ProtectionExclusiveControlSetCommand.CommandClassId);
        Assert.AreEqual((byte)ProtectionCommand.ExclusiveControlSet, ProtectionCommandClass.ProtectionExclusiveControlSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + NodeID
        Assert.AreEqual(5, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ExclusiveControlSetCommand_Create_ResetWithZero()
    {
        ProtectionCommandClass.ProtectionExclusiveControlSetCommand command =
            ProtectionCommandClass.ProtectionExclusiveControlSetCommand.Create(0);

        Assert.AreEqual(0, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ExclusiveControlGetCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionExclusiveControlGetCommand command =
            ProtectionCommandClass.ProtectionExclusiveControlGetCommand.Create();

        Assert.AreEqual(CommandClassId.Protection, ProtectionCommandClass.ProtectionExclusiveControlGetCommand.CommandClassId);
        Assert.AreEqual((byte)ProtectionCommand.ExclusiveControlGet, ProtectionCommandClass.ProtectionExclusiveControlGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void ExclusiveControlReportCommand_Parse_NodeId()
    {
        byte[] data = [0x75, 0x08, 0x05];
        CommandClassFrame frame = new(data);

        byte nodeId = ProtectionCommandClass.ProtectionExclusiveControlReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)5, nodeId);
    }

    [TestMethod]
    public void ExclusiveControlReportCommand_Parse_NoExclusiveControl()
    {
        byte[] data = [0x75, 0x08, 0x00];
        CommandClassFrame frame = new(data);

        byte nodeId = ProtectionCommandClass.ProtectionExclusiveControlReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, nodeId);
    }

    [TestMethod]
    public void ExclusiveControlReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x75, 0x08];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ProtectionCommandClass.ProtectionExclusiveControlReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ExclusiveControlReportCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionExclusiveControlReportCommand command =
            ProtectionCommandClass.ProtectionExclusiveControlReportCommand.Create(10);

        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(10, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ExclusiveControlReportCommand_RoundTrip()
    {
        ProtectionCommandClass.ProtectionExclusiveControlReportCommand command =
            ProtectionCommandClass.ProtectionExclusiveControlReportCommand.Create(42);

        byte nodeId = ProtectionCommandClass.ProtectionExclusiveControlReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)42, nodeId);
    }
}
