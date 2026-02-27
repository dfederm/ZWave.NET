using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ColorSwitchCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ColorSwitchCommandClass.ColorSwitchGetCommand command =
            ColorSwitchCommandClass.ColorSwitchGetCommand.Create(ColorSwitchColorComponent.Red);

        Assert.AreEqual(CommandClassId.ColorSwitch, ColorSwitchCommandClass.ColorSwitchGetCommand.CommandClassId);
        Assert.AreEqual((byte)ColorSwitchCommand.Get, ColorSwitchCommandClass.ColorSwitchGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)ColorSwitchColorComponent.Red, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_WarmWhite()
    {
        ColorSwitchCommandClass.ColorSwitchGetCommand command =
            ColorSwitchCommandClass.ColorSwitchGetCommand.Create(ColorSwitchColorComponent.WarmWhite);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)ColorSwitchColorComponent.WarmWhite, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void Report_Parse_Version1_CurrentValueOnly()
    {
        // CC=0x33, Cmd=0x04, ColorComponent=0x02 (Red), Value=0x80
        byte[] data = [0x33, 0x04, 0x02, 0x80];
        CommandClassFrame frame = new(data);

        ColorSwitchReport report = ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ColorSwitchColorComponent.Red, report.ColorComponent);
        Assert.AreEqual((byte)0x80, report.CurrentValue);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version1_MinValue()
    {
        // CC=0x33, Cmd=0x04, ColorComponent=0x04 (Blue), Value=0x00
        byte[] data = [0x33, 0x04, 0x04, 0x00];
        CommandClassFrame frame = new(data);

        ColorSwitchReport report = ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ColorSwitchColorComponent.Blue, report.ColorComponent);
        Assert.AreEqual((byte)0x00, report.CurrentValue);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version1_MaxValue()
    {
        // CC=0x33, Cmd=0x04, ColorComponent=0x03 (Green), Value=0xFF
        byte[] data = [0x33, 0x04, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        ColorSwitchReport report = ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ColorSwitchColorComponent.Green, report.ColorComponent);
        Assert.AreEqual((byte)0xFF, report.CurrentValue);
    }

    [TestMethod]
    public void Report_Parse_Version3_WithTargetAndDuration()
    {
        // CC=0x33, Cmd=0x04, ColorComponent=0x02 (Red), Current=0x00, Target=0xFF, Duration=0x05 (5 sec)
        byte[] data = [0x33, 0x04, 0x02, 0x00, 0xFF, 0x05];
        CommandClassFrame frame = new(data);

        ColorSwitchReport report = ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ColorSwitchColorComponent.Red, report.ColorComponent);
        Assert.AreEqual((byte)0x00, report.CurrentValue);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0xFF, report.TargetValue.Value);
        Assert.IsNotNull(report.Duration);
        Assert.AreEqual((byte)0x05, report.Duration.Value.Value);
        Assert.AreEqual(TimeSpan.FromSeconds(5), report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version3_AlreadyAtTarget()
    {
        // CC=0x33, Cmd=0x04, ColorComponent=0x00 (WW), Current=0xFF, Target=0xFF, Duration=0x00
        byte[] data = [0x33, 0x04, 0x00, 0xFF, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        ColorSwitchReport report = ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ColorSwitchColorComponent.WarmWhite, report.ColorComponent);
        Assert.AreEqual((byte)0xFF, report.CurrentValue);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0xFF, report.TargetValue.Value);
        Assert.IsNotNull(report.Duration);
        Assert.AreEqual(TimeSpan.Zero, report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version3_PartialPayload_TargetOnly()
    {
        // CC=0x33, Cmd=0x04, ColorComponent=0x02, Current=0xFF, Target=0x00, no duration
        byte[] data = [0x33, 0x04, 0x02, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        ColorSwitchReport report = ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ColorSwitchColorComponent.Red, report.ColorComponent);
        Assert.AreEqual((byte)0xFF, report.CurrentValue);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0x00, report.TargetValue.Value);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version3_UnknownDuration()
    {
        // CC=0x33, Cmd=0x04, ColorComponent=0x02, Current=0x00, Target=0xFF, Duration=0xFE (unknown)
        byte[] data = [0x33, 0x04, 0x02, 0x00, 0xFF, 0xFE];
        CommandClassFrame frame = new(data);

        ColorSwitchReport report = ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(report.Duration);
        Assert.AreEqual((byte)0xFE, report.Duration.Value.Value);
        Assert.IsNull(report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x33, Cmd=0x04, only 1 parameter byte (need at least 2)
        byte[] data = [0x33, 0x04, 0x02];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_TooShort_NoParameters_Throws()
    {
        // CC=0x33, Cmd=0x04, no parameters
        byte[] data = [0x33, 0x04];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ColorSwitchCommandClass.ColorSwitchReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SetCommand_Create_Version1_SingleComponent_NoDuration()
    {
        Dictionary<ColorSwitchColorComponent, byte> values = new()
        {
            { ColorSwitchColorComponent.Red, 0xFF },
        };

        ColorSwitchCommandClass.ColorSwitchSetCommand command =
            ColorSwitchCommandClass.ColorSwitchSetCommand.Create(1, values, null);

        Assert.AreEqual(CommandClassId.ColorSwitch, ColorSwitchCommandClass.ColorSwitchSetCommand.CommandClassId);
        Assert.AreEqual((byte)ColorSwitchCommand.Set, ColorSwitchCommandClass.ColorSwitchSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Count byte + 1 component (2 bytes) = 3 parameter bytes
        Assert.AreEqual(3, parameters.Length);
        // Color Component Count = 1
        Assert.AreEqual(0x01, parameters[0] & 0b0001_1111);
        // Reserved bits must be 0
        Assert.AreEqual(0x00, parameters[0] & 0b1110_0000);
        // Color Component ID = Red (0x02)
        Assert.AreEqual((byte)ColorSwitchColorComponent.Red, parameters[1]);
        // Value = 0xFF
        Assert.AreEqual(0xFF, parameters[2]);
    }

    [TestMethod]
    public void SetCommand_Create_Version1_MultipleComponents()
    {
        Dictionary<ColorSwitchColorComponent, byte> values = new()
        {
            { ColorSwitchColorComponent.Red, 0x80 },
            { ColorSwitchColorComponent.Green, 0x40 },
            { ColorSwitchColorComponent.Blue, 0xC0 },
        };

        ColorSwitchCommandClass.ColorSwitchSetCommand command =
            ColorSwitchCommandClass.ColorSwitchSetCommand.Create(1, values, null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Count byte + 3 components (2 bytes each) = 7 parameter bytes
        Assert.AreEqual(7, parameters.Length);
        // Color Component Count = 3
        Assert.AreEqual(0x03, parameters[0] & 0b0001_1111);
    }

    [TestMethod]
    public void SetCommand_Create_Version1_IgnoresDuration()
    {
        Dictionary<ColorSwitchColorComponent, byte> values = new()
        {
            { ColorSwitchColorComponent.Red, 0xFF },
        };
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));

        ColorSwitchCommandClass.ColorSwitchSetCommand command =
            ColorSwitchCommandClass.ColorSwitchSetCommand.Create(1, values, duration);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // V1 does not include duration, even if provided
        Assert.AreEqual(3, parameters.Length);
    }

    [TestMethod]
    public void SetCommand_Create_Version2_WithDuration()
    {
        Dictionary<ColorSwitchColorComponent, byte> values = new()
        {
            { ColorSwitchColorComponent.Red, 0xFF },
        };
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(10));

        ColorSwitchCommandClass.ColorSwitchSetCommand command =
            ColorSwitchCommandClass.ColorSwitchSetCommand.Create(2, values, duration);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Count byte + 1 component (2 bytes) + 1 duration byte = 4 parameter bytes
        Assert.AreEqual(4, parameters.Length);
        Assert.AreEqual(0x01, parameters[0] & 0b0001_1111);
        Assert.AreEqual((byte)ColorSwitchColorComponent.Red, parameters[1]);
        Assert.AreEqual(0xFF, parameters[2]);
        Assert.AreEqual(0x0A, parameters[3]); // 10 seconds
    }

    [TestMethod]
    public void SetCommand_Create_Version2_NullDuration()
    {
        Dictionary<ColorSwitchColorComponent, byte> values = new()
        {
            { ColorSwitchColorComponent.Red, 0xFF },
        };

        ColorSwitchCommandClass.ColorSwitchSetCommand command =
            ColorSwitchCommandClass.ColorSwitchSetCommand.Create(2, values, null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // No duration means same as V1
        Assert.AreEqual(3, parameters.Length);
    }

    [TestMethod]
    public void SetCommand_Create_Version2_FactoryDefaultDuration()
    {
        Dictionary<ColorSwitchColorComponent, byte> values = new()
        {
            { ColorSwitchColorComponent.WarmWhite, 0x80 },
        };

        ColorSwitchCommandClass.ColorSwitchSetCommand command =
            ColorSwitchCommandClass.ColorSwitchSetCommand.Create(2, values, DurationSet.FactoryDefault);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(4, parameters.Length);
        Assert.AreEqual(0xFF, parameters[3]); // Factory default = 0xFF
    }

    [TestMethod]
    public void SetCommand_Create_ZeroValue()
    {
        Dictionary<ColorSwitchColorComponent, byte> values = new()
        {
            { ColorSwitchColorComponent.Blue, 0x00 },
        };

        ColorSwitchCommandClass.ColorSwitchSetCommand command =
            ColorSwitchCommandClass.ColorSwitchSetCommand.Create(1, values, null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)ColorSwitchColorComponent.Blue, parameters[1]);
        Assert.AreEqual(0x00, parameters[2]);
    }
}
