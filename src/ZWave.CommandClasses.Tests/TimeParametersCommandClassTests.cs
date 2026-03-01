using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class TimeParametersCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_HasCorrectFormat()
    {
        DateTime utcTime = new DateTime(2025, 8, 18, 14, 30, 45, DateTimeKind.Utc);
        TimeParametersCommandClass.TimeParametersSetCommand command =
            TimeParametersCommandClass.TimeParametersSetCommand.Create(utcTime);

        Assert.AreEqual(CommandClassId.TimeParameters, TimeParametersCommandClass.TimeParametersSetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeParametersCommand.Set, TimeParametersCommandClass.TimeParametersSetCommand.CommandId);
        Assert.AreEqual(9, command.Frame.Data.Length); // CC + Cmd + 7 params

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        // 2025 = 0x07E9
        Assert.AreEqual(0x07, span[0]);
        Assert.AreEqual(0xE9, span[1]);
        Assert.AreEqual(8, span[2]);
        Assert.AreEqual(18, span[3]);
        Assert.AreEqual(14, span[4]);
        Assert.AreEqual(30, span[5]);
        Assert.AreEqual(45, span[6]);
    }

    [TestMethod]
    public void SetCommand_Create_Year2007()
    {
        DateTime utcTime = new DateTime(2007, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        TimeParametersCommandClass.TimeParametersSetCommand command =
            TimeParametersCommandClass.TimeParametersSetCommand.Create(utcTime);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        // 2007 = 0x07D7
        Assert.AreEqual(0x07, span[0]);
        Assert.AreEqual(0xD7, span[1]);
        Assert.AreEqual(12, span[2]);
        Assert.AreEqual(31, span[3]);
        Assert.AreEqual(23, span[4]);
        Assert.AreEqual(59, span[5]);
        Assert.AreEqual(59, span[6]);
    }

    [TestMethod]
    public void SetCommand_Create_NonUtc_Throws()
    {
        DateTime localTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => TimeParametersCommandClass.TimeParametersSetCommand.Create(localTime));
    }

    [TestMethod]
    public void SetCommand_Create_Unspecified_Throws()
    {
        DateTime unspecifiedTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(
            () => TimeParametersCommandClass.TimeParametersSetCommand.Create(unspecifiedTime));
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        TimeParametersCommandClass.TimeParametersGetCommand command =
            TimeParametersCommandClass.TimeParametersGetCommand.Create();

        Assert.AreEqual(CommandClassId.TimeParameters, TimeParametersCommandClass.TimeParametersGetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeParametersCommand.Get, TimeParametersCommandClass.TimeParametersGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_ValidReport()
    {
        // CC=0x8B, Cmd=0x03, Year=2025 (0x07E9), Month=8, Day=18, Hour=14, Minute=30, Second=45
        byte[] data = [0x8B, 0x03, 0x07, 0xE9, 8, 18, 14, 30, 45];
        CommandClassFrame frame = new(data);

        DateTime result = TimeParametersCommandClass.TimeParametersReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new DateTime(2025, 8, 18, 14, 30, 45, DateTimeKind.Utc), result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    [TestMethod]
    public void Report_Parse_Midnight_NewYear()
    {
        // Year=2026, Month=1, Day=1, Hour=0, Minute=0, Second=0
        byte[] data = [0x8B, 0x03, 0x07, 0xEA, 1, 1, 0, 0, 0];
        CommandClassFrame frame = new(data);

        DateTime result = TimeParametersCommandClass.TimeParametersReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void Report_Parse_EndOfDay()
    {
        byte[] data = [0x8B, 0x03, 0x07, 0xE9, 12, 31, 23, 59, 59];
        CommandClassFrame frame = new(data);

        DateTime result = TimeParametersCommandClass.TimeParametersReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // Only 6 param bytes, need 7
        byte[] data = [0x8B, 0x03, 0x07, 0xE9, 8, 18, 14, 30];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeParametersCommandClass.TimeParametersReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_InvalidMonth_ThrowsZWaveException()
    {
        // Month=13 (invalid)
        byte[] data = [0x8B, 0x03, 0x07, 0xE9, 13, 1, 0, 0, 0];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeParametersCommandClass.TimeParametersReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_InvalidDay_ThrowsZWaveException()
    {
        // Feb 30 (invalid)
        byte[] data = [0x8B, 0x03, 0x07, 0xE9, 2, 30, 0, 0, 0];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeParametersCommandClass.TimeParametersReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_ExtraBytes_Ignored()
    {
        byte[] data = [0x8B, 0x03, 0x07, 0xE9, 8, 18, 14, 30, 45, 0xFF];
        CommandClassFrame frame = new(data);

        DateTime result = TimeParametersCommandClass.TimeParametersReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new DateTime(2025, 8, 18, 14, 30, 45, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void SetCommand_RoundTrips_ThroughReportParse()
    {
        DateTime original = new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        TimeParametersCommandClass.TimeParametersSetCommand setCommand =
            TimeParametersCommandClass.TimeParametersSetCommand.Create(original);

        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x8B;
        reportData[1] = 0x03; // Report command
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        DateTime parsed = TimeParametersCommandClass.TimeParametersReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual(original, parsed);
        Assert.AreEqual(DateTimeKind.Utc, parsed.Kind);
    }
}
