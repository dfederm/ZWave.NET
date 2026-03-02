using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class AssociationCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        AssociationCommandClass.AssociationGetCommand command =
            AssociationCommandClass.AssociationGetCommand.Create(3);

        Assert.AreEqual(CommandClassId.Association, AssociationCommandClass.AssociationGetCommand.CommandClassId);
        Assert.AreEqual((byte)AssociationCommand.Get, AssociationCommandClass.AssociationGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + GroupId
        Assert.AreEqual((byte)3, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void Report_ParseInto_NodeIdDestinations()
    {
        // CC=0x85, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0, NodeID=2, NodeID=3
        byte[] data = [0x85, 0x03, 0x01, 0x05, 0x00, 0x02, 0x03];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            AssociationCommandClass.AssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)5, maxNodesSupported);
        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.HasCount(2, nodeIdDestinations);
        Assert.AreEqual((byte)2, nodeIdDestinations[0]);
        Assert.AreEqual((byte)3, nodeIdDestinations[1]);
    }

    [TestMethod]
    public void Report_ParseInto_EmptyDestinations()
    {
        // CC=0x85, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0, no destinations
        byte[] data = [0x85, 0x03, 0x01, 0x05, 0x00];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            AssociationCommandClass.AssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)5, maxNodesSupported);
        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.IsEmpty(nodeIdDestinations);
    }

    [TestMethod]
    public void Report_ParseInto_ReportsToFollow()
    {
        // CC=0x85, Cmd=0x03, GroupId=1, MaxNodes=20, ReportsToFollow=2, NodeID=1
        byte[] data = [0x85, 0x03, 0x01, 0x14, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            AssociationCommandClass.AssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)20, maxNodesSupported);
        Assert.AreEqual((byte)2, reportsToFollow);
        Assert.HasCount(1, nodeIdDestinations);
        Assert.AreEqual((byte)1, nodeIdDestinations[0]);
    }

    [TestMethod]
    public void Report_ParseInto_TooShort_Throws()
    {
        // Only 2 parameter bytes, need at least 3 (GroupId + MaxNodes + ReportsToFollow)
        byte[] data = [0x85, 0x03, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        Assert.Throws<ZWaveException>(
            () => AssociationCommandClass.AssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_ParseInto_MultiFrameAggregation()
    {
        // Frame 1: GroupId=1, MaxNodes=20, ReportsToFollow=1, NodeID=1, NodeID=2
        byte[] data1 = [0x85, 0x03, 0x01, 0x14, 0x01, 0x01, 0x02];
        CommandClassFrame frame1 = new(data1);

        // Frame 2: GroupId=1, MaxNodes=20, ReportsToFollow=0, NodeID=3
        byte[] data2 = [0x85, 0x03, 0x01, 0x14, 0x00, 0x03];
        CommandClassFrame frame2 = new(data2);

        List<byte> allNodeIdDestinations = [];

        (byte maxNodesSupported1, byte reportsToFollow1) =
            AssociationCommandClass.AssociationReportCommand.ParseInto(
                frame1, allNodeIdDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)20, maxNodesSupported1);
        Assert.AreEqual((byte)1, reportsToFollow1);

        (byte maxNodesSupported2, byte reportsToFollow2) =
            AssociationCommandClass.AssociationReportCommand.ParseInto(
                frame2, allNodeIdDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)20, maxNodesSupported2);
        Assert.AreEqual((byte)0, reportsToFollow2);

        // Combined result: 3 NodeID destinations
        Assert.HasCount(3, allNodeIdDestinations);
        Assert.AreEqual((byte)1, allNodeIdDestinations[0]);
        Assert.AreEqual((byte)2, allNodeIdDestinations[1]);
        Assert.AreEqual((byte)3, allNodeIdDestinations[2]);
    }

    [TestMethod]
    public void Report_ParseInto_ManyNodes()
    {
        // CC=0x85, Cmd=0x03, GroupId=1, MaxNodes=10, ReportsToFollow=0, NodeID=1..5
        byte[] data = [0x85, 0x03, 0x01, 0x0A, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            AssociationCommandClass.AssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)10, maxNodesSupported);
        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.HasCount(5, nodeIdDestinations);
        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual((byte)(i + 1), nodeIdDestinations[i]);
        }
    }
}
