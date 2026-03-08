using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class WindowCoveringCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        WindowCoveringCommandClass.WindowCoveringGetCommand command =
            WindowCoveringCommandClass.WindowCoveringGetCommand.Create(WindowCoveringParameterId.OutboundLeftPosition);

        Assert.AreEqual(CommandClassId.WindowCovering, WindowCoveringCommandClass.WindowCoveringGetCommand.CommandClassId);
        Assert.AreEqual((byte)WindowCoveringCommand.Get, WindowCoveringCommandClass.WindowCoveringGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundLeftPosition, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_MovementParameter()
    {
        WindowCoveringCommandClass.WindowCoveringGetCommand command =
            WindowCoveringCommandClass.WindowCoveringGetCommand.Create(WindowCoveringParameterId.OutboundLeftMovement);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundLeftMovement, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_SlatsAngle()
    {
        WindowCoveringCommandClass.WindowCoveringGetCommand command =
            WindowCoveringCommandClass.WindowCoveringGetCommand.Create(WindowCoveringParameterId.HorizontalSlatsAnglePosition);

        Assert.AreEqual((byte)WindowCoveringParameterId.HorizontalSlatsAnglePosition, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void Report_Parse_PositionParameter()
    {
        // CC=0x6A, Cmd=0x04, ParameterId=0x01 (OutboundLeftPosition), Current=0x32, Target=0x63, Duration=0x05
        byte[] data = [0x6A, 0x04, 0x01, 0x32, 0x63, 0x05];
        CommandClassFrame frame = new(data);

        WindowCoveringReport report = WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(WindowCoveringParameterId.OutboundLeftPosition, report.ParameterId);
        Assert.AreEqual((byte)0x32, report.CurrentValue);
        Assert.AreEqual((byte)0x63, report.TargetValue);
        Assert.AreEqual((byte)0x05, report.Duration.Value);
        Assert.AreEqual(TimeSpan.FromSeconds(5), report.Duration.Duration);
    }

    [TestMethod]
    public void Report_Parse_MovementParameter_Stationary()
    {
        // Even parameter, not moving: Current=0x00, Target=0x00, Duration=0xFE (unknown)
        byte[] data = [0x6A, 0x04, 0x00, 0x00, 0x00, 0xFE];
        CommandClassFrame frame = new(data);

        WindowCoveringReport report = WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(WindowCoveringParameterId.OutboundLeftMovement, report.ParameterId);
        Assert.AreEqual((byte)0x00, report.CurrentValue);
        Assert.AreEqual((byte)0x00, report.TargetValue);
        Assert.AreEqual((byte)0xFE, report.Duration.Value);
        Assert.IsNull(report.Duration.Duration);
    }

    [TestMethod]
    public void Report_Parse_FullyOpen()
    {
        // Position parameter, fully open, transition complete
        byte[] data = [0x6A, 0x04, 0x0D, 0x63, 0x63, 0x00];
        CommandClassFrame frame = new(data);

        WindowCoveringReport report = WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(WindowCoveringParameterId.OutboundBottomPosition, report.ParameterId);
        Assert.AreEqual((byte)0x63, report.CurrentValue);
        Assert.AreEqual((byte)0x63, report.TargetValue);
        Assert.AreEqual(TimeSpan.Zero, report.Duration.Duration);
    }

    [TestMethod]
    public void Report_Parse_FullyClosed()
    {
        byte[] data = [0x6A, 0x04, 0x0D, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        WindowCoveringReport report = WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.CurrentValue);
        Assert.AreEqual((byte)0x00, report.TargetValue);
    }

    [TestMethod]
    public void Report_Parse_SlatsAngle_MidPosition()
    {
        // Vertical slats angle at open (0x32)
        byte[] data = [0x6A, 0x04, 0x0B, 0x32, 0x32, 0x00];
        CommandClassFrame frame = new(data);

        WindowCoveringReport report = WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(WindowCoveringParameterId.VerticalSlatsAnglePosition, report.ParameterId);
        Assert.AreEqual((byte)0x32, report.CurrentValue);
    }

    [TestMethod]
    public void Report_Parse_DurationInMinutes()
    {
        // Duration=0x82 (3 minutes per Table 2.10: 0x80=1min, 0x81=2min, 0x82=3min)
        byte[] data = [0x6A, 0x04, 0x01, 0x00, 0x63, 0x82];
        CommandClassFrame frame = new(data);

        WindowCoveringReport report = WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x82, report.Duration.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(3), report.Duration.Duration);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // Only 3 parameter bytes (need 4: paramId, current, target, duration)
        byte[] data = [0x6A, 0x04, 0x01, 0x32, 0x63];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_NoParameters_Throws()
    {
        byte[] data = [0x6A, 0x04];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => WindowCoveringCommandClass.WindowCoveringReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SetCommand_Create_SingleParameter()
    {
        Dictionary<WindowCoveringParameterId, byte> values = new()
        {
            { WindowCoveringParameterId.OutboundLeftPosition, 0x63 },
        };
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));

        WindowCoveringCommandClass.WindowCoveringSetCommand command =
            WindowCoveringCommandClass.WindowCoveringSetCommand.Create(values, duration);

        Assert.AreEqual(CommandClassId.WindowCovering, WindowCoveringCommandClass.WindowCoveringSetCommand.CommandClassId);
        Assert.AreEqual((byte)WindowCoveringCommand.Set, WindowCoveringCommandClass.WindowCoveringSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (count) + 2 (paramId+value) + 1 (duration) = 4 bytes
        Assert.AreEqual(4, parameters.Length);
        // Parameter count = 1
        Assert.AreEqual(0x01, parameters[0] & 0b0001_1111);
        // Reserved bits must be 0
        Assert.AreEqual(0x00, parameters[0] & 0b1110_0000);
        // Parameter ID = OutboundLeftPosition (0x01)
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundLeftPosition, parameters[1]);
        // Value = 0x63
        Assert.AreEqual(0x63, parameters[2]);
        // Duration = 5 seconds
        Assert.AreEqual(0x05, parameters[3]);
    }

    [TestMethod]
    public void SetCommand_Create_MultipleParameters()
    {
        Dictionary<WindowCoveringParameterId, byte> values = new()
        {
            { WindowCoveringParameterId.OutboundBottomPosition, 0x63 },
            { WindowCoveringParameterId.VerticalSlatsAnglePosition, 0x32 },
        };
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(10));

        WindowCoveringCommandClass.WindowCoveringSetCommand command =
            WindowCoveringCommandClass.WindowCoveringSetCommand.Create(values, duration);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (count) + 4 (2 params × 2 bytes) + 1 (duration) = 6 bytes
        Assert.AreEqual(6, parameters.Length);
        // Parameter count = 2
        Assert.AreEqual(0x02, parameters[0] & 0b0001_1111);
    }

    [TestMethod]
    public void SetCommand_Create_FactoryDefaultDuration()
    {
        Dictionary<WindowCoveringParameterId, byte> values = new()
        {
            { WindowCoveringParameterId.OutboundLeftPosition, 0x00 },
        };

        WindowCoveringCommandClass.WindowCoveringSetCommand command =
            WindowCoveringCommandClass.WindowCoveringSetCommand.Create(values, DurationSet.FactoryDefault);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(4, parameters.Length);
        Assert.AreEqual(0xFF, parameters[3]); // Factory default = 0xFF
    }

    [TestMethod]
    public void SetCommand_Create_ZeroDuration()
    {
        Dictionary<WindowCoveringParameterId, byte> values = new()
        {
            { WindowCoveringParameterId.OutboundLeftPosition, 0x63 },
        };
        DurationSet duration = new DurationSet(0x00);

        WindowCoveringCommandClass.WindowCoveringSetCommand command =
            WindowCoveringCommandClass.WindowCoveringSetCommand.Create(values, duration);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, parameters[3]); // Instant
    }

    [TestMethod]
    public void SetCommand_Create_ZeroValue()
    {
        Dictionary<WindowCoveringParameterId, byte> values = new()
        {
            { WindowCoveringParameterId.OutboundBottomPosition, 0x00 },
        };
        DurationSet duration = new DurationSet(0x05);

        WindowCoveringCommandClass.WindowCoveringSetCommand command =
            WindowCoveringCommandClass.WindowCoveringSetCommand.Create(values, duration);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)WindowCoveringParameterId.OutboundBottomPosition, parameters[1]);
        Assert.AreEqual(0x00, parameters[2]);
    }
}
