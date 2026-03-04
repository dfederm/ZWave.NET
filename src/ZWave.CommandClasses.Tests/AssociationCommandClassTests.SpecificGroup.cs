using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class AssociationCommandClassTests
{
    [TestMethod]
    public void SpecificGroupGetCommand_Create_HasCorrectFormat()
    {
        AssociationCommandClass.AssociationSpecificGroupGetCommand command =
            AssociationCommandClass.AssociationSpecificGroupGetCommand.Create();

        Assert.AreEqual(
            CommandClassId.Association,
            AssociationCommandClass.AssociationSpecificGroupGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)AssociationCommand.SpecificGroupGet,
            AssociationCommandClass.AssociationSpecificGroupGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void SpecificGroupReport_Parse_ValidGroup()
    {
        // CC=0x85, Cmd=0x0C, Group=3
        byte[] data = [0x85, 0x0C, 0x03];
        CommandClassFrame frame = new(data);

        byte group = AssociationCommandClass.AssociationSpecificGroupReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)3, group);
    }

    [TestMethod]
    public void SpecificGroupReport_Parse_NotSupported()
    {
        // CC=0x85, Cmd=0x0C, Group=0 (not supported or no recent button)
        byte[] data = [0x85, 0x0C, 0x00];
        CommandClassFrame frame = new(data);

        byte group = AssociationCommandClass.AssociationSpecificGroupReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, group);
    }

    [TestMethod]
    public void SpecificGroupReport_Parse_MaxGroup()
    {
        // CC=0x85, Cmd=0x0C, Group=255
        byte[] data = [0x85, 0x0C, 0xFF];
        CommandClassFrame frame = new(data);

        byte group = AssociationCommandClass.AssociationSpecificGroupReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)255, group);
    }

    [TestMethod]
    public void SpecificGroupReport_Parse_TooShort_Throws()
    {
        // CC=0x85, Cmd=0x0C, no parameters
        byte[] data = [0x85, 0x0C];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationCommandClass.AssociationSpecificGroupReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SpecificGroupReport_Create_ParseRoundTrip()
    {
        AssociationCommandClass.AssociationSpecificGroupReportCommand report =
            AssociationCommandClass.AssociationSpecificGroupReportCommand.Create(5);

        byte group = AssociationCommandClass.AssociationSpecificGroupReportCommand.Parse(
            report.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)5, group);
    }

    [TestMethod]
    public void SpecificGroupReport_Create_NotSupported()
    {
        AssociationCommandClass.AssociationSpecificGroupReportCommand report =
            AssociationCommandClass.AssociationSpecificGroupReportCommand.Create(0);

        byte group = AssociationCommandClass.AssociationSpecificGroupReportCommand.Parse(
            report.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, group);
    }
}
