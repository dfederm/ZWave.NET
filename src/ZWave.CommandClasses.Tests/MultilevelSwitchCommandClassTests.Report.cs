using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultilevelSwitchCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_Version1_NoParameters()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchSetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.Create(1, new GenericValue(50), null);

        Assert.AreEqual(CommandClassId.MultilevelSwitch, MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSwitchCommand.Set, MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(50, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Version1_IgnoresDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));
        MultilevelSwitchCommandClass.MultilevelSwitchSetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.Create(1, new GenericValue(99), duration);

        // V1 does not include duration, even if provided
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(99, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Version2_WithDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(10));
        MultilevelSwitchCommandClass.MultilevelSwitchSetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.Create(2, new GenericValue(75), duration);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(75, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x0A, command.Frame.CommandParameters.Span[1]); // 10 seconds
    }

    [TestMethod]
    public void SetCommand_Create_Version2_NullDuration()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchSetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.Create(2, new GenericValue(0), null);

        // V2 without duration still sends just the value byte
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Version2_FactoryDefaultDuration()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchSetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.Create(2, new GenericValue(true), DurationSet.FactoryDefault);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]); // On
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[1]); // Factory default
    }

    [TestMethod]
    public void SetCommand_Create_OffValue()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchSetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchSetCommand.Create(1, new GenericValue(false), null);

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchGetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchGetCommand.Create();

        Assert.AreEqual(CommandClassId.MultilevelSwitch, MultilevelSwitchCommandClass.MultilevelSwitchGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSwitchCommand.Get, MultilevelSwitchCommandClass.MultilevelSwitchGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Version1_CurrentValueOnly_On()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0xFF (On)
        byte[] data = [0x26, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFF, report.CurrentValue.Value);
        Assert.IsTrue(report.CurrentValue.State);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version1_CurrentValueOnly_Off()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0x00 (Off)
        byte[] data = [0x26, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.CurrentValue.Value);
        Assert.IsFalse(report.CurrentValue.State);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version1_MidLevel()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0x32 (50%)
        byte[] data = [0x26, 0x03, 0x32];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x32, report.CurrentValue.Value);
        Assert.AreEqual(50, report.CurrentValue.Level);
    }

    [TestMethod]
    public void Report_Parse_Version4_WithTargetAndDuration()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0x00, TargetValue=0x63, Duration=0x05 (5 seconds)
        byte[] data = [0x26, 0x03, 0x00, 0x63, 0x05];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.CurrentValue.Value);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0x63, report.TargetValue.Value.Value);
        Assert.IsNotNull(report.Duration);
        Assert.AreEqual((byte)0x05, report.Duration.Value.Value);
        Assert.AreEqual(TimeSpan.FromSeconds(5), report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version4_AlreadyAtTarget()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0x63, TargetValue=0x63, Duration=0x00
        byte[] data = [0x26, 0x03, 0x63, 0x63, 0x00];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x63, report.CurrentValue.Value);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0x63, report.TargetValue.Value.Value);
        Assert.IsNotNull(report.Duration);
        Assert.AreEqual(TimeSpan.Zero, report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version4_UnknownDuration()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0x00, TargetValue=0x63, Duration=0xFE (unknown)
        byte[] data = [0x26, 0x03, 0x00, 0x63, 0xFE];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(report.Duration);
        Assert.AreEqual((byte)0xFE, report.Duration.Value.Value);
        Assert.IsNull(report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version4_PartialPayload_TargetOnly()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0x63, TargetValue=0x00, no duration
        byte[] data = [0x26, 0x03, 0x63, 0x00];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x63, report.CurrentValue.Value);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0x00, report.TargetValue.Value.Value);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x26, Cmd=0x03, no parameters
        byte[] data = [0x26, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_ReservedValue_Preserved()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0xA0 (reserved per spec, but raw byte preserved)
        byte[] data = [0x26, 0x03, 0xA0];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xA0, report.CurrentValue.Value);
    }

    [TestMethod]
    public void Report_Parse_UnknownValue_0xFE()
    {
        // CC=0x26, Cmd=0x03, CurrentValue=0xFE (Unknown per Table 2.418)
        byte[] data = [0x26, 0x03, 0xFE];
        CommandClassFrame frame = new(data);

        MultilevelSwitchReport report = MultilevelSwitchCommandClass.MultilevelSwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFE, report.CurrentValue.Value);
    }
}
