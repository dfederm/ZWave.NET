namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelAssociationCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_NodeIdDestinationsOnly()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                new byte[] { 2, 3 },
                Array.Empty<EndpointDestination>());

        Assert.AreEqual(CommandClassId.MultiChannelAssociation, MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.CommandClassId);
        Assert.AreEqual((byte)MultiChannelAssociationCommand.Set, MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.CommandId);

        // CC + Cmd + GroupId + NodeID(2) + NodeID(3) = 5 bytes
        Assert.AreEqual(5, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)2, parameters[1]); // NodeID 1
        Assert.AreEqual((byte)3, parameters[2]); // NodeID 2
    }

    [TestMethod]
    public void SetCommand_Create_EndpointDestinationsOnly()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                2,
                Array.Empty<byte>(),
                new EndpointDestination[] { new EndpointDestination(5, 1) });

        // CC + Cmd + GroupId + Marker + MCNodeID + Properties = 6 bytes
        Assert.AreEqual(6, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)2, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)5, parameters[2]); // MCNodeID
        Assert.AreEqual((byte)0x01, parameters[3]); // EP=1
    }

    [TestMethod]
    public void SetCommand_Create_MixedDestinations()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                new byte[] { 2 },
                new EndpointDestination[] { new EndpointDestination(3, 1) });

        // CC + Cmd + GroupId + NodeID(2) + Marker + MCNodeID(3) + Properties = 7 bytes
        Assert.AreEqual(7, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)2, parameters[1]); // NodeID
        Assert.AreEqual((byte)0x00, parameters[2]); // Marker
        Assert.AreEqual((byte)3, parameters[3]); // MCNodeID
        Assert.AreEqual((byte)0x01, parameters[4]); // EP=1
    }

    [TestMethod]
    public void SetCommand_Create_BitAddressEndpoint()
    {
        // Multiple endpoints 1,2,3 on node 4 → uses bit addressing (0b1000_0111 = 0x87)
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                Array.Empty<byte>(),
                new EndpointDestination[] { new EndpointDestination(4, new byte[] { 1, 2, 3 }) });

        // CC + Cmd + GroupId + Marker + MCNodeID + 0x87 = 6 bytes
        Assert.AreEqual(6, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)4, parameters[2]); // MCNodeID
        Assert.AreEqual((byte)0x87, parameters[3]); // 0b1000_0111 = bit address with EP 1,2,3
    }

    [TestMethod]
    public void SetCommand_Create_FallbackToIndividual_OnlyOneBitAddressable()
    {
        // Endpoints 0 and 1: only EP 1 is bit-addressable (1-7), EP 0 is not.
        // Only 1 bit-addressable → no bit addressing, both written individually.
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                Array.Empty<byte>(),
                new EndpointDestination[] { new EndpointDestination(4, new byte[] { 0, 1 }) });

        // CC + Cmd + GroupId + Marker + (NodeId+EP0) + (NodeId+EP1) = 8 bytes
        Assert.AreEqual(8, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)4, parameters[2]); // MCNodeID
        Assert.AreEqual((byte)0x00, parameters[3]); // EP=0
        Assert.AreEqual((byte)4, parameters[4]); // MCNodeID
        Assert.AreEqual((byte)0x01, parameters[5]); // EP=1
    }

    [TestMethod]
    public void SetCommand_Create_MixedBitAddressAndIndividual()
    {
        // Endpoints 0,1,2,3: EP 0 is not bit-addressable, EPs 1,2,3 are → bit-addressed + EP 0 individual
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                Array.Empty<byte>(),
                new EndpointDestination[] { new EndpointDestination(4, new byte[] { 0, 1, 2, 3 }) });

        // CC + Cmd + GroupId + Marker + (NodeId+0x87 bit-addressed) + (NodeId+EP0) = 8 bytes
        Assert.AreEqual(8, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)4, parameters[2]); // MCNodeID (bit-addressed entry)
        Assert.AreEqual((byte)0x87, parameters[3]); // 0b1000_0111 = bit address with EP 1,2,3
        Assert.AreEqual((byte)4, parameters[4]); // MCNodeID (individual entry)
        Assert.AreEqual((byte)0x00, parameters[5]); // EP=0
    }

    [TestMethod]
    public void SetCommand_Create_NoDestinations()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                Array.Empty<byte>(),
                Array.Empty<EndpointDestination>());

        // CC + Cmd + GroupId = 3 bytes. No marker because no EP destinations.
        Assert.AreEqual(3, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
    }

    [TestMethod]
    public void RemoveCommand_Create_SpecificNodeIdFromGroup()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                3,
                new byte[] { 5 },
                Array.Empty<EndpointDestination>());

        Assert.AreEqual(CommandClassId.MultiChannelAssociation, MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.CommandClassId);
        Assert.AreEqual((byte)MultiChannelAssociationCommand.Remove, MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
        Assert.AreEqual((byte)5, parameters[1]); // NodeID
    }

    [TestMethod]
    public void RemoveCommand_Create_AllFromGroup()
    {
        // GroupId > 0, no destinations → remove all from group
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                3,
                Array.Empty<byte>(),
                Array.Empty<EndpointDestination>());

        // CC + Cmd + GroupId = 3 bytes
        Assert.AreEqual(3, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
    }

    [TestMethod]
    public void RemoveCommand_Create_AllFromAllGroups()
    {
        // GroupId = 0, no destinations → remove all from all groups
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                0,
                Array.Empty<byte>(),
                Array.Empty<EndpointDestination>());

        Assert.AreEqual(3, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0, parameters[0]); // GroupId = 0
    }

    [TestMethod]
    public void RemoveCommand_Create_SpecificNodeIdFromAllGroups()
    {
        // GroupId = 0, with NodeID → remove from all groups
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                0,
                new byte[] { 7 },
                Array.Empty<EndpointDestination>());

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0, parameters[0]); // GroupId = 0
        Assert.AreEqual((byte)7, parameters[1]); // NodeID
    }

    [TestMethod]
    public void RemoveCommand_Create_EndpointFromGroup()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                3,
                Array.Empty<byte>(),
                new EndpointDestination[] { new EndpointDestination(5, 2) });

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)5, parameters[2]); // MCNodeID
        Assert.AreEqual((byte)0x02, parameters[3]); // EP=2
    }

    [TestMethod]
    public void RemoveCommand_Create_MixedFromGroup()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                3,
                new byte[] { 2 },
                new EndpointDestination[] { new EndpointDestination(5, 1) });

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
        Assert.AreEqual((byte)2, parameters[1]); // NodeID
        Assert.AreEqual((byte)0x00, parameters[2]); // Marker
        Assert.AreEqual((byte)5, parameters[3]); // MCNodeID
        Assert.AreEqual((byte)0x01, parameters[4]); // EP=1
    }
}
