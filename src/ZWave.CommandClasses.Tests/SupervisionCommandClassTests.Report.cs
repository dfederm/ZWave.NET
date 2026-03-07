using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class SupervisionCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        // Encapsulate a Basic Set (0x20, 0x01, 0xFF) with session ID 5, no status updates
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x01, [0xFF]);
        CommandClassFrame getFrame = SupervisionCommandClass.CreateGet(statusUpdates: false, sessionId: 5, innerFrame);

        Assert.AreEqual(CommandClassId.Supervision, getFrame.CommandClassId);
        Assert.AreEqual((byte)SupervisionCommand.Get, getFrame.CommandId);

        ReadOnlySpan<byte> parameters = getFrame.CommandParameters.Span;
        // byte 0: StatusUpdates(0) | Reserved(0) | SessionID(5) = 0x05
        Assert.AreEqual((byte)0x05, parameters[0]);
        // byte 1: Encapsulated command length = 3 (CC + Cmd + param)
        Assert.AreEqual((byte)3, parameters[1]);
        // bytes 2..4: Encapsulated command
        Assert.AreEqual((byte)0x20, parameters[2]); // Basic CC
        Assert.AreEqual((byte)0x01, parameters[3]); // Set
        Assert.AreEqual((byte)0xFF, parameters[4]); // Value
    }

    [TestMethod]
    public void GetCommand_Create_WithStatusUpdates()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x01, [0xFF]);
        CommandClassFrame getFrame = SupervisionCommandClass.CreateGet(statusUpdates: true, sessionId: 10, innerFrame);

        ReadOnlySpan<byte> parameters = getFrame.CommandParameters.Span;
        // byte 0: StatusUpdates(1) | Reserved(0) | SessionID(10) = 0x80 | 0x0A = 0x8A
        Assert.AreEqual((byte)0x8A, parameters[0]);
    }

    [TestMethod]
    public void GetCommand_Create_MaxSessionId()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x01, [0xFF]);
        CommandClassFrame getFrame = SupervisionCommandClass.CreateGet(statusUpdates: false, sessionId: 63, innerFrame);

        ReadOnlySpan<byte> parameters = getFrame.CommandParameters.Span;
        Assert.AreEqual((byte)63, parameters[0] & 0x3F);
    }

    [TestMethod]
    public void GetCommand_Create_RejectsSessionIdAbove63()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x01, [0xFF]);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SupervisionCommandClass.CreateGet(statusUpdates: false, sessionId: 64, innerFrame));
    }

    [TestMethod]
    public void GetCommand_Parse_NoStatusUpdates()
    {
        // CC=0x6C, Cmd=0x01, Flags=0x05 (sessionId=5, statusUpdates=0), Length=3, Basic Set
        byte[] data = [0x6C, 0x01, 0x05, 0x03, 0x20, 0x01, 0xFF];
        CommandClassFrame frame = new(data);

        SupervisionGet get = SupervisionCommandClass.ParseGet(frame, NullLogger.Instance);

        Assert.IsFalse(get.StatusUpdates);
        Assert.AreEqual((byte)5, get.SessionId);
        Assert.AreEqual(CommandClassId.Basic, get.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x01, get.EncapsulatedFrame.CommandId);
        Assert.AreEqual(1, get.EncapsulatedFrame.CommandParameters.Length);
        Assert.AreEqual((byte)0xFF, get.EncapsulatedFrame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Parse_WithStatusUpdates()
    {
        // StatusUpdates=1, sessionId=42 → 0x80 | 0x2A = 0xAA
        byte[] data = [0x6C, 0x01, 0xAA, 0x03, 0x20, 0x01, 0xFF];
        CommandClassFrame frame = new(data);

        SupervisionGet get = SupervisionCommandClass.ParseGet(frame, NullLogger.Instance);

        Assert.IsTrue(get.StatusUpdates);
        Assert.AreEqual((byte)42, get.SessionId);
    }

    [TestMethod]
    public void GetCommand_Parse_IgnoresReservedBit()
    {
        // Reserved bit (bit 6) set: 0x45 = 0100_0101 → sessionId=5, statusUpdates=0, reserved=1
        byte[] data = [0x6C, 0x01, 0x45, 0x03, 0x20, 0x01, 0xFF];
        CommandClassFrame frame = new(data);

        SupervisionGet get = SupervisionCommandClass.ParseGet(frame, NullLogger.Instance);

        Assert.IsFalse(get.StatusUpdates);
        Assert.AreEqual((byte)5, get.SessionId);
    }

    [TestMethod]
    public void GetCommand_Parse_TooShort_Throws()
    {
        // Only 2 parameter bytes (need at least 3: flags + length + 1 byte encapsulated)
        byte[] data = [0x6C, 0x01, 0x05, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => SupervisionCommandClass.ParseGet(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void GetCommand_Parse_LengthMismatch_Throws()
    {
        // Claims 5 bytes of encapsulated data but only 3 available
        byte[] data = [0x6C, 0x01, 0x05, 0x05, 0x20, 0x01, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => SupervisionCommandClass.ParseGet(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void GetCommand_RoundTrip_PreservesInnerFrame()
    {
        byte[] innerParams = [0xFF, 0x01, 0x00, 0x05];
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01, innerParams);

        CommandClassFrame getFrame = SupervisionCommandClass.CreateGet(statusUpdates: true, sessionId: 33, innerFrame);
        SupervisionGet parsed = SupervisionCommandClass.ParseGet(getFrame, NullLogger.Instance);

        Assert.IsTrue(parsed.StatusUpdates);
        Assert.AreEqual((byte)33, parsed.SessionId);
        Assert.AreEqual(CommandClassId.BinarySwitch, parsed.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x01, parsed.EncapsulatedFrame.CommandId);
        Assert.IsTrue(innerParams.AsSpan().SequenceEqual(parsed.EncapsulatedFrame.CommandParameters.Span));
    }

    [TestMethod]
    public void ReportCommand_Create_Success_HasCorrectFormat()
    {
        CommandClassFrame reportFrame = SupervisionCommandClass.CreateReport(
            moreStatusUpdates: false,
            wakeUpRequest: false,
            sessionId: 10,
            status: SupervisionStatus.Success,
            duration: new DurationReport(0));

        Assert.AreEqual(CommandClassId.Supervision, reportFrame.CommandClassId);
        Assert.AreEqual((byte)SupervisionCommand.Report, reportFrame.CommandId);

        ReadOnlySpan<byte> parameters = reportFrame.CommandParameters.Span;
        // byte 0: MoreStatusUpdates(0) | WakeUpRequest(0) | SessionID(10) = 0x0A
        Assert.AreEqual((byte)0x0A, parameters[0]);
        // byte 1: Status = SUCCESS = 0xFF
        Assert.AreEqual((byte)0xFF, parameters[1]);
        // byte 2: Duration = 0
        Assert.AreEqual((byte)0x00, parameters[2]);
    }

    [TestMethod]
    public void ReportCommand_Create_Working_WithDuration()
    {
        CommandClassFrame reportFrame = SupervisionCommandClass.CreateReport(
            moreStatusUpdates: true,
            wakeUpRequest: false,
            sessionId: 20,
            status: SupervisionStatus.Working,
            duration: new DurationReport(5));

        ReadOnlySpan<byte> parameters = reportFrame.CommandParameters.Span;
        // byte 0: MoreStatusUpdates(1) | WakeUpRequest(0) | SessionID(20) = 0x80 | 0x14 = 0x94
        Assert.AreEqual((byte)0x94, parameters[0]);
        // byte 1: Status = WORKING = 0x01
        Assert.AreEqual((byte)0x01, parameters[1]);
        // byte 2: Duration = 5 seconds
        Assert.AreEqual((byte)0x05, parameters[2]);
    }

    [TestMethod]
    public void ReportCommand_Create_WithWakeUpRequest_V2()
    {
        CommandClassFrame reportFrame = SupervisionCommandClass.CreateReport(
            moreStatusUpdates: false,
            wakeUpRequest: true,
            sessionId: 7,
            status: SupervisionStatus.Success,
            duration: new DurationReport(0));

        ReadOnlySpan<byte> parameters = reportFrame.CommandParameters.Span;
        // byte 0: MoreStatusUpdates(0) | WakeUpRequest(1) | SessionID(7) = 0x40 | 0x07 = 0x47
        Assert.AreEqual((byte)0x47, parameters[0]);
    }

    [TestMethod]
    public void ReportCommand_Create_NoSupport()
    {
        CommandClassFrame reportFrame = SupervisionCommandClass.CreateReport(
            moreStatusUpdates: false,
            wakeUpRequest: false,
            sessionId: 0,
            status: SupervisionStatus.NoSupport,
            duration: new DurationReport(0));

        ReadOnlySpan<byte> parameters = reportFrame.CommandParameters.Span;
        Assert.AreEqual((byte)0x00, parameters[1]); // NO_SUPPORT = 0x00
        Assert.AreEqual((byte)0x00, parameters[2]); // Zero duration per spec
    }

    [TestMethod]
    public void ReportCommand_Create_Fail()
    {
        CommandClassFrame reportFrame = SupervisionCommandClass.CreateReport(
            moreStatusUpdates: false,
            wakeUpRequest: false,
            sessionId: 15,
            status: SupervisionStatus.Fail,
            duration: new DurationReport(0));

        ReadOnlySpan<byte> parameters = reportFrame.CommandParameters.Span;
        Assert.AreEqual((byte)0x02, parameters[1]); // FAIL = 0x02
    }

    [TestMethod]
    public void ReportCommand_Create_RejectsSessionIdAbove63()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SupervisionCommandClass.CreateReport(
                moreStatusUpdates: false,
                wakeUpRequest: false,
                sessionId: 64,
                status: SupervisionStatus.Success,
                duration: new DurationReport(0)));
    }

    [TestMethod]
    public void ReportCommand_Parse_Success()
    {
        // CC=0x6C, Cmd=0x02, Flags=0x0A (sessionId=10, moreUpdates=0, wakeUp=0), Status=0xFF, Duration=0x00
        byte[] data = [0x6C, 0x02, 0x0A, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.IsFalse(report.MoreStatusUpdates);
        Assert.IsFalse(report.WakeUpRequest);
        Assert.AreEqual((byte)10, report.SessionId);
        Assert.AreEqual(SupervisionStatus.Success, report.Status);
        Assert.AreEqual((byte)0x00, report.Duration.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Working_WithDuration()
    {
        // Flags=0x94 (moreUpdates=1, wakeUp=0, sessionId=20), Status=0x01, Duration=0x05
        byte[] data = [0x6C, 0x02, 0x94, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.IsTrue(report.MoreStatusUpdates);
        Assert.IsFalse(report.WakeUpRequest);
        Assert.AreEqual((byte)20, report.SessionId);
        Assert.AreEqual(SupervisionStatus.Working, report.Status);
        Assert.AreEqual((byte)0x05, report.Duration.Value);
        Assert.AreEqual(TimeSpan.FromSeconds(5), report.Duration.Duration);
    }

    [TestMethod]
    public void ReportCommand_Parse_V2_WithWakeUpRequest()
    {
        // Flags=0x47 (moreUpdates=0, wakeUp=1, sessionId=7), Status=0xFF, Duration=0x00
        byte[] data = [0x6C, 0x02, 0x47, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.IsFalse(report.MoreStatusUpdates);
        Assert.IsTrue(report.WakeUpRequest);
        Assert.AreEqual((byte)7, report.SessionId);
        Assert.AreEqual(SupervisionStatus.Success, report.Status);
    }

    [TestMethod]
    public void ReportCommand_Parse_Fail()
    {
        byte[] data = [0x6C, 0x02, 0x00, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.AreEqual(SupervisionStatus.Fail, report.Status);
    }

    [TestMethod]
    public void ReportCommand_Parse_NoSupport()
    {
        byte[] data = [0x6C, 0x02, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.AreEqual(SupervisionStatus.NoSupport, report.Status);
    }

    [TestMethod]
    public void ReportCommand_Parse_TooShort_Throws()
    {
        // Only 2 parameter bytes (need 3: flags + status + duration)
        byte[] data = [0x6C, 0x02, 0x0A, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => SupervisionCommandClass.ParseReport(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ReportCommand_RoundTrip_AllFlags()
    {
        CommandClassFrame reportFrame = SupervisionCommandClass.CreateReport(
            moreStatusUpdates: true,
            wakeUpRequest: true,
            sessionId: 63,
            status: SupervisionStatus.Working,
            duration: new DurationReport(0x85)); // 6 minutes

        SupervisionReport parsed = SupervisionCommandClass.ParseReport(reportFrame, NullLogger.Instance);

        Assert.IsTrue(parsed.MoreStatusUpdates);
        Assert.IsTrue(parsed.WakeUpRequest);
        Assert.AreEqual((byte)63, parsed.SessionId);
        Assert.AreEqual(SupervisionStatus.Working, parsed.Status);
        Assert.AreEqual((byte)0x85, parsed.Duration.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(6), parsed.Duration.Duration);
    }

    [TestMethod]
    public void ReportCommand_Parse_ReservedStatusValue()
    {
        // Use a reserved status value (0x03) — spec says "MUST be ignored by a receiving node"
        // but we should still parse without throwing
        byte[] data = [0x6C, 0x02, 0x00, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.AreEqual((SupervisionStatus)0x03, report.Status);
    }

    [TestMethod]
    public void ReportCommand_Parse_DurationMinutes()
    {
        // Duration = 0xFD → 126 minutes
        byte[] data = [0x6C, 0x02, 0x00, 0x01, 0xFD];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFD, report.Duration.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(126), report.Duration.Duration);
    }

    [TestMethod]
    public void ReportCommand_Parse_UnknownDuration()
    {
        // Duration = 0xFE → Unknown
        byte[] data = [0x6C, 0x02, 0x00, 0x01, 0xFE];
        CommandClassFrame frame = new(data);

        SupervisionReport report = SupervisionCommandClass.ParseReport(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFE, report.Duration.Value);
        Assert.IsNull(report.Duration.Duration);
    }

    [TestMethod]
    public void GetCommand_Create_MinimalEncapsulatedCommand()
    {
        // Minimum valid encapsulated command: just CC ID + Command ID (2 bytes)
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x02);
        CommandClassFrame getFrame = SupervisionCommandClass.CreateGet(statusUpdates: false, sessionId: 0, innerFrame);

        SupervisionGet parsed = SupervisionCommandClass.ParseGet(getFrame, NullLogger.Instance);

        Assert.AreEqual((byte)0, parsed.SessionId);
        Assert.AreEqual(CommandClassId.Basic, parsed.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x02, parsed.EncapsulatedFrame.CommandId);
        Assert.AreEqual(0, parsed.EncapsulatedFrame.CommandParameters.Length);
    }

    [TestMethod]
    public void ReportCommand_Create_BothFlags_CorrectBitLayout()
    {
        // Verify bit layout: bit7=MoreStatusUpdates, bit6=WakeUpRequest, bits5..0=SessionID
        CommandClassFrame reportFrame = SupervisionCommandClass.CreateReport(
            moreStatusUpdates: true,
            wakeUpRequest: true,
            sessionId: 0,
            status: SupervisionStatus.Success,
            duration: new DurationReport(0));

        ReadOnlySpan<byte> parameters = reportFrame.CommandParameters.Span;
        // 0xC0 = 1100_0000
        Assert.AreEqual((byte)0xC0, parameters[0]);
    }
}
