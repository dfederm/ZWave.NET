namespace ZWave.CommandClasses.Tests;

[TestClass]
public partial class MultiChannelAssociationCommandClassTests
{
    [TestMethod]
    public void EndPointDestination_SingleEndpoint_HasCorrectProperties()
    {
        EndPointDestination dest = new EndPointDestination(5, 3);

        Assert.AreEqual((byte)5, dest.NodeId);
        Assert.IsFalse(dest.IsBitAddress);
        Assert.AreEqual((byte)3, dest.Destination);
    }

    [TestMethod]
    public void EndPointDestination_SingleEndpoint_Zero_RootDevice()
    {
        EndPointDestination dest = new EndPointDestination(1, 0);

        Assert.AreEqual((byte)1, dest.NodeId);
        Assert.IsFalse(dest.IsBitAddress);
        Assert.AreEqual((byte)0, dest.Destination);
    }

    [TestMethod]
    public void EndPointDestination_MultipleEndpoints_HasCorrectProperties()
    {
        EndPointDestination dest = new EndPointDestination(4, new byte[] { 1, 2, 3 });

        Assert.AreEqual((byte)4, dest.NodeId);
        Assert.IsTrue(dest.IsBitAddress);
        // Endpoints 1, 2, 3 → bits 0, 1, 2 → 0b00000111 = 0x07
        Assert.AreEqual((byte)0x07, dest.Destination);
    }

    [TestMethod]
    public void EndPointDestination_MultipleEndpoints_SingleEndpoint()
    {
        EndPointDestination dest = new EndPointDestination(4, new byte[] { 5 });

        Assert.IsTrue(dest.IsBitAddress);
        // Endpoint 5 → bit 4 → 0b00010000 = 0x10
        Assert.AreEqual((byte)0x10, dest.Destination);
    }

    [TestMethod]
    public void EndPointDestination_MultipleEndpoints_AllEndpoints()
    {
        EndPointDestination dest = new EndPointDestination(4, new byte[] { 1, 2, 3, 4, 5, 6, 7 });

        Assert.IsTrue(dest.IsBitAddress);
        // Endpoints 1-7 → bits 0-6 → 0b01111111 = 0x7F
        Assert.AreEqual((byte)0x7F, dest.Destination);
    }

    [TestMethod]
    public void EndPointDestination_MultipleEndpoints_EndpointZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EndPointDestination(4, new byte[] { 0 }));
    }

    [TestMethod]
    public void EndPointDestination_MultipleEndpoints_EndpointTooHigh_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EndPointDestination(4, new byte[] { 8 }));
    }
}
