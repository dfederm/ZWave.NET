using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class ClockCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_HasCorrectFormat()
    {
        ClockCommandClass.ClockSetCommand command = ClockCommandClass.ClockSetCommand.Create(
            ClockWeekday.Wednesday,
            new TimeOnly(14, 30));

        Assert.AreEqual(CommandClassId.Clock, ClockCommandClass.ClockSetCommand.CommandClassId);
        Assert.AreEqual((byte)ClockCommand.Set, ClockCommandClass.ClockSetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length); // CC + Cmd + 2 params
        // Weekday=3 (Wednesday) << 5 = 0x60, Hour=14 = 0x0E => 0x6E
        Assert.AreEqual(0x6E, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(30, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_MondayMidnight()
    {
        ClockCommandClass.ClockSetCommand command = ClockCommandClass.ClockSetCommand.Create(
            ClockWeekday.Monday,
            new TimeOnly(0, 0));

        // Weekday=1 (Monday) << 5 = 0x20, Hour=0 => 0x20
        Assert.AreEqual(0x20, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_SundayEndOfDay()
    {
        ClockCommandClass.ClockSetCommand command = ClockCommandClass.ClockSetCommand.Create(
            ClockWeekday.Sunday,
            new TimeOnly(23, 59));

        // Weekday=7 (Sunday) << 5 = 0xE0, Hour=23 = 0x17 => 0xF7
        Assert.AreEqual(0xF7, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(59, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_UnknownWeekday()
    {
        ClockCommandClass.ClockSetCommand command = ClockCommandClass.ClockSetCommand.Create(
            ClockWeekday.Unknown,
            new TimeOnly(12, 0));

        // Weekday=0 << 5 = 0x00, Hour=12 = 0x0C => 0x0C
        Assert.AreEqual(0x0C, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ClockCommandClass.ClockGetCommand command = ClockCommandClass.ClockGetCommand.Create();

        Assert.AreEqual(CommandClassId.Clock, ClockCommandClass.ClockGetCommand.CommandClassId);
        Assert.AreEqual((byte)ClockCommand.Get, ClockCommandClass.ClockGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void Report_Parse_ValidReport()
    {
        // CC=0x81, Cmd=0x06, Weekday=Wednesday(3)<<5 | Hour=14 = 0x6E, Minute=30
        byte[] data = [0x81, 0x06, 0x6E, 30];
        CommandClassFrame frame = new(data);

        ClockReport report = ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ClockWeekday.Wednesday, report.Weekday);
        Assert.AreEqual(new TimeOnly(14, 30), report.Time);
    }

    [TestMethod]
    public void Report_Parse_MondayMidnight()
    {
        // Weekday=Monday(1)<<5 | Hour=0 = 0x20, Minute=0
        byte[] data = [0x81, 0x06, 0x20, 0x00];
        CommandClassFrame frame = new(data);

        ClockReport report = ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ClockWeekday.Monday, report.Weekday);
        Assert.AreEqual(new TimeOnly(0, 0), report.Time);
    }

    [TestMethod]
    public void Report_Parse_SundayEndOfDay()
    {
        // Weekday=Sunday(7)<<5 | Hour=23 = 0xF7, Minute=59
        byte[] data = [0x81, 0x06, 0xF7, 59];
        CommandClassFrame frame = new(data);

        ClockReport report = ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ClockWeekday.Sunday, report.Weekday);
        Assert.AreEqual(new TimeOnly(23, 59), report.Time);
    }

    [TestMethod]
    public void Report_Parse_UnknownWeekday()
    {
        // Weekday=Unknown(0)<<5 | Hour=12 = 0x0C, Minute=0
        byte[] data = [0x81, 0x06, 0x0C, 0x00];
        CommandClassFrame frame = new(data);

        ClockReport report = ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ClockWeekday.Unknown, report.Weekday);
        Assert.AreEqual(new TimeOnly(12, 0), report.Time);
    }

    [TestMethod]
    public void Report_Parse_TooShort_ZeroBytes_Throws()
    {
        // CC=0x81, Cmd=0x06, no parameters
        byte[] data = [0x81, 0x06];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_TooShort_OneByte_Throws()
    {
        // CC=0x81, Cmd=0x06, only 1 param byte (need 2)
        byte[] data = [0x81, 0x06, 0x20];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_SaturdayAfternoon()
    {
        // Weekday=Saturday(6)<<5 | Hour=15 = 0xCF, Minute=45
        byte[] data = [0x81, 0x06, 0xCF, 45];
        CommandClassFrame frame = new(data);

        ClockReport report = ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ClockWeekday.Saturday, report.Weekday);
        Assert.AreEqual(new TimeOnly(15, 45), report.Time);
    }

    [TestMethod]
    public void SetCommand_RoundTrips_ThroughReportParse()
    {
        ClockCommandClass.ClockSetCommand setCommand = ClockCommandClass.ClockSetCommand.Create(
            ClockWeekday.Friday,
            new TimeOnly(8, 15));

        // Build a report frame from the same parameter bytes
        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x81; // CC
        reportData[1] = 0x06; // Report command
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        ClockReport report = ClockCommandClass.ClockReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual(ClockWeekday.Friday, report.Weekday);
        Assert.AreEqual(new TimeOnly(8, 15), report.Time);
    }

    [TestMethod]
    public void Report_Parse_AllWeekdays()
    {
        ClockWeekday[] weekdays =
        [
            ClockWeekday.Unknown,
            ClockWeekday.Monday,
            ClockWeekday.Tuesday,
            ClockWeekday.Wednesday,
            ClockWeekday.Thursday,
            ClockWeekday.Friday,
            ClockWeekday.Saturday,
            ClockWeekday.Sunday,
        ];

        for (int i = 0; i < weekdays.Length; i++)
        {
            byte firstByte = (byte)(i << 5); // weekday in bits 7-5, hour=0
            byte[] data = [0x81, 0x06, firstByte, 0x00];
            CommandClassFrame frame = new(data);

            ClockReport report = ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance);

            Assert.AreEqual(weekdays[i], report.Weekday, $"Failed for weekday index {i}");
        }
    }

    [TestMethod]
    public void Report_Parse_ExtraBytes_Ignored()
    {
        // Extra trailing bytes should be ignored (forward compatibility)
        byte[] data = [0x81, 0x06, 0x6E, 30, 0xFF, 0xAA];
        CommandClassFrame frame = new(data);

        ClockReport report = ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ClockWeekday.Wednesday, report.Weekday);
        Assert.AreEqual(new TimeOnly(14, 30), report.Time);
    }

    [TestMethod]
    public void Report_Parse_InvalidMinute_ThrowsZWaveException()
    {
        // Hour=0, Minute=255 (invalid)
        byte[] data = [0x81, 0x06, 0x00, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_InvalidHour_ThrowsZWaveException()
    {
        // Weekday=0, Hour=31 (5 bits all set, exceeds 0-23 range), Minute=0
        byte[] data = [0x81, 0x06, 0x1F, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ClockCommandClass.ClockReportCommand.Parse(frame, NullLogger.Instance));
    }
}
