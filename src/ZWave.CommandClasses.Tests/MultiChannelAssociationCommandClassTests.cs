namespace ZWave.CommandClasses.Tests;

[TestClass]
public partial class MultiChannelAssociationCommandClassTests
{
    [TestMethod]
    public void EndpointDestination_SingleEndpoint_HasCorrectProperties()
    {
        EndpointDestination dest = new EndpointDestination(5, 3);

        Assert.AreEqual((byte)5, dest.NodeId);
        Assert.HasCount(1, dest.Endpoints);
        Assert.AreEqual((byte)3, dest.Endpoints[0]);
    }

    [TestMethod]
    public void EndpointDestination_SingleEndpoint_Zero_RootDevice()
    {
        EndpointDestination dest = new EndpointDestination(1, 0);

        Assert.AreEqual((byte)1, dest.NodeId);
        Assert.HasCount(1, dest.Endpoints);
        Assert.AreEqual((byte)0, dest.Endpoints[0]);
    }

    [TestMethod]
    public void EndpointDestination_MultipleEndpoints_HasCorrectProperties()
    {
        EndpointDestination dest = new EndpointDestination(4, new byte[] { 1, 2, 3 });

        Assert.AreEqual((byte)4, dest.NodeId);
        Assert.HasCount(3, dest.Endpoints);
        Assert.AreEqual((byte)1, dest.Endpoints[0]);
        Assert.AreEqual((byte)2, dest.Endpoints[1]);
        Assert.AreEqual((byte)3, dest.Endpoints[2]);
    }

    [TestMethod]
    public void EndpointDestination_MultipleEndpoints_SingleItem()
    {
        EndpointDestination dest = new EndpointDestination(4, new byte[] { 5 });

        Assert.HasCount(1, dest.Endpoints);
        Assert.AreEqual((byte)5, dest.Endpoints[0]);
    }

    [TestMethod]
    public void EndpointDestination_MultipleEndpoints_AllEndpoints()
    {
        EndpointDestination dest = new EndpointDestination(4, new byte[] { 1, 2, 3, 4, 5, 6, 7 });

        Assert.HasCount(7, dest.Endpoints);
        for (int i = 0; i < 7; i++)
        {
            Assert.AreEqual((byte)(i + 1), dest.Endpoints[i]);
        }
    }

    [TestMethod]
    public void EndpointDestination_MultipleEndpoints_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new EndpointDestination(4, (ReadOnlySpan<byte>)Array.Empty<byte>()));
    }
}
