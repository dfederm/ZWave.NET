using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class AssociationCommandClassTests
{
    [TestMethod]
    public void SupportedGroupingsGetCommand_Create_HasCorrectFormat()
    {
        AssociationCommandClass.AssociationSupportedGroupingsGetCommand command =
            AssociationCommandClass.AssociationSupportedGroupingsGetCommand.Create();

        Assert.AreEqual(
            CommandClassId.Association,
            AssociationCommandClass.AssociationSupportedGroupingsGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)AssociationCommand.SupportedGroupingsGet,
            AssociationCommandClass.AssociationSupportedGroupingsGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_ValidFrame()
    {
        // CC=0x85, Cmd=0x06, SupportedGroupings=5
        byte[] data = [0x85, 0x06, 0x05];
        CommandClassFrame frame = new(data);

        byte groupings = AssociationCommandClass.AssociationSupportedGroupingsReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)5, groupings);
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_SingleGroup()
    {
        // CC=0x85, Cmd=0x06, SupportedGroupings=1
        byte[] data = [0x85, 0x06, 0x01];
        CommandClassFrame frame = new(data);

        byte groupings = AssociationCommandClass.AssociationSupportedGroupingsReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupings);
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_MaxGroups()
    {
        // CC=0x85, Cmd=0x06, SupportedGroupings=255
        byte[] data = [0x85, 0x06, 0xFF];
        CommandClassFrame frame = new(data);

        byte groupings = AssociationCommandClass.AssociationSupportedGroupingsReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)255, groupings);
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_TooShort_Throws()
    {
        // CC=0x85, Cmd=0x06, no parameters
        byte[] data = [0x85, 0x06];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationCommandClass.AssociationSupportedGroupingsReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedGroupingsReport_Create_ParseRoundTrip()
    {
        AssociationCommandClass.AssociationSupportedGroupingsReportCommand report =
            AssociationCommandClass.AssociationSupportedGroupingsReportCommand.Create(3);

        byte groupings = AssociationCommandClass.AssociationSupportedGroupingsReportCommand.Parse(
            report.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)3, groupings);
    }
}
