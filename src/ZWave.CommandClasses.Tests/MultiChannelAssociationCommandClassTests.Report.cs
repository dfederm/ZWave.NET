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
    public void Report_ParseInto_NodeIdDestinationsOnly()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0, NodeID=2, NodeID=3
        byte[] data = [0x8E, 0x03, 0x01, 0x05, 0x00, 0x02, 0x03];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)5, maxNodesSupported);
        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.HasCount(2, nodeIdDestinations);
        Assert.AreEqual((byte)2, nodeIdDestinations[0]);
        Assert.AreEqual((byte)3, nodeIdDestinations[1]);
        Assert.IsEmpty(endpointDestinations);
    }

    [TestMethod]
    public void Report_ParseInto_EndpointDestinationsOnly_GroupedByNodeId()
    {
        // CC=0x8E, Cmd=0x03, GroupId=2, MaxNodes=10, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=5, EP=1, MCNodeID=5, EP=2
        byte[] data = [0x8E, 0x03, 0x02, 0x0A, 0x00, 0x00, 0x05, 0x01, 0x05, 0x02];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)10, maxNodesSupported);
        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.IsEmpty(nodeIdDestinations);
        // Two wire entries for node 5, EP1 and EP2 → grouped into one EndpointDestination
        Assert.HasCount(1, endpointDestinations);
        Assert.AreEqual((byte)5, endpointDestinations[0].NodeId);
        Assert.HasCount(2, endpointDestinations[0].Endpoints);
        Assert.AreEqual((byte)1, endpointDestinations[0].Endpoints[0]);
        Assert.AreEqual((byte)2, endpointDestinations[0].Endpoints[1]);
    }

    [TestMethod]
    public void Report_ParseInto_MixedDestinations()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=10, ReportsToFollow=0,
        // NodeID=1, NodeID=2, Marker=0x00, MCNodeID=3, EP=1
        byte[] data = [0x8E, 0x03, 0x01, 0x0A, 0x00, 0x01, 0x02, 0x00, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
            frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        Assert.HasCount(2, nodeIdDestinations);
        Assert.AreEqual((byte)1, nodeIdDestinations[0]);
        Assert.AreEqual((byte)2, nodeIdDestinations[1]);
        Assert.HasCount(1, endpointDestinations);
        Assert.AreEqual((byte)3, endpointDestinations[0].NodeId);
        Assert.HasCount(1, endpointDestinations[0].Endpoints);
        Assert.AreEqual((byte)1, endpointDestinations[0].Endpoints[0]);
    }

    [TestMethod]
    public void Report_ParseInto_EmptyDestinations()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0, no destinations
        byte[] data = [0x8E, 0x03, 0x01, 0x05, 0x00];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)5, maxNodesSupported);
        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.IsEmpty(nodeIdDestinations);
        Assert.IsEmpty(endpointDestinations);
    }

    [TestMethod]
    public void Report_ParseInto_BitAddressFlag_ExpandsEndpoints()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=10, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=4, BitAddr=1|0b0000111 → properties byte = 0x87
        byte[] data = [0x8E, 0x03, 0x01, 0x0A, 0x00, 0x00, 0x04, 0x87];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
            frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        // 0x87 = 0b1000_0111 → bit address, endpoints 1, 2, 3
        Assert.HasCount(1, endpointDestinations);
        Assert.AreEqual((byte)4, endpointDestinations[0].NodeId);
        Assert.HasCount(3, endpointDestinations[0].Endpoints);
        Assert.AreEqual((byte)1, endpointDestinations[0].Endpoints[0]);
        Assert.AreEqual((byte)2, endpointDestinations[0].Endpoints[1]);
        Assert.AreEqual((byte)3, endpointDestinations[0].Endpoints[2]);
    }

    [TestMethod]
    public void Report_ParseInto_EndpointZero_V3()
    {
        // V3 allows EndPoint 0 (Root Device destination).
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=5, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=1, BitAddr=0|EP=0
        byte[] data = [0x8E, 0x03, 0x01, 0x05, 0x00, 0x00, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
            frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        Assert.HasCount(1, endpointDestinations);
        Assert.AreEqual((byte)1, endpointDestinations[0].NodeId);
        Assert.HasCount(1, endpointDestinations[0].Endpoints);
        Assert.AreEqual((byte)0, endpointDestinations[0].Endpoints[0]);
    }

    [TestMethod]
    public void Report_ParseInto_ReportsToFollow()
    {
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=20, ReportsToFollow=2, NodeID=1
        byte[] data = [0x8E, 0x03, 0x01, 0x14, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        (byte maxNodesSupported, byte reportsToFollow) =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)2, reportsToFollow);
        Assert.HasCount(1, nodeIdDestinations);
    }

    [TestMethod]
    public void Report_ParseInto_TooShort_Throws()
    {
        // Only 2 parameter bytes, need at least 3 (GroupId + MaxNodes + ReportsToFollow)
        byte[] data = [0x8E, 0x03, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        Assert.Throws<ZWaveException>(
            () => MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
                frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_ParseInto_MultipleEndpointsSameNode_GroupedByNodeId()
    {
        // Same NodeID with different endpoints (e.g. power strip with 3 outlets).
        // CC=0x8E, Cmd=0x03, GroupId=1, MaxNodes=10, ReportsToFollow=0,
        // Marker=0x00, MCNodeID=5 EP=1, MCNodeID=5 EP=2, MCNodeID=5 EP=3
        byte[] data = [0x8E, 0x03, 0x01, 0x0A, 0x00, 0x00, 0x05, 0x01, 0x05, 0x02, 0x05, 0x03];
        CommandClassFrame frame = new(data);

        List<byte> nodeIdDestinations = [];
        List<EndpointDestination> endpointDestinations = [];
        MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
            frame, nodeIdDestinations, endpointDestinations, NullLogger.Instance);

        // Three wire entries for node 5 → grouped into one EndpointDestination
        Assert.HasCount(1, endpointDestinations);
        Assert.AreEqual((byte)5, endpointDestinations[0].NodeId);
        Assert.HasCount(3, endpointDestinations[0].Endpoints);
        Assert.AreEqual((byte)1, endpointDestinations[0].Endpoints[0]);
        Assert.AreEqual((byte)2, endpointDestinations[0].Endpoints[1]);
        Assert.AreEqual((byte)3, endpointDestinations[0].Endpoints[2]);
    }

    [TestMethod]
    public void Report_ParseInto_MultiFrameAggregation()
    {
        // Frame 1: GroupId=1, MaxNodes=20, ReportsToFollow=1, NodeID=1, NodeID=2
        byte[] data1 = [0x8E, 0x03, 0x01, 0x14, 0x01, 0x01, 0x02];
        CommandClassFrame frame1 = new(data1);

        // Frame 2: GroupId=1, MaxNodes=20, ReportsToFollow=0, Marker=0x00, MCNodeID=5 EP=1
        byte[] data2 = [0x8E, 0x03, 0x01, 0x14, 0x00, 0x00, 0x05, 0x01];
        CommandClassFrame frame2 = new(data2);

        List<byte> allNodeIdDestinations = [];
        List<EndpointDestination> allEndpointDestinations = [];

        (byte maxNodesSupported1, byte reportsToFollow1) =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
                frame1, allNodeIdDestinations, allEndpointDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)20, maxNodesSupported1);
        Assert.AreEqual((byte)1, reportsToFollow1);

        (byte maxNodesSupported2, byte reportsToFollow2) =
            MultiChannelAssociationCommandClass.MultiChannelAssociationReportCommand.ParseInto(
                frame2, allNodeIdDestinations, allEndpointDestinations, NullLogger.Instance);

        Assert.AreEqual((byte)20, maxNodesSupported2);
        Assert.AreEqual((byte)0, reportsToFollow2);

        // Combined result: 2 NodeID destinations + 1 endpoint destination
        Assert.HasCount(2, allNodeIdDestinations);
        Assert.AreEqual((byte)1, allNodeIdDestinations[0]);
        Assert.AreEqual((byte)2, allNodeIdDestinations[1]);
        Assert.HasCount(1, allEndpointDestinations);
        Assert.AreEqual((byte)5, allEndpointDestinations[0].NodeId);
        Assert.HasCount(1, allEndpointDestinations[0].Endpoints);
        Assert.AreEqual((byte)1, allEndpointDestinations[0].Endpoints[0]);
    }
}
