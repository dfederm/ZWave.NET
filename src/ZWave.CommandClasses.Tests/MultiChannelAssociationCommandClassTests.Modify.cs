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
                Array.Empty<EndPointDestination>());

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
    public void SetCommand_Create_EndPointDestinationsOnly()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                2,
                Array.Empty<byte>(),
                new EndPointDestination[] { new EndPointDestination(5, 1) });

        // CC + Cmd + GroupId + Marker + MCNodeID + Properties = 6 bytes
        Assert.AreEqual(6, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)2, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)5, parameters[2]); // MCNodeID
        Assert.AreEqual((byte)0x01, parameters[3]); // IsBitAddress=0|EP=1
    }

    [TestMethod]
    public void SetCommand_Create_MixedDestinations()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                new byte[] { 2 },
                new EndPointDestination[] { new EndPointDestination(3, 1) });

        // CC + Cmd + GroupId + NodeID(2) + Marker + MCNodeID(3) + Properties = 7 bytes
        Assert.AreEqual(7, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)2, parameters[1]); // NodeID
        Assert.AreEqual((byte)0x00, parameters[2]); // Marker
        Assert.AreEqual((byte)3, parameters[3]); // MCNodeID
        Assert.AreEqual((byte)0x01, parameters[4]); // IsBitAddress=0|EP=1
    }

    [TestMethod]
    public void SetCommand_Create_BitAddressEndPoint()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                Array.Empty<byte>(),
                new EndPointDestination[] { new EndPointDestination(4, new byte[] { 1, 2, 3 }) });

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)4, parameters[2]); // MCNodeID
        Assert.AreEqual((byte)0x87, parameters[3]); // IsBitAddress=1|EP=0x07 → 0x80 | 0x07 = 0x87
    }

    [TestMethod]
    public void SetCommand_Create_NoDestinations()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationSetCommand.Create(
                1,
                Array.Empty<byte>(),
                Array.Empty<EndPointDestination>());

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
                Array.Empty<EndPointDestination>());

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
                Array.Empty<EndPointDestination>());

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
                Array.Empty<EndPointDestination>());

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
                Array.Empty<EndPointDestination>());

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0, parameters[0]); // GroupId = 0
        Assert.AreEqual((byte)7, parameters[1]); // NodeID
    }

    [TestMethod]
    public void RemoveCommand_Create_EndPointFromGroup()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                3,
                Array.Empty<byte>(),
                new EndPointDestination[] { new EndPointDestination(5, 2) });

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
        Assert.AreEqual((byte)0x00, parameters[1]); // Marker
        Assert.AreEqual((byte)5, parameters[2]); // MCNodeID
        Assert.AreEqual((byte)0x02, parameters[3]); // IsBitAddress=0|EP=2
    }

    [TestMethod]
    public void RemoveCommand_Create_MixedFromGroup()
    {
        MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand command =
            MultiChannelAssociationCommandClass.MultiChannelAssociationRemoveCommand.Create(
                3,
                new byte[] { 2 },
                new EndPointDestination[] { new EndPointDestination(5, 1) });

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
        Assert.AreEqual((byte)2, parameters[1]); // NodeID
        Assert.AreEqual((byte)0x00, parameters[2]); // Marker
        Assert.AreEqual((byte)5, parameters[3]); // MCNodeID
        Assert.AreEqual((byte)0x01, parameters[4]); // IsBitAddress=0|EP=1
    }
}
