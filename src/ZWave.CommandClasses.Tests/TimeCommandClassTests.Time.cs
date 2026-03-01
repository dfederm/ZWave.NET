using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class TimeCommandClassTests
{
    [TestMethod]
    public void TimeGetCommand_Create_HasCorrectFormat()
    {
        TimeCommandClass.TimeGetCommand command = TimeCommandClass.TimeGetCommand.Create();

        Assert.AreEqual(CommandClassId.Time, TimeCommandClass.TimeGetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeCommand.TimeGet, TimeCommandClass.TimeGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void TimeSetCommand_Create_HasCorrectFormat()
    {
        TimeCommandClass.TimeSetCommand command = TimeCommandClass.TimeSetCommand.Create(new TimeOnly(14, 30, 45));

        Assert.AreEqual(CommandClassId.Time, TimeCommandClass.TimeSetCommand.CommandClassId);
        Assert.AreEqual((byte)TimeCommand.TimeSet, TimeCommandClass.TimeSetCommand.CommandId);
        Assert.AreEqual(5, command.Frame.Data.Length); // CC + Cmd + 3 params
        // Hour=14 in bits 4-0, reserved bits 7-5 = 0 => 0x0E
        Assert.AreEqual(0x0E, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(30, command.Frame.CommandParameters.Span[1]);
        Assert.AreEqual(45, command.Frame.CommandParameters.Span[2]);
    }

    [TestMethod]
    public void TimeSetCommand_Create_Midnight()
    {
        TimeCommandClass.TimeSetCommand command = TimeCommandClass.TimeSetCommand.Create(new TimeOnly(0, 0, 0));

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0, command.Frame.CommandParameters.Span[1]);
        Assert.AreEqual(0, command.Frame.CommandParameters.Span[2]);
    }

    [TestMethod]
    public void TimeSetCommand_Create_EndOfDay()
    {
        TimeCommandClass.TimeSetCommand command = TimeCommandClass.TimeSetCommand.Create(new TimeOnly(23, 59, 59));

        Assert.AreEqual(0x17, command.Frame.CommandParameters.Span[0]); // 23 = 0x17
        Assert.AreEqual(59, command.Frame.CommandParameters.Span[1]);
        Assert.AreEqual(59, command.Frame.CommandParameters.Span[2]);
    }

    [TestMethod]
    public void TimeReport_Parse_V1_NoRtcFailure()
    {
        // CC=0x8A, Cmd=0x02, [RtcFailure=0, Reserved=00, Hour=14] = 0x0E, Minute=30, Second=45
        byte[] data = [0x8A, 0x02, 0x0E, 30, 45];
        CommandClassFrame frame = new(data);

        TimeReport report = TimeCommandClass.TimeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.RtcFailure);
        Assert.AreEqual(TimeSource.ZWave, report.TimeSource);
        Assert.AreEqual(new TimeOnly(14, 30, 45), report.Time);
    }

    [TestMethod]
    public void TimeReport_Parse_WithRtcFailure()
    {
        // RtcFailure=1, Reserved=00, Hour=0 => 0x80
        byte[] data = [0x8A, 0x02, 0x80, 0, 0];
        CommandClassFrame frame = new(data);

        TimeReport report = TimeCommandClass.TimeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.RtcFailure);
        Assert.AreEqual(TimeSource.ZWave, report.TimeSource);
        Assert.AreEqual(new TimeOnly(0, 0, 0), report.Time);
    }

    [TestMethod]
    public void TimeReport_Parse_V3_WithTimeSource_Gps()
    {
        // RtcFailure=0, TimeSource=01 (GPS), Hour=12 => 0b0_01_01100 = 0x2C
        byte[] data = [0x8A, 0x02, 0x2C, 0, 0];
        CommandClassFrame frame = new(data);

        TimeReport report = TimeCommandClass.TimeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.RtcFailure);
        Assert.AreEqual(TimeSource.GpsSatelliteNav, report.TimeSource);
        Assert.AreEqual(new TimeOnly(12, 0, 0), report.Time);
    }

    [TestMethod]
    public void TimeReport_Parse_V3_WithTimeSource_WiFi()
    {
        // RtcFailure=0, TimeSource=10 (WiFi), Hour=23 => 0b0_10_10111 = 0x57
        byte[] data = [0x8A, 0x02, 0x57, 59, 59];
        CommandClassFrame frame = new(data);

        TimeReport report = TimeCommandClass.TimeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.RtcFailure);
        Assert.AreEqual(TimeSource.WiFiInternet, report.TimeSource);
        Assert.AreEqual(new TimeOnly(23, 59, 59), report.Time);
    }

    [TestMethod]
    public void TimeReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x8A, 0x02, 0x0E, 30]; // only 2 param bytes, need 3
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeCommandClass.TimeReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void TimeReport_Parse_ExtraBytes_Ignored()
    {
        byte[] data = [0x8A, 0x02, 0x0E, 30, 45, 0xFF, 0xAA];
        CommandClassFrame frame = new(data);

        TimeReport report = TimeCommandClass.TimeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new TimeOnly(14, 30, 45), report.Time);
    }

    [TestMethod]
    public void TimeReport_Parse_InvalidMinute_ThrowsZWaveException()
    {
        // Hour=0, Minute=255, Second=0 (invalid)
        byte[] data = [0x8A, 0x02, 0x00, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => TimeCommandClass.TimeReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void TimeSetCommand_RoundTrips_ThroughReportParse()
    {
        TimeCommandClass.TimeSetCommand setCommand = TimeCommandClass.TimeSetCommand.Create(new TimeOnly(8, 15, 30));

        // Build a report frame from the same parameter bytes
        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x8A;
        reportData[1] = 0x02; // Report command
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        TimeReport report = TimeCommandClass.TimeReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual(new TimeOnly(8, 15, 30), report.Time);
    }
}
