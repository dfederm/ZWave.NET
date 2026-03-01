using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class TimeCommandClassTests
{
    [TestMethod]
    public void DateGetCommand_Create_HasCorrectFormat()
    {
        TimeCommandClass.DateGetCommand command = TimeCommandClass.DateGetCommand.Create();

        Assert.AreEqual(CommandClassId.Time, TimeCommandClass.DateGetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeCommand.DateGet, TimeCommandClass.DateGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void DateSetCommand_Create_HasCorrectFormat()
    {
        TimeCommandClass.DateSetCommand command = TimeCommandClass.DateSetCommand.Create(new DateOnly(2025, 8, 18));

        Assert.AreEqual(CommandClassId.Time, TimeCommandClass.DateSetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeCommand.DateSet, TimeCommandClass.DateSetCommand.CommandId);
        Assert.AreEqual(6, command.Frame.Data.Length); // CC + Cmd + 4 params

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        // 2025 = 0x07E9
        Assert.AreEqual(0x07, span[0]);
        Assert.AreEqual(0xE9, span[1]);
        Assert.AreEqual(8, span[2]);
        Assert.AreEqual(18, span[3]);
    }

    [TestMethod]
    public void DateSetCommand_Create_Year2007Example()
    {
        // Example from spec: Year1=0x07, Year2=0xD7 => 2007
        TimeCommandClass.DateSetCommand command = TimeCommandClass.DateSetCommand.Create(new DateOnly(2007, 12, 31));

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x07, span[0]);
        Assert.AreEqual(0xD7, span[1]);
        Assert.AreEqual(12, span[2]);
        Assert.AreEqual(31, span[3]);
    }

    [TestMethod]
    public void DateReport_Parse_ValidReport()
    {
        // CC=0x8A, Cmd=0x04, Year=2025 (0x07E9), Month=8, Day=18
        byte[] data = [0x8A, 0x04, 0x07, 0xE9, 8, 18];
        CommandClassFrame frame = new(data);

        DateOnly date = TimeCommandClass.DateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new DateOnly(2025, 8, 18), date);
    }

    [TestMethod]
    public void DateReport_Parse_Year2007Example()
    {
        // From spec: Year1=0x07, Year2=0xD7 => 2007
        byte[] data = [0x8A, 0x04, 0x07, 0xD7, 6, 15];
        CommandClassFrame frame = new(data);

        DateOnly date = TimeCommandClass.DateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new DateOnly(2007, 6, 15), date);
    }

    [TestMethod]
    public void DateReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x8A, 0x04, 0x07, 0xE9, 8]; // only 3 param bytes, need 4
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeCommandClass.DateReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void DateReport_Parse_ExtraBytes_Ignored()
    {
        byte[] data = [0x8A, 0x04, 0x07, 0xE9, 8, 18, 0xFF];
        CommandClassFrame frame = new(data);

        DateOnly date = TimeCommandClass.DateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new DateOnly(2025, 8, 18), date);
    }

    [TestMethod]
    public void DateReport_Parse_InvalidMonth_ThrowsZWaveException()
    {
        // Year=2025, Month=13 (invalid), Day=1
        byte[] data = [0x8A, 0x04, 0x07, 0xE9, 13, 1];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeCommandClass.DateReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void DateSetCommand_RoundTrips_ThroughReportParse()
    {
        TimeCommandClass.DateSetCommand setCommand = TimeCommandClass.DateSetCommand.Create(new DateOnly(2025, 3, 1));

        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x8A;
        reportData[1] = 0x04; // Date Report
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        DateOnly date = TimeCommandClass.DateReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual(new DateOnly(2025, 3, 1), date);
    }
}
