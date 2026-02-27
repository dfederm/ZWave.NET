using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class BinarySwitchCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_OnValue_Version1()
    {
        BinarySwitchCommandClass.BinarySwitchSetCommand command =
            BinarySwitchCommandClass.BinarySwitchSetCommand.Create(1, true, null);

        Assert.AreEqual(CommandClassId.BinarySwitch, BinarySwitchCommandClass.BinarySwitchSetCommand.CommandClassId);
        Assert.AreEqual((byte)BinarySwitchCommand.Set, BinarySwitchCommandClass.BinarySwitchSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_OffValue_Version1()
    {
        BinarySwitchCommandClass.BinarySwitchSetCommand command =
            BinarySwitchCommandClass.BinarySwitchSetCommand.Create(1, false, null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Version1_IgnoresDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));
        BinarySwitchCommandClass.BinarySwitchSetCommand command =
            BinarySwitchCommandClass.BinarySwitchSetCommand.Create(1, true, duration);

        // V1 does not include duration, even if provided
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Version2_WithDuration()
    {
        DurationSet duration = new DurationSet(TimeSpan.FromSeconds(5));
        BinarySwitchCommandClass.BinarySwitchSetCommand command =
            BinarySwitchCommandClass.BinarySwitchSetCommand.Create(2, true, duration);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x05, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_Version2_NullDuration()
    {
        BinarySwitchCommandClass.BinarySwitchSetCommand command =
            BinarySwitchCommandClass.BinarySwitchSetCommand.Create(2, false, null);

        // V2 without duration still sends just the value byte
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Version2_FactoryDefaultDuration()
    {
        BinarySwitchCommandClass.BinarySwitchSetCommand command =
            BinarySwitchCommandClass.BinarySwitchSetCommand.Create(2, true, DurationSet.FactoryDefault);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        BinarySwitchCommandClass.BinarySwitchGetCommand command =
            BinarySwitchCommandClass.BinarySwitchGetCommand.Create();

        Assert.AreEqual(CommandClassId.BinarySwitch, BinarySwitchCommandClass.BinarySwitchGetCommand.CommandClassId);
        Assert.AreEqual((byte)BinarySwitchCommand.Get, BinarySwitchCommandClass.BinarySwitchGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Version1_CurrentValueOnly_On()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0xFF (On)
        byte[] data = [0x25, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFF, report.CurrentValue.Value);
        Assert.IsTrue(report.CurrentValue.State);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version1_CurrentValueOnly_Off()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0x00 (Off)
        byte[] data = [0x25, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.CurrentValue.Value);
        Assert.IsFalse(report.CurrentValue.State);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version1_UnknownValue()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0xFE (Unknown)
        byte[] data = [0x25, 0x03, 0xFE];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFE, report.CurrentValue.Value);
        Assert.IsNull(report.CurrentValue.State);
        Assert.IsNull(report.TargetValue);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version2_WithTargetAndDuration()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0x00, TargetValue=0xFF, Duration=0x05 (5 seconds)
        byte[] data = [0x25, 0x03, 0x00, 0xFF, 0x05];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.CurrentValue.Value);
        Assert.IsFalse(report.CurrentValue.State);
        Assert.IsNotNull(report.TargetValue);
        Assert.AreEqual((byte)0xFF, report.TargetValue.Value.Value);
        Assert.IsTrue(report.TargetValue.Value.State);
        Assert.IsNotNull(report.Duration);
        Assert.AreEqual((byte)0x05, report.Duration.Value.Value);
        Assert.AreEqual(TimeSpan.FromSeconds(5), report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version2_AlreadyAtTarget()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0xFF, TargetValue=0xFF, Duration=0x00 (already there)
        byte[] data = [0x25, 0x03, 0xFF, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.CurrentValue.State);
        Assert.IsNotNull(report.TargetValue);
        Assert.IsTrue(report.TargetValue.Value.State);
        Assert.IsNotNull(report.Duration);
        Assert.AreEqual(TimeSpan.Zero, report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version2_UnknownDuration()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0x00, TargetValue=0xFF, Duration=0xFE (unknown)
        byte[] data = [0x25, 0x03, 0x00, 0xFF, 0xFE];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(report.Duration);
        Assert.AreEqual((byte)0xFE, report.Duration.Value.Value);
        Assert.IsNull(report.Duration.Value.Duration);
    }

    [TestMethod]
    public void Report_Parse_Version2_PartialPayload_TargetOnly()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0xFF, TargetValue=0x00, no duration
        byte[] data = [0x25, 0x03, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.CurrentValue.State);
        Assert.IsNotNull(report.TargetValue);
        Assert.IsFalse(report.TargetValue.Value.State);
        Assert.IsNull(report.Duration);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x25, Cmd=0x03, no parameters
        byte[] data = [0x25, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_ReservedValue_Preserved()
    {
        // CC=0x25, Cmd=0x03, CurrentValue=0x50 (reserved per spec, but raw byte preserved)
        byte[] data = [0x25, 0x03, 0x50];
        CommandClassFrame frame = new(data);

        BinarySwitchReport report = BinarySwitchCommandClass.BinarySwitchReportCommand.Parse(frame, NullLogger.Instance);

        // Raw byte is preserved
        Assert.AreEqual((byte)0x50, report.CurrentValue.Value);
    }
}
