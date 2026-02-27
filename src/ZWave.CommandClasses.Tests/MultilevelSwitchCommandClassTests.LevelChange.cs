namespace ZWave.CommandClasses.Tests;

public partial class MultilevelSwitchCommandClassTests
{
    [TestMethod]
    public void StartLevelChangeCommand_Create_Version1_Up_IgnoreStartLevel()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: MultilevelSwitchChangeDirection.Up,
                startLevel: null,
                duration: null);

        Assert.AreEqual(CommandClassId.MultilevelSwitch, MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSwitchCommand.StartLevelChange, MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 2 parameter bytes (no duration in V1)
        Assert.AreEqual(2, parameters.Length);
        // Bit 6 = 0 (Up), Bit 5 = 1 (Ignore Start Level)
        Assert.AreEqual(0b0010_0000, parameters[0]);
        // Start Level = 0 (default when null)
        Assert.AreEqual(0x00, parameters[1]);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version1_Down_WithStartLevel()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: MultilevelSwitchChangeDirection.Down,
                startLevel: new GenericValue(0x50),
                duration: null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(2, parameters.Length);
        // Bit 6 = 1 (Down), Bit 5 = 0 (respect Start Level)
        Assert.AreEqual(0b0100_0000, parameters[0]);
        Assert.AreEqual(0x50, parameters[1]);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version1_IgnoresDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));

        MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: MultilevelSwitchChangeDirection.Up,
                startLevel: null,
                duration: duration);

        // V1 does not include duration
        Assert.AreEqual(2, command.Frame.CommandParameters.Span.Length);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version2_WithDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(10));

        MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.Create(
                version: 2,
                direction: MultilevelSwitchChangeDirection.Down,
                startLevel: new GenericValue(0x63),
                duration: duration);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        // Bit 6 = 1 (Down), Bit 5 = 0 (respect Start Level)
        Assert.AreEqual(0b0100_0000, parameters[0]);
        Assert.AreEqual(0x63, parameters[1]);
        Assert.AreEqual(0x0A, parameters[2]); // 10 seconds
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version2_NullDuration()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.Create(
                version: 2,
                direction: MultilevelSwitchChangeDirection.Up,
                startLevel: null,
                duration: null);

        // No duration means no extra byte
        Assert.AreEqual(2, command.Frame.CommandParameters.Span.Length);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_ReservedBitsAreZero()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: MultilevelSwitchChangeDirection.Down,
                startLevel: new GenericValue(0x00),
                duration: null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Bit 7 (reserved) must be 0, Bits 4-0 (reserved) must be 0
        Assert.AreEqual(0x00, parameters[0] & 0b1000_0000); // bit 7 reserved
        Assert.AreEqual(0x00, parameters[0] & 0b0001_1111); // bits 4-0 reserved
    }

    [TestMethod]
    public void StopLevelChangeCommand_Create_HasCorrectFormat()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchStopLevelChangeCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchStopLevelChangeCommand.Create();

        Assert.AreEqual(CommandClassId.MultilevelSwitch, MultilevelSwitchCommandClass.MultilevelSwitchStopLevelChangeCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSwitchCommand.StopLevelChange, MultilevelSwitchCommandClass.MultilevelSwitchStopLevelChangeCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }
}
