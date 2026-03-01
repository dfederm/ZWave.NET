using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class TimeCommandClassTests
{
    [TestMethod]
    public void TimeOffsetGetCommand_Create_HasCorrectFormat()
    {
        TimeCommandClass.TimeOffsetGetCommand command = TimeCommandClass.TimeOffsetGetCommand.Create();

        Assert.AreEqual(CommandClassId.Time, TimeCommandClass.TimeOffsetGetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeCommand.TimeOffsetGet, TimeCommandClass.TimeOffsetGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void TimeOffsetSetCommand_Create_PositiveTzo_PositiveDst()
    {
        // UTC+5:30, DST +60 min, start March 10 02:00, end Nov 3 02:00
        TimeOffsetReport offset = new(
            TimeZoneOffset: TimeSpan.FromMinutes(330),
            DstOffset: TimeSpan.FromMinutes(60),
            DstStart: new DstTransition(3, 10, 2),
            DstEnd: new DstTransition(11, 3, 2));

        TimeCommandClass.TimeOffsetSetCommand command = TimeCommandClass.TimeOffsetSetCommand.Create(offset);

        Assert.AreEqual(CommandClassId.Time, TimeCommandClass.TimeOffsetSetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeCommand.TimeOffsetSet, TimeCommandClass.TimeOffsetSetCommand.CommandId);
        Assert.AreEqual(11, command.Frame.Data.Length); // CC + Cmd + 9 params

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        // Sign=0, Hour=5 => 0x05
        Assert.AreEqual(0x05, span[0]);
        Assert.AreEqual(30, span[1]); // Minute TZO
        // Sign=0, MinuteDst=60 => 0x3C
        Assert.AreEqual(0x3C, span[2]);
        Assert.AreEqual(3, span[3]);  // DST start month
        Assert.AreEqual(10, span[4]); // DST start day
        Assert.AreEqual(2, span[5]);  // DST start hour
        Assert.AreEqual(11, span[6]); // DST end month
        Assert.AreEqual(3, span[7]);  // DST end day
        Assert.AreEqual(2, span[8]);  // DST end hour
    }

    [TestMethod]
    public void TimeOffsetSetCommand_Create_NegativeTzo()
    {
        // UTC-8:00, no DST
        TimeOffsetReport offset = new(
            TimeZoneOffset: TimeSpan.FromHours(-8),
            DstOffset: TimeSpan.Zero,
            DstStart: new DstTransition(0, 0, 0),
            DstEnd: new DstTransition(0, 0, 0));

        TimeCommandClass.TimeOffsetSetCommand command = TimeCommandClass.TimeOffsetSetCommand.Create(offset);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        // Sign=1, Hour=8 => 0x88
        Assert.AreEqual(0x88, span[0]);
        Assert.AreEqual(0, span[1]); // Minute TZO
        Assert.AreEqual(0, span[2]); // No DST
    }

    [TestMethod]
    public void TimeOffsetSetCommand_Create_NegativeDst()
    {
        // UTC+0, DST -60 min
        TimeOffsetReport offset = new(
            TimeZoneOffset: TimeSpan.Zero,
            DstOffset: TimeSpan.FromMinutes(-60),
            DstStart: new DstTransition(10, 1, 2),
            DstEnd: new DstTransition(3, 1, 3));

        TimeCommandClass.TimeOffsetSetCommand command = TimeCommandClass.TimeOffsetSetCommand.Create(offset);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, span[0]); // Sign=0, Hour=0
        Assert.AreEqual(0, span[1]);    // Minute TZO
        // Sign=1, MinuteDst=60 => 0x80 | 0x3C = 0xBC
        Assert.AreEqual(0xBC, span[2]);
    }

    [TestMethod]
    public void TimeOffsetReport_Parse_PositiveTzo_PositiveDst()
    {
        // CC=0x8A, Cmd=0x07
        // Sign=0, HourTZO=5 (0x05), MinuteTZO=30
        // Sign=0, MinuteDST=60 (0x3C)
        // DstStart: March 10 02:00, DstEnd: Nov 3 02:00
        byte[] data = [0x8A, 0x07, 0x05, 30, 0x3C, 3, 10, 2, 11, 3, 2];
        CommandClassFrame frame = new(data);

        TimeOffsetReport report = TimeCommandClass.TimeOffsetReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromMinutes(330), report.TimeZoneOffset);
        Assert.AreEqual(TimeSpan.FromMinutes(60), report.DstOffset);
        Assert.AreEqual((byte)3, report.DstStart.Month);
        Assert.AreEqual((byte)10, report.DstStart.Day);
        Assert.AreEqual((byte)2, report.DstStart.Hour);
        Assert.AreEqual((byte)11, report.DstEnd.Month);
        Assert.AreEqual((byte)3, report.DstEnd.Day);
        Assert.AreEqual((byte)2, report.DstEnd.Hour);
    }

    [TestMethod]
    public void TimeOffsetReport_Parse_NegativeTzo()
    {
        // Sign=1, HourTZO=8 (0x88), MinuteTZO=0, no DST
        byte[] data = [0x8A, 0x07, 0x88, 0, 0x00, 0, 0, 0, 0, 0, 0];
        CommandClassFrame frame = new(data);

        TimeOffsetReport report = TimeCommandClass.TimeOffsetReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromHours(-8), report.TimeZoneOffset);
        Assert.AreEqual(TimeSpan.Zero, report.DstOffset);
    }

    [TestMethod]
    public void TimeOffsetReport_Parse_NegativeDst()
    {
        // Sign=0, HourTZO=0, MinuteTZO=0
        // Sign=1, MinuteDST=60 => 0xBC
        byte[] data = [0x8A, 0x07, 0x00, 0, 0xBC, 10, 1, 2, 3, 1, 3];
        CommandClassFrame frame = new(data);

        TimeOffsetReport report = TimeCommandClass.TimeOffsetReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.Zero, report.TimeZoneOffset);
        Assert.AreEqual(TimeSpan.FromMinutes(-60), report.DstOffset);
        Assert.AreEqual((byte)10, report.DstStart.Month);
    }

    [TestMethod]
    public void TimeOffsetReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x8A, 0x07, 0x05, 30, 0x3C, 3, 10, 2, 11]; // 7 param bytes, need 9
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeCommandClass.TimeOffsetReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void TimeOffsetSetCommand_RoundTrips_ThroughReportParse()
    {
        TimeOffsetReport original = new(
            TimeZoneOffset: TimeSpan.FromMinutes(-330),
            DstOffset: TimeSpan.FromMinutes(60),
            DstStart: new DstTransition(3, 14, 2),
            DstEnd: new DstTransition(11, 7, 2));

        TimeCommandClass.TimeOffsetSetCommand setCommand = TimeCommandClass.TimeOffsetSetCommand.Create(original);

        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x8A;
        reportData[1] = 0x07; // Time Offset Report
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        TimeOffsetReport parsed = TimeCommandClass.TimeOffsetReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual(original.TimeZoneOffset, parsed.TimeZoneOffset);
        Assert.AreEqual(original.DstOffset, parsed.DstOffset);
        Assert.AreEqual(original.DstStart, parsed.DstStart);
        Assert.AreEqual(original.DstEnd, parsed.DstEnd);
    }

    [TestMethod]
    public void TimeOffsetReport_Parse_ExtraBytes_Ignored()
    {
        byte[] data = [0x8A, 0x07, 0x05, 30, 0x3C, 3, 10, 2, 11, 3, 2, 0xFF];
        CommandClassFrame frame = new(data);

        TimeOffsetReport report = TimeCommandClass.TimeOffsetReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromMinutes(330), report.TimeZoneOffset);
    }

    [TestMethod]
    public void TimeOffsetSetCommand_Create_ZeroOffset()
    {
        TimeOffsetReport offset = new(
            TimeZoneOffset: TimeSpan.Zero,
            DstOffset: TimeSpan.Zero,
            DstStart: new DstTransition(0, 0, 0),
            DstEnd: new DstTransition(0, 0, 0));

        TimeCommandClass.TimeOffsetSetCommand command = TimeCommandClass.TimeOffsetSetCommand.Create(offset);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        for (int i = 0; i < 9; i++)
        {
            Assert.AreEqual(0, span[i], $"Byte {i} should be zero");
        }
    }

    [TestMethod]
    public void TimeOffsetSetCommand_Create_TzoHourOverflow_Throws()
    {
        TimeOffsetReport offset = new(
            TimeZoneOffset: TimeSpan.FromHours(128),
            DstOffset: TimeSpan.Zero,
            DstStart: new DstTransition(0, 0, 0),
            DstEnd: new DstTransition(0, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TimeCommandClass.TimeOffsetSetCommand.Create(offset));
    }

    [TestMethod]
    public void TimeOffsetSetCommand_Create_DstMinuteOverflow_Throws()
    {
        TimeOffsetReport offset = new(
            TimeZoneOffset: TimeSpan.Zero,
            DstOffset: TimeSpan.FromMinutes(128),
            DstStart: new DstTransition(0, 0, 0),
            DstEnd: new DstTransition(0, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TimeCommandClass.TimeOffsetSetCommand.Create(offset));
    }
}
