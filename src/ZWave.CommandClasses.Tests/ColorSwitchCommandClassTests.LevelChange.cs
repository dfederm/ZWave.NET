using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ColorSwitchCommandClassTests
{
    [TestMethod]
    public void StartLevelChangeCommand_Create_Version1_Up_IgnoreStartLevel()
    {
        ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: ColorSwitchChangeDirection.Up,
                colorComponent: ColorSwitchColorComponent.Red,
                startLevel: null,
                duration: null);

        Assert.AreEqual(CommandClassId.ColorSwitch, ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.CommandClassId);
        Assert.AreEqual((byte)ColorSwitchCommand.StartLevelChange, ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 3 parameter bytes (no duration in V1)
        Assert.AreEqual(3, parameters.Length);
        // Bit 6 = 0 (Up), Bit 5 = 1 (Ignore Start Level)
        Assert.AreEqual(0b0010_0000, parameters[0]);
        // Color Component ID = Red (0x02)
        Assert.AreEqual((byte)ColorSwitchColorComponent.Red, parameters[1]);
        // Start Level = 0 (default when null)
        Assert.AreEqual(0x00, parameters[2]);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version1_Down_WithStartLevel()
    {
        ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: ColorSwitchChangeDirection.Down,
                colorComponent: ColorSwitchColorComponent.Blue,
                startLevel: 0x80,
                duration: null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        // Bit 6 = 1 (Down), Bit 5 = 0 (respect Start Level)
        Assert.AreEqual(0b0100_0000, parameters[0]);
        Assert.AreEqual((byte)ColorSwitchColorComponent.Blue, parameters[1]);
        Assert.AreEqual(0x80, parameters[2]);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version1_IgnoresDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));

        ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: ColorSwitchChangeDirection.Up,
                colorComponent: ColorSwitchColorComponent.Green,
                startLevel: null,
                duration: duration);

        // V1 does not include duration
        Assert.AreEqual(3, command.Frame.CommandParameters.Span.Length);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version2_IgnoresDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));

        ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.Create(
                version: 2,
                direction: ColorSwitchChangeDirection.Up,
                colorComponent: ColorSwitchColorComponent.Green,
                startLevel: null,
                duration: duration);

        // V2 does not include duration either (added in V3)
        Assert.AreEqual(3, command.Frame.CommandParameters.Span.Length);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version3_WithDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(10));

        ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.Create(
                version: 3,
                direction: ColorSwitchChangeDirection.Down,
                colorComponent: ColorSwitchColorComponent.WarmWhite,
                startLevel: 0xFF,
                duration: duration);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(4, parameters.Length);
        // Bit 6 = 1 (Down), Bit 5 = 0 (respect Start Level)
        Assert.AreEqual(0b0100_0000, parameters[0]);
        Assert.AreEqual((byte)ColorSwitchColorComponent.WarmWhite, parameters[1]);
        Assert.AreEqual(0xFF, parameters[2]);
        Assert.AreEqual(0x0A, parameters[3]); // 10 seconds
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_Version3_NullDuration()
    {
        ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.Create(
                version: 3,
                direction: ColorSwitchChangeDirection.Up,
                colorComponent: ColorSwitchColorComponent.Red,
                startLevel: null,
                duration: null);

        // No duration means no extra byte
        Assert.AreEqual(3, command.Frame.CommandParameters.Span.Length);
    }

    [TestMethod]
    public void StartLevelChangeCommand_Create_ReservedBitsAreZero()
    {
        ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStartLevelChangeCommand.Create(
                version: 1,
                direction: ColorSwitchChangeDirection.Down,
                colorComponent: ColorSwitchColorComponent.Red,
                startLevel: 0x00,
                duration: null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Bit 7 (reserved) must be 0, Bits 4-0 (reserved) must be 0
        Assert.AreEqual(0x00, parameters[0] & 0b1000_0000); // bit 7 reserved
        Assert.AreEqual(0x00, parameters[0] & 0b0001_1111); // bits 4-0 reserved
    }

    [TestMethod]
    public void StopLevelChangeCommand_Create_HasCorrectFormat()
    {
        ColorSwitchCommandClass.ColorSwitchStopLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStopLevelChangeCommand.Create(ColorSwitchColorComponent.Green);

        Assert.AreEqual(CommandClassId.ColorSwitch, ColorSwitchCommandClass.ColorSwitchStopLevelChangeCommand.CommandClassId);
        Assert.AreEqual((byte)ColorSwitchCommand.StopLevelChange, ColorSwitchCommandClass.ColorSwitchStopLevelChangeCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)ColorSwitchColorComponent.Green, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void StopLevelChangeCommand_Create_WarmWhite()
    {
        ColorSwitchCommandClass.ColorSwitchStopLevelChangeCommand command =
            ColorSwitchCommandClass.ColorSwitchStopLevelChangeCommand.Create(ColorSwitchColorComponent.WarmWhite);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)ColorSwitchColorComponent.WarmWhite, command.Frame.CommandParameters.Span[0]);
    }
}
