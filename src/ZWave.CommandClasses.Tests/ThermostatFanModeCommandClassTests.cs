using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class ThermostatFanModeCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_AutoLow_NotOff()
    {
        ThermostatFanModeCommandClass.ThermostatFanModeSetCommand command =
            ThermostatFanModeCommandClass.ThermostatFanModeSetCommand.Create(2, ThermostatFanMode.AutoLow, off: false);

        Assert.AreEqual(CommandClassId.ThermostatFanMode, ThermostatFanModeCommandClass.ThermostatFanModeSetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatFanModeCommand.Set, ThermostatFanModeCommandClass.ThermostatFanModeSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        // Bit 7 = Off (0), bits 3-0 = 0x00 (AutoLow)
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Low_NotOff()
    {
        ThermostatFanModeCommandClass.ThermostatFanModeSetCommand command =
            ThermostatFanModeCommandClass.ThermostatFanModeSetCommand.Create(2, ThermostatFanMode.Low, off: false);

        Assert.AreEqual(3, command.Frame.Data.Length);
        // Bit 7 = Off (0), bits 3-0 = 0x01 (Low)
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_High_WithOff()
    {
        ThermostatFanModeCommandClass.ThermostatFanModeSetCommand command =
            ThermostatFanModeCommandClass.ThermostatFanModeSetCommand.Create(2, ThermostatFanMode.High, off: true);

        Assert.AreEqual(3, command.Frame.Data.Length);
        // Bit 7 = Off (1), bits 3-0 = 0x03 (High) → 0x83
        Assert.AreEqual(0x83, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_ExternalCirculation_NotOff()
    {
        ThermostatFanModeCommandClass.ThermostatFanModeSetCommand command =
            ThermostatFanModeCommandClass.ThermostatFanModeSetCommand.Create(5, ThermostatFanMode.ExternalCirculation, off: false);

        Assert.AreEqual(3, command.Frame.Data.Length);
        // Bit 7 = Off (0), bits 3-0 = 0x0B (ExternalCirculation)
        Assert.AreEqual(0x0B, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_Version1_OffIgnored()
    {
        // V1 does not have the Off bit; reserved bits MUST be 0
        ThermostatFanModeCommandClass.ThermostatFanModeSetCommand command =
            ThermostatFanModeCommandClass.ThermostatFanModeSetCommand.Create(1, ThermostatFanMode.High, off: true);

        Assert.AreEqual(3, command.Frame.Data.Length);
        // Off is ignored for V1, so bit 7 = 0, bits 3-0 = 0x03 (High)
        Assert.AreEqual(0x03, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ThermostatFanModeCommandClass.ThermostatFanModeGetCommand command =
            ThermostatFanModeCommandClass.ThermostatFanModeGetCommand.Create();

        Assert.AreEqual(CommandClassId.ThermostatFanMode, ThermostatFanModeCommandClass.ThermostatFanModeGetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatFanModeCommand.Get, ThermostatFanModeCommandClass.ThermostatFanModeGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_AutoLow_NotOff()
    {
        // CC=0x44, Cmd=0x03, Value=0x00 (Off=0, Mode=AutoLow)
        byte[] data = [0x44, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        ThermostatFanModeReport report =
            ThermostatFanModeCommandClass.ThermostatFanModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanMode.AutoLow, report.FanMode);
        Assert.IsFalse(report.Off);
    }

    [TestMethod]
    public void Report_Parse_Low_NotOff()
    {
        // CC=0x44, Cmd=0x03, Value=0x01 (Off=0, Mode=Low)
        byte[] data = [0x44, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        ThermostatFanModeReport report =
            ThermostatFanModeCommandClass.ThermostatFanModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanMode.Low, report.FanMode);
        Assert.IsFalse(report.Off);
    }

    [TestMethod]
    public void Report_Parse_High_WithOff()
    {
        // CC=0x44, Cmd=0x03, Value=0x83 (Off=1, Mode=High)
        byte[] data = [0x44, 0x03, 0x83];
        CommandClassFrame frame = new(data);

        ThermostatFanModeReport report =
            ThermostatFanModeCommandClass.ThermostatFanModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanMode.High, report.FanMode);
        Assert.IsTrue(report.Off);
    }

    [TestMethod]
    public void Report_Parse_Circulation_NotOff()
    {
        // CC=0x44, Cmd=0x03, Value=0x06 (Off=0, Mode=Circulation)
        byte[] data = [0x44, 0x03, 0x06];
        CommandClassFrame frame = new(data);

        ThermostatFanModeReport report =
            ThermostatFanModeCommandClass.ThermostatFanModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanMode.Circulation, report.FanMode);
        Assert.IsFalse(report.Off);
    }

    [TestMethod]
    public void Report_Parse_ExternalCirculation_WithOff()
    {
        // CC=0x44, Cmd=0x03, Value=0x8B (Off=1, Mode=ExternalCirculation=0x0B)
        byte[] data = [0x44, 0x03, 0x8B];
        CommandClassFrame frame = new(data);

        ThermostatFanModeReport report =
            ThermostatFanModeCommandClass.ThermostatFanModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanMode.ExternalCirculation, report.FanMode);
        Assert.IsTrue(report.Off);
    }

    [TestMethod]
    public void Report_Parse_ReservedMode_Preserved()
    {
        // CC=0x44, Cmd=0x03, Value=0x0E (Off=0, Mode=0x0E reserved)
        byte[] data = [0x44, 0x03, 0x0E];
        CommandClassFrame frame = new(data);

        ThermostatFanModeReport report =
            ThermostatFanModeCommandClass.ThermostatFanModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ThermostatFanMode)0x0E, report.FanMode);
        Assert.IsFalse(report.Off);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x44, Cmd=0x03, no parameters
        byte[] data = [0x44, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ThermostatFanModeCommandClass.ThermostatFanModeReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        ThermostatFanModeCommandClass.ThermostatFanModeSupportedGetCommand command =
            ThermostatFanModeCommandClass.ThermostatFanModeSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.ThermostatFanMode, ThermostatFanModeCommandClass.ThermostatFanModeSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatFanModeCommand.SupportedGet, ThermostatFanModeCommandClass.ThermostatFanModeSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_AutoLowAutoHighAutoMedium()
    {
        // Per spec example: bits 0, 2, 4 set = AutoLow(0), AutoHigh(2), AutoMedium(4)
        // CC=0x44, Cmd=0x05, BitMask=0b0001_0101 = 0x15
        byte[] data = [0x44, 0x05, 0x15];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatFanMode> supported =
            ThermostatFanModeCommandClass.ThermostatFanModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, supported);
        Assert.Contains(ThermostatFanMode.AutoLow, supported);
        Assert.Contains(ThermostatFanMode.AutoHigh, supported);
        Assert.Contains(ThermostatFanMode.AutoMedium, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_LowAndHigh()
    {
        // Bits 1 and 3 set = Low(1) and High(3)
        // CC=0x44, Cmd=0x05, BitMask=0b0000_1010 = 0x0A
        byte[] data = [0x44, 0x05, 0x0A];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatFanMode> supported =
            ThermostatFanModeCommandClass.ThermostatFanModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(ThermostatFanMode.Low, supported);
        Assert.Contains(ThermostatFanMode.High, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TwoBytes_IncludesHighModes()
    {
        // BitMask1: 0b0000_0001 (bit 0 = AutoLow)
        // BitMask2: 0b0000_0111 (bits 8, 9, 10 = LeftRight, UpDown, Quiet)
        byte[] data = [0x44, 0x05, 0x01, 0x07];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatFanMode> supported =
            ThermostatFanModeCommandClass.ThermostatFanModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(4, supported);
        Assert.Contains(ThermostatFanMode.AutoLow, supported);
        Assert.Contains(ThermostatFanMode.LeftRight, supported);
        Assert.Contains(ThermostatFanMode.UpDown, supported);
        Assert.Contains(ThermostatFanMode.Quiet, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_ExternalCirculation()
    {
        // BitMask1: 0x00, BitMask2: 0b0000_1000 (bit 11 = ExternalCirculation)
        byte[] data = [0x44, 0x05, 0x00, 0x08];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatFanMode> supported =
            ThermostatFanModeCommandClass.ThermostatFanModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, supported);
        Assert.Contains(ThermostatFanMode.ExternalCirculation, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_EmptyBitmask_ReturnsEmpty()
    {
        // CC=0x44, Cmd=0x05, no bitmask bytes
        byte[] data = [0x44, 0x05];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatFanMode> supported =
            ThermostatFanModeCommandClass.ThermostatFanModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_AllZeros_ReturnsEmpty()
    {
        // CC=0x44, Cmd=0x05, BitMask=0x00
        byte[] data = [0x44, 0x05, 0x00];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatFanMode> supported =
            ThermostatFanModeCommandClass.ThermostatFanModeSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }
}
