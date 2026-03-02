namespace ZWave.CommandClasses.Tests;

public partial class AssociationCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_WithNodeIds()
    {
        AssociationCommandClass.AssociationSetCommand command =
            AssociationCommandClass.AssociationSetCommand.Create(1, new byte[] { 2, 3 });

        Assert.AreEqual(CommandClassId.Association, AssociationCommandClass.AssociationSetCommand.CommandClassId);
        Assert.AreEqual((byte)AssociationCommand.Set, AssociationCommandClass.AssociationSetCommand.CommandId);

        // CC + Cmd + GroupId + NodeID(2) + NodeID(3) = 5 bytes
        Assert.AreEqual(5, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
        Assert.AreEqual((byte)2, parameters[1]); // NodeID 1
        Assert.AreEqual((byte)3, parameters[2]); // NodeID 2
    }

    [TestMethod]
    public void SetCommand_Create_NoNodeIds()
    {
        AssociationCommandClass.AssociationSetCommand command =
            AssociationCommandClass.AssociationSetCommand.Create(1, Array.Empty<byte>());

        // CC + Cmd + GroupId = 3 bytes
        Assert.AreEqual(3, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)1, parameters[0]); // GroupId
    }

    [TestMethod]
    public void SetCommand_Create_SingleNodeId()
    {
        AssociationCommandClass.AssociationSetCommand command =
            AssociationCommandClass.AssociationSetCommand.Create(5, new byte[] { 10 });

        // CC + Cmd + GroupId + NodeID = 4 bytes
        Assert.AreEqual(4, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)5, parameters[0]); // GroupId
        Assert.AreEqual((byte)10, parameters[1]); // NodeID
    }

    [TestMethod]
    public void RemoveCommand_Create_SpecificNodeIdFromGroup()
    {
        AssociationCommandClass.AssociationRemoveCommand command =
            AssociationCommandClass.AssociationRemoveCommand.Create(3, new byte[] { 5 });

        Assert.AreEqual(CommandClassId.Association, AssociationCommandClass.AssociationRemoveCommand.CommandClassId);
        Assert.AreEqual((byte)AssociationCommand.Remove, AssociationCommandClass.AssociationRemoveCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
        Assert.AreEqual((byte)5, parameters[1]); // NodeID
    }

    [TestMethod]
    public void RemoveCommand_Create_AllFromGroup()
    {
        // GroupId > 0, no NodeIDs → remove all from group
        AssociationCommandClass.AssociationRemoveCommand command =
            AssociationCommandClass.AssociationRemoveCommand.Create(3, Array.Empty<byte>());

        // CC + Cmd + GroupId = 3 bytes
        Assert.AreEqual(3, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)3, parameters[0]); // GroupId
    }

    [TestMethod]
    public void RemoveCommand_Create_AllFromAllGroups_V2()
    {
        // GroupId = 0, no NodeIDs → remove all from all groups (V2+)
        AssociationCommandClass.AssociationRemoveCommand command =
            AssociationCommandClass.AssociationRemoveCommand.Create(0, Array.Empty<byte>());

        Assert.AreEqual(3, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0, parameters[0]); // GroupId = 0
    }

    [TestMethod]
    public void RemoveCommand_Create_SpecificNodeIdFromAllGroups_V2()
    {
        // GroupId = 0, with NodeIDs → remove from all groups (V2+)
        AssociationCommandClass.AssociationRemoveCommand command =
            AssociationCommandClass.AssociationRemoveCommand.Create(0, new byte[] { 7 });

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0, parameters[0]); // GroupId = 0
        Assert.AreEqual((byte)7, parameters[1]); // NodeID
    }

    [TestMethod]
    public void RemoveCommand_Create_MultipleNodeIdsFromGroup()
    {
        AssociationCommandClass.AssociationRemoveCommand command =
            AssociationCommandClass.AssociationRemoveCommand.Create(2, new byte[] { 3, 4, 5 });

        // CC + Cmd + GroupId + 3 NodeIDs = 6 bytes
        Assert.AreEqual(6, command.Frame.Data.Length);
        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)2, parameters[0]); // GroupId
        Assert.AreEqual((byte)3, parameters[1]); // NodeID 1
        Assert.AreEqual((byte)4, parameters[2]); // NodeID 2
        Assert.AreEqual((byte)5, parameters[3]); // NodeID 3
    }
}
