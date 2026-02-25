using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelCommandClassTests
{
    [TestMethod]
    public void AggregatedMembersGet_Create_HasCorrectFormat()
    {
        MultiChannelCommandClass.AggregatedMembersGetCommand command = MultiChannelCommandClass.AggregatedMembersGetCommand.Create(4);

        Assert.AreEqual(CommandClassId.MultiChannel, MultiChannelCommandClass.AggregatedMembersGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultiChannelCommand.AggregatedMembersGet, MultiChannelCommandClass.AggregatedMembersGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)4, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void AggregatedMembersGet_Create_RejectsInvalidIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.AggregatedMembersGetCommand.Create(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.AggregatedMembersGetCommand.Create(128));
    }

    [TestMethod]
    public void AggregatedMembersReport_Parse_WithMembers()
    {
        // Aggregated EP=4, 1 bitmask byte, bitmask=0x05 (EP1 + EP3)
        byte[] data = [0x60, 0x0F, 0x04, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        IReadOnlyList<byte> members = MultiChannelCommandClass.AggregatedMembersReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, members);
        Assert.AreEqual((byte)1, members[0]);
        Assert.AreEqual((byte)3, members[1]);
    }

    [TestMethod]
    public void AggregatedMembersReport_Parse_MultipleBitmaskBytes()
    {
        // Aggregated EP=5, 2 bitmask bytes, byte1=0x03 (EP1+EP2), byte2=0x01 (EP9)
        byte[] data = [0x60, 0x0F, 0x05, 0x02, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        IReadOnlyList<byte> members = MultiChannelCommandClass.AggregatedMembersReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, members);
        Assert.AreEqual((byte)1, members[0]);
        Assert.AreEqual((byte)2, members[1]);
        Assert.AreEqual((byte)9, members[2]);
    }

    [TestMethod]
    public void AggregatedMembersReport_Parse_NoMembers()
    {
        // Number of bit masks = 0
        byte[] data = [0x60, 0x0F, 0x04, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlyList<byte> members = MultiChannelCommandClass.AggregatedMembersReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(members);
    }

    [TestMethod]
    public void AggregatedMembersReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x60, 0x0F, 0x04];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultiChannelCommandClass.AggregatedMembersReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void AggregatedMembersReport_GetAggregatedEndpointIndex()
    {
        byte[] data = [0x60, 0x0F, 0x87, 0x01, 0x01];
        CommandClassFrame frame = new(data);

        byte index = MultiChannelCommandClass.AggregatedMembersReportCommand.GetAggregatedEndpointIndex(frame);

        Assert.AreEqual((byte)7, index); // 0x87 masked to 7
    }
}
