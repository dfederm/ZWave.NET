using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelAssociationCommandClassTests
{
    [TestMethod]
    public void SupportedGroupingsGetCommand_Create_HasCorrectFormat()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsGetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsGetCommand.Create();

        Assert.AreEqual(
            CommandClassId.MultiChannelAssociation,
            MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)MultiChannelAssociationCommand.SupportedGroupingsGet,
            MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_ValidFrame()
    {
        // CC=0x8E, Cmd=0x06, SupportedGroupings=5
        byte[] data = [0x8E, 0x06, 0x05];
        CommandClassFrame frame = new(data);

        byte groupings = MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)5, groupings);
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_SingleGroup()
    {
        // CC=0x8E, Cmd=0x06, SupportedGroupings=1
        byte[] data = [0x8E, 0x06, 0x01];
        CommandClassFrame frame = new(data);

        byte groupings = MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, groupings);
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_MaxGroups()
    {
        // CC=0x8E, Cmd=0x06, SupportedGroupings=255
        byte[] data = [0x8E, 0x06, 0xFF];
        CommandClassFrame frame = new(data);

        byte groupings = MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsReportCommand.Parse(
            frame, NullLogger.Instance);

        Assert.AreEqual((byte)255, groupings);
    }

    [TestMethod]
    public void SupportedGroupingsReport_Parse_TooShort_Throws()
    {
        // CC=0x8E, Cmd=0x06, no parameters
        byte[] data = [0x8E, 0x06];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultiChannelAssociationCommandClass.MultiChannelAssociationSupportedGroupingsReportCommand.Parse(
                frame, NullLogger.Instance));
    }
}
