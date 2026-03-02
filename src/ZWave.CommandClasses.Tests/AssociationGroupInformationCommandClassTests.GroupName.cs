using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class AssociationGroupInformationCommandClassTests
{
    [TestMethod]
    public void GroupNameGetCommand_Create_HasCorrectFormat()
    {
        AssociationGroupInformationCommandClass.GroupNameGetCommand command =
            AssociationGroupInformationCommandClass.GroupNameGetCommand.Create(3);

        Assert.AreEqual(
            CommandClassId.AssociationGroupInformation,
            AssociationGroupInformationCommandClass.GroupNameGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)AssociationGroupInformationCommand.GroupNameGet,
            AssociationGroupInformationCommandClass.GroupNameGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + GroupId
        Assert.AreEqual((byte)3, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GroupNameReport_Parse_ValidName()
    {
        // CC=0x59, Cmd=0x02, GroupId=1, NameLength=8, "Lifeline"
        byte[] data = [0x59, 0x02, 0x01, 0x08, 0x4C, 0x69, 0x66, 0x65, 0x6C, 0x69, 0x6E, 0x65];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, string name) =
            AssociationGroupInformationCommandClass.GroupNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.AreEqual("Lifeline", name);
    }

    [TestMethod]
    public void GroupNameReport_Parse_EmptyName()
    {
        // CC=0x59, Cmd=0x02, GroupId=2, NameLength=0
        byte[] data = [0x59, 0x02, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, string name) =
            AssociationGroupInformationCommandClass.GroupNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, groupingIdentifier);
        Assert.AreEqual(string.Empty, name);
    }

    [TestMethod]
    public void GroupNameReport_Parse_MaxLengthName()
    {
        // CC=0x59, Cmd=0x02, GroupId=1, NameLength=42, then 42 ASCII 'A' characters
        byte[] data = new byte[4 + 42];
        data[0] = 0x59;
        data[1] = 0x02;
        data[2] = 0x01;
        data[3] = 42;
        for (int i = 0; i < 42; i++)
        {
            data[4 + i] = 0x41; // 'A'
        }

        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, string name) =
            AssociationGroupInformationCommandClass.GroupNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.AreEqual(new string('A', 42), name);
    }

    [TestMethod]
    public void GroupNameReport_Parse_Utf8Characters()
    {
        // CC=0x59, Cmd=0x02, GroupId=1, NameLength=6, "Ménage" (UTF-8: 4D C3 A9 6E 61 67 65)
        // Actually "Ménage" is 7 bytes in UTF-8. Let's use a simpler example: "café" = 63 61 66 C3 A9 = 5 bytes
        byte[] data = [0x59, 0x02, 0x01, 0x05, 0x63, 0x61, 0x66, 0xC3, 0xA9];
        CommandClassFrame frame = new(data);

        (byte groupingIdentifier, string name) =
            AssociationGroupInformationCommandClass.GroupNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupingIdentifier);
        Assert.AreEqual("café", name);
    }

    [TestMethod]
    public void GroupNameReport_Parse_TooShort_Throws()
    {
        // CC=0x59, Cmd=0x02, only 1 parameter byte (need at least 2)
        byte[] data = [0x59, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationGroupInformationCommandClass.GroupNameReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void GroupNameReport_Parse_TruncatedName_Throws()
    {
        // CC=0x59, Cmd=0x02, GroupId=1, NameLength=10, but only 3 name bytes
        byte[] data = [0x59, 0x02, 0x01, 0x0A, 0x41, 0x42, 0x43];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationGroupInformationCommandClass.GroupNameReportCommand.Parse(
                frame, NullLogger.Instance));
    }
}
