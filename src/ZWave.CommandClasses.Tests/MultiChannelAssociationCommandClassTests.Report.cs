using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelAssociationCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationGetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationGetCommand.Create(3);

        Assert.AreEqual(CommandClassId.MultiChannelAssociation, MultiChannelAssociationCommandClass.MultiChannelAssociationGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultiChannelAssociationCommand.Get, MultiChannelAssociationCommandClass.MultiChannelAssociationGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + GroupId
        Assert.AreEqual((byte)3, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void Report_Parse_NodeIdDestinationsOnly()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0, NodeID=2, NodeID=3
        byte[] data = [0x8E, 0x03, 0x01, 0x05, 0x00, 0x02, 0x03];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.GroupingIdentifier);
        Assert.AreEqual((byte)5, report.MaxNodesSupported);
        Assert.AreEqual((byte)0, report.ReportsToFollow);
        Assert.HasCount(2, report.NodeIdDestinations);
        Assert.AreEqual((byte)2, report.NodeIdDestinations[0]);
        Assert.AreEqual((byte)3, report.NodeIdDestinations[1]);
        Assert.IsEmpty(report.EndPointDestinations);
    }

    [TestMethod]
    public void Report_Parse_EndPointDestinationsOnly()
    {
        // CC=0x8E, Cmd=0x03, GroupId=2, MaxNodes=10, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=5, BitAddr=0|EP=1, MCNodeID=5, BitAddr=0|EP=2
        byte[] data = [0x8E, 0x03, 0x02, 0x0A, 0x00, 0x00, 0x05, 0x01, 0x05, 0x02];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.GroupingIdentifier);
        Assert.AreEqual((byte)10, report.MaxNodesSupported);
        Assert.IsEmpty(report.NodeIdDestinations);
        Assert.HasCount(2, report.EndPointDestinations);
        Assert.AreEqual((byte)5, report.EndPointDestinations[0].NodeId);
        Assert.IsFalse(report.EndPointDestinations[0].IsBitAddress);
        Assert.AreEqual((byte)1, report.EndPointDestinations[0].Destination);
        Assert.AreEqual((byte)5, report.EndPointDestinations[1].NodeId);
        Assert.IsFalse(report.EndPointDestinations[1].IsBitAddress);
        Assert.AreEqual((byte)2, report.EndPointDestinations[1].Destination);
    }

    [TestMethod]
    public void Report_Parse_MixedDestinations()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=10, ReportsToFollow=0,
        // NodeID=1, NodeID=2, Marker=0x00, MCNodeID=3, BitAddr=0|EP=1
        byte[] data = [0x8E, 0x03, 0x01, 0x0A, 0x00, 0x01, 0x02, 0x00, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.GroupingIdentifier);
        Assert.HasCount(2, report.NodeIdDestinations);
        Assert.AreEqual((byte)1, report.NodeIdDestinations[0]);
        Assert.AreEqual((byte)2, report.NodeIdDestinations[1]);
        Assert.HasCount(1, report.EndPointDestinations);
        Assert.AreEqual((byte)3, report.EndPointDestinations[0].NodeId);
        Assert.IsFalse(report.EndPointDestinations[0].IsBitAddress);
        Assert.AreEqual((byte)1, report.EndPointDestinations[0].Destination);
    }

    [TestMethod]
    public void Report_Parse_EmptyDestinations()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0, no destinations
        byte[] data = [0x8E, 0x03, 0x01, 0x05, 0x00];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.GroupingIdentifier);
        Assert.AreEqual((byte)5, report.MaxNodesSupported);
        Assert.AreEqual((byte)0, report.ReportsToFollow);
        Assert.IsEmpty(report.NodeIdDestinations);
        Assert.IsEmpty(report.EndPointDestinations);
    }

    [TestMethod]
    public void Report_Parse_BitAddressFlag()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=10, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=4, BitAddr=1|EP=0x07 → properties byte = 0x87
        byte[] data = [0x8E, 0x03, 0x01, 0x0A, 0x00, 0x00, 0x04, 0x87];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, report.EndPointDestinations);
        Assert.AreEqual((byte)4, report.EndPointDestinations[0].NodeId);
        Assert.IsTrue(report.EndPointDestinations[0].IsBitAddress);
        Assert.AreEqual((byte)0x07, report.EndPointDestinations[0].Destination);
    }

    [TestMethod]
    public void Report_Parse_EndPointZero_V3()
    {
        // V3 allows EndPoint 0 (Root Device destination).
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=1, BitAddr=0|EP=0
        byte[] data = [0x8E, 0x03, 0x01, 0x05, 0x00, 0x00, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, report.EndPointDestinations);
        Assert.AreEqual((byte)1, report.EndPointDestinations[0].NodeId);
        Assert.IsFalse(report.EndPointDestinations[0].IsBitAddress);
        Assert.AreEqual((byte)0, report.EndPointDestinations[0].Destination);
    }

    [TestMethod]
    public void Report_Parse_ReportsToFollow()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=20, ReportsToFollow=2, NodeID=1
        byte[] data = [0x8E, 0x03, 0x01, 0x14, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.ReportsToFollow);
        Assert.HasCount(1, report.NodeIdDestinations);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // Only 2 parameter bytes, need at least 3 (GroupId + MaxNodes + ReportsToFollow)
        byte[] data = [0x8E, 0x03, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_MultipleEndPointsSameNode()
    {
        // Same NodeID with different endpoints (e.g. power strip with 3 outlets).
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=10, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=5 EP=1, MCNodeID=5 EP=2, MCNodeID=5 EP=3
        byte[] data = [0x8E, 0x03, 0x01, 0x0A, 0x00, 0x00, 0x05, 0x01, 0x05, 0x02, 0x05, 0x03];
        CommandClassFrame frame = new(data);

        MultiChannelAssociationReport report =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, report.EndPointDestinations);
        Assert.AreEqual((byte)1, report.EndPointDestinations[0].Destination);
        Assert.AreEqual((byte)2, report.EndPointDestinations[1].Destination);
        Assert.AreEqual((byte)3, report.EndPointDestinations[2].Destination);
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual((byte)5, report.EndPointDestinations[i].NodeId);
        }
    }
}
