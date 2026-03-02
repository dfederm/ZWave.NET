using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class AssociationGroupInformationCommandClassTests
{
    [TestMethod]
    public void CommandListGetCommand_Create_HasCorrectFormat()
    {
        AssociationGroupInformationCommandClass.CommandListGetCommand command =
            AssociationGroupInformationCommandClass.CommandListGetCommand.Create(5);

        Assert.AreEqual(
            CommandClassId.AssociationGroupInformation,
            AssociationGroupInformationCommandClass.CommandListGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)AssociationGroupInformationCommand.CommandListGet,
            AssociationGroupInformationCommandClass.CommandListGetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length); // CC + Cmd + Flags + GroupId
        // Flags: Allow Cache = 0b1000_0000
        Assert.AreEqual((byte)0b1000_0000, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)5, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void CommandListReport_Parse_NormalCommandClasses()
    {
        // CC=0x59, Cmd=0x06, GroupId=1, ListLength=4
        // Command 1: Basic (0x20) Set (0x01)
        // Command 2: BinarySwitch (0x25) Set (0x01)
        byte[] data = [0x59, 0x06, 0x01, 0x04, 0x20, 0x01, 0x25, 0x01];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, IReadOnlyList<AssociationGroupCommand> commands) =
            AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.HasCount(2, commands);
        Assert.AreEqual((ushort)CommandClassId.Basic, commands[0].CommandClassId);
        Assert.AreEqual((byte)0x01, commands[0].CommandId);
        Assert.AreEqual((ushort)CommandClassId.BinarySwitch, commands[1].CommandClassId);
        Assert.AreEqual((byte)0x01, commands[1].CommandId);
    }

    [TestMethod]
    public void CommandListReport_Parse_ExtendedCommandClass()
    {
        // CC=0x59, Cmd=0x06, GroupId=2, ListLength=5
        // Command 1: Extended CC (0xF1, 0x00) Cmd (0x01) - 3 bytes
        // Command 2: Basic (0x20) Set (0x01) - 2 bytes
        byte[] data = [0x59, 0x06, 0x02, 0x05, 0xF1, 0x00, 0x01, 0x20, 0x01];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, IReadOnlyList<AssociationGroupCommand> commands) =
            AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, groupingIdentifier);
        Assert.HasCount(2, commands);
        Assert.AreEqual((ushort)0xF100, commands[0].CommandClassId);
        Assert.AreEqual((byte)0x01, commands[0].CommandId);
        Assert.AreEqual((ushort)CommandClassId.Basic, commands[1].CommandClassId);
        Assert.AreEqual((byte)0x01, commands[1].CommandId);
    }

    [TestMethod]
    public void CommandListReport_Parse_EmptyCommandList()
    {
        // CC=0x59, Cmd=0x06, GroupId=1, ListLength=0
        byte[] data = [0x59, 0x06, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, IReadOnlyList<AssociationGroupCommand> commands) =
            AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.IsEmpty(commands);
    }

    [TestMethod]
    public void CommandListReport_Parse_SingleCommand()
    {
        // CC=0x59, Cmd=0x06, GroupId=1, ListLength=2
        // Command: MultilevelSwitch (0x26) Set (0x01)
        byte[] data = [0x59, 0x06, 0x01, 0x02, 0x26, 0x01];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, IReadOnlyList<AssociationGroupCommand> commands) =
            AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.HasCount(1, commands);
        Assert.AreEqual((ushort)0x26, commands[0].CommandClassId);
        Assert.AreEqual((byte)0x01, commands[0].CommandId);
    }

    [TestMethod]
    public void CommandListReport_Parse_LifelineGroup()
    {
        // Lifeline group typically has multiple report commands:
        // CC=0x59, Cmd=0x06, GroupId=1, ListLength=8
        // Notification Report (0x71, 0x05), Battery Report (0x80, 0x03),
        // Device Reset (0x5A, 0x01), Sensor Report (0x31, 0x05)
        byte[] data = [0x59, 0x06, 0x01, 0x08, 0x71, 0x05, 0x80, 0x03, 0x5A, 0x01, 0x31, 0x05];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, IReadOnlyList<AssociationGroupCommand> commands) =
            AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.HasCount(4, commands);
        Assert.AreEqual((ushort)CommandClassId.Notification, commands[0].CommandClassId);
        Assert.AreEqual((byte)0x05, commands[0].CommandId);
        Assert.AreEqual((ushort)CommandClassId.Battery, commands[1].CommandClassId);
        Assert.AreEqual((byte)0x03, commands[1].CommandId);
    }

    [TestMethod]
    public void CommandListReport_Parse_TooShort_Throws()
    {
        // CC=0x59, Cmd=0x06, only GroupId (need GroupId + ListLength)
        byte[] data = [0x59, 0x06, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CommandListReport_Parse_TruncatedList_Throws()
    {
        // CC=0x59, Cmd=0x06, GroupId=1, ListLength=6, but only 2 bytes of data
        byte[] data = [0x59, 0x06, 0x01, 0x06, 0x20, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CommandListReport_Parse_MixedNormalAndExtended()
    {
        // CC=0x59, Cmd=0x06, GroupId=1, ListLength=7
        // Normal: Basic (0x20) Set (0x01) = 2 bytes
        // Extended: (0xF2, 0x05) Cmd (0x03) = 3 bytes
        // Normal: Notification (0x71) Report (0x05) = 2 bytes
        byte[] data = [0x59, 0x06, 0x01, 0x07, 0x20, 0x01, 0xF2, 0x05, 0x03, 0x71, 0x05];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, IReadOnlyList<AssociationGroupCommand> commands) =
            AssociationGroupInformationCommandClass.CommandListReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.HasCount(3, commands);

        Assert.AreEqual((ushort)CommandClassId.Basic, commands[0].CommandClassId);
        Assert.AreEqual((byte)0x01, commands[0].CommandId);

        Assert.AreEqual((ushort)0xF205, commands[1].CommandClassId);
        Assert.AreEqual((byte)0x03, commands[1].CommandId);

        Assert.AreEqual((ushort)CommandClassId.Notification, commands[2].CommandClassId);
        Assert.AreEqual((byte)0x05, commands[2].CommandId);
    }
}
