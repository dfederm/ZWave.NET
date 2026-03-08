namespace ZWave.CommandClasses.Tests;

public partial class WindowCoveringCommandClassTests
{
    [TestMethod]
    public void StartLevelChangeCommand_Create_Up()
    {
        WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand.Create(
                direction: WindowCoveringChangeDirection.Up,
                parameterId: WindowCoveringParameterId.OutboundBottomPosition,
                duration: new DurationSet(TimeSpan.FromSeconds(10)));

        Assert.AreEqual(CommandClassId.WindowCovering, WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand.CommandClassId);
        Assert.AreEqual((byte)WindowCoveringCommand.StartLevelChange, WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        // Bit 6 = 0 (Up), all other bits reserved = 0
        Assert.AreEqual(0b0000_0000, parameters[0]);
        // Parameter ID = OutboundBottomPosition (0x0D)
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundBottomPosition, parameters[1]);
        // Duration = 10 seconds
        Assert.AreEqual(0x0A, parameters[2]);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Down()
    {
        WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand.Create(
                direction: WindowCoveringChangeDirection.Down,
                parameterId: WindowCoveringParameterId.OutboundLeftPosition,
                duration: new DurationSet(TimeSpan.FromSeconds(5)));

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        // Bit 6 = 1 (Down)
        Assert.AreEqual(0b0100_0000, parameters[0]);
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundLeftPosition, parameters[1]);
        Assert.AreEqual(0x05, parameters[2]);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_MovementParameter()
    {
        WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand.Create(
                direction: WindowCoveringChangeDirection.Up,
                parameterId: WindowCoveringParameterId.OutboundLeftMovement,
                duration: new DurationSet(0x0A));

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundLeftMovement, parameters[1]);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_ReservedBitsAreZero()
    {
        WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand.Create(
                direction: WindowCoveringChangeDirection.Down,
                parameterId: WindowCoveringParameterId.OutboundBottomPosition,
                duration: new DurationSet(0x05));

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Bit 7 (reserved) must be 0, Bits 5-0 (reserved) must be 0
        Assert.AreEqual(0x00, parameters[0] & 0b1000_0000); // bit 7 reserved
        Assert.AreEqual(0x00, parameters[0] & 0b0011_1111); // bits 5-0 reserved
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_FactoryDefaultDuration()
    {
        WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStartLevelChangeCommand.Create(
                direction: WindowCoveringChangeDirection.Up,
                parameterId: WindowCoveringParameterId.InboundTopBottomPosition,
                duration: DurationSet.FactoryDefault);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0xFF, parameters[2]); // Factory default
    }

    [TestMethod]
    public void StopLevelChangeCommand_Create_HasCorrectFormat()
    {
        WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand.Create(WindowCoveringParameterId.OutboundBottomPosition);

        Assert.AreEqual(CommandClassId.WindowCovering, WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand.CommandClassId);
        Assert.AreEqual((byte)WindowCoveringCommand.StopLevelChange, WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundBottomPosition, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void StopLevelChangeCommand_Create_MovementParameter()
    {
        WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand.Create(WindowCoveringParameterId.OutboundLeftMovement);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundLeftMovement, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void StopLevelChangeCommand_Create_SlatsAngle()
    {
        WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand command =
            WindowCoveringCommandClass.WindowCoveringStopLevelChangeCommand.Create(WindowCoveringParameterId.HorizontalSlatsAnglePosition);

        Assert.AreEqual((byte)WindowCoveringParameterId.HorizontalSlatsAnglePosition, command.Frame.CommandParameters.Span[0]);
    }
}
