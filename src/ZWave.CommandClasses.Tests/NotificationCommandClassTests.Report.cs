using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class NotificationCommandClassTests
{
    [TestMethod]
    public void GetV1Command_Create_HasCorrectFormat()
    {
        NotificationCommandClass.NotificationGetV1Command command =
            NotificationCommandClass.NotificationGetV1Command.Create(0x05);

        Assert.AreEqual(CommandClassId.Notification, NotificationCommandClass.NotificationGetV1Command.CommandClassId);
        Assert.AreEqual((byte)NotificationCommand.Get, NotificationCommandClass.NotificationGetV1Command.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x05, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version2_NoEvent()
    {
        NotificationCommandClass.NotificationGetCommand command =
            NotificationCommandClass.NotificationGetCommand.Create(2, NotificationType.SmokeAlarm, null);

        Assert.AreEqual(CommandClassId.Notification, NotificationCommandClass.NotificationGetCommand.CommandClassId);
        Assert.AreEqual((byte)NotificationCommand.Get, NotificationCommandClass.NotificationGetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // V2: 2 bytes (V1 Alarm Type + Notification Type), no event field
        Assert.AreEqual(2, parameters.Length);
        Assert.AreEqual(0x00, parameters[0]); // V1 Alarm Type = 0 (not used)
        Assert.AreEqual((byte)NotificationType.SmokeAlarm, parameters[1]);
    }

    [TestMethod]
    public void GetCommand_Create_Version3_WithEvent()
    {
        NotificationCommandClass.NotificationGetCommand command =
            NotificationCommandClass.NotificationGetCommand.Create(3, NotificationType.HomeSecurity, 0x07);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // V3: 3 bytes (V1 Alarm Type + Notification Type + Event)
        Assert.AreEqual(3, parameters.Length);
        Assert.AreEqual(0x00, parameters[0]);
        Assert.AreEqual((byte)NotificationType.HomeSecurity, parameters[1]);
        Assert.AreEqual(0x07, parameters[2]);
    }

    [TestMethod]
    public void GetCommand_Create_Version3_NullEvent_DefaultsToZero()
    {
        NotificationCommandClass.NotificationGetCommand command =
            NotificationCommandClass.NotificationGetCommand.Create(3, NotificationType.WaterAlarm, null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        Assert.AreEqual(0x00, parameters[2]); // default event = 0
    }

    [TestMethod]
    public void GetCommand_Create_Version3_RequestPending_EventIsZero()
    {
        NotificationCommandClass.NotificationGetCommand command =
            NotificationCommandClass.NotificationGetCommand.Create(3, NotificationType.RequestPendingNotification, 0x05);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        Assert.AreEqual((byte)NotificationType.RequestPendingNotification, parameters[1]);
        // Per spec: event must be 0x00 when type is 0xFF
        Assert.AreEqual(0x00, parameters[2]);
    }

    [TestMethod]
    public void SetCommand_Create_Enabled()
    {
        NotificationCommandClass.NotificationSetCommand command =
            NotificationCommandClass.NotificationSetCommand.Create(NotificationType.SmokeAlarm, true);

        Assert.AreEqual(CommandClassId.Notification, NotificationCommandClass.NotificationSetCommand.CommandClassId);
        Assert.AreEqual((byte)NotificationCommand.Set, NotificationCommandClass.NotificationSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(2, parameters.Length);
        Assert.AreEqual((byte)NotificationType.SmokeAlarm, parameters[0]);
        Assert.AreEqual(0xFF, parameters[1]);
    }

    [TestMethod]
    public void SetCommand_Create_Disabled()
    {
        NotificationCommandClass.NotificationSetCommand command =
            NotificationCommandClass.NotificationSetCommand.Create(NotificationType.AccessControl, false);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(2, parameters.Length);
        Assert.AreEqual((byte)NotificationType.AccessControl, parameters[0]);
        Assert.AreEqual(0x00, parameters[1]);
    }

    [TestMethod]
    public void Report_Parse_V1Only_MinimalPayload()
    {
        // CC=0x71, Cmd=0x05, V1AlarmType=0x01, V1AlarmLevel=0x63
        byte[] data = [0x71, 0x05, 0x01, 0x63];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x01, report.V1AlarmType);
        Assert.AreEqual((byte)0x63, report.V1AlarmLevel);
        Assert.IsNull(report.NotificationStatus);
        Assert.IsNull(report.NotificationType);
        Assert.IsNull(report.NotificationEvent);
        Assert.IsNull(report.EventParameters);
        Assert.IsNull(report.SequenceNumber);
    }

    [TestMethod]
    public void Report_Parse_V1Fields_AlwaysParsed()
    {
        // Full V3+ report with V1 fields and a valid event — V1 fields still parsed
        // CC=0x71, Cmd=0x05, V1AlarmType=0x05, V1AlarmLevel=0x10, Reserved=0x00,
        // Status=0xFF, Type=0x01 (Smoke), Event=0x01 (Smoke Detected), Params=0x00
        byte[] data = [0x71, 0x05, 0x05, 0x10, 0x00, 0xFF, 0x01, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        // V1 fields are always parsed regardless of notification event
        Assert.AreEqual((byte)0x05, report.V1AlarmType);
        Assert.AreEqual((byte)0x10, report.V1AlarmLevel);
        Assert.IsTrue(report.NotificationStatus);
        Assert.AreEqual(NotificationType.SmokeAlarm, report.NotificationType);
        Assert.AreEqual((byte)0x01, report.NotificationEvent);
    }

    [TestMethod]
    public void Report_Parse_FullReport_WithEventParams()
    {
        // CC=0x71, Cmd=0x05, V1AlarmType=0x00, V1AlarmLevel=0x00, Reserved=0x00,
        // Status=0xFF, Type=0x06 (AccessControl), Event=0x01,
        // SeqBit=0 | ParamLen=3, Param1=0x63, Param2=0x03, Param3=0x01
        byte[] data = [0x71, 0x05, 0x00, 0x00, 0x00, 0xFF, 0x06, 0x01, 0x03, 0x63, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.V1AlarmType);
        Assert.IsTrue(report.NotificationStatus);
        Assert.AreEqual(NotificationType.AccessControl, report.NotificationType);
        Assert.AreEqual((byte)0x01, report.NotificationEvent);
        Assert.IsNotNull(report.EventParameters);
        Assert.AreEqual(3, report.EventParameters.Value.Length);
        Assert.AreEqual((byte)0x63, report.EventParameters.Value.Span[0]);
        Assert.AreEqual((byte)0x03, report.EventParameters.Value.Span[1]);
        Assert.AreEqual((byte)0x01, report.EventParameters.Value.Span[2]);
        Assert.IsNull(report.SequenceNumber);
    }

    [TestMethod]
    public void Report_Parse_WithSequenceNumber()
    {
        // CC=0x71, Cmd=0x05, V1=0x00, V1=0x00, Reserved=0x00,
        // Status=0xFF, Type=0x01 (Smoke), Event=0x01,
        // Seq=1 | ParamLen=0 (0x80), SequenceNumber=0x0F
        byte[] data = [0x71, 0x05, 0x00, 0x00, 0x00, 0xFF, 0x01, 0x01, 0x80, 0x0F];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(NotificationType.SmokeAlarm, report.NotificationType);
        Assert.AreEqual((byte)0x01, report.NotificationEvent);
        Assert.IsNull(report.EventParameters);
        Assert.IsNotNull(report.SequenceNumber);
        Assert.AreEqual((byte)0x0F, report.SequenceNumber.Value);
    }

    [TestMethod]
    public void Report_Parse_WithEventParamsAndSequence()
    {
        // CC=0x71, Cmd=0x05, V1=0x00, V1=0x00, Reserved=0x00,
        // Status=0xFF, Type=0x01, Event=0x01,
        // Seq=1 | ParamLen=2 (0x82), Param1=0xAA, Param2=0xBB, SeqNum=0x05
        byte[] data = [0x71, 0x05, 0x00, 0x00, 0x00, 0xFF, 0x01, 0x01, 0x82, 0xAA, 0xBB, 0x05];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(report.EventParameters);
        Assert.AreEqual(2, report.EventParameters.Value.Length);
        Assert.AreEqual((byte)0xAA, report.EventParameters.Value.Span[0]);
        Assert.AreEqual((byte)0xBB, report.EventParameters.Value.Span[1]);
        Assert.IsNotNull(report.SequenceNumber);
        Assert.AreEqual((byte)0x05, report.SequenceNumber.Value);
    }

    [TestMethod]
    public void Report_Parse_StatusDisabled()
    {
        // CC=0x71, Cmd=0x05, V1=0x00, V1=0x00, Reserved=0x00,
        // Status=0x00 (disabled), Type=0x01, Event=0x00 (state idle), Params=0x00
        byte[] data = [0x71, 0x05, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.NotificationStatus);
        Assert.AreEqual(NotificationType.SmokeAlarm, report.NotificationType);
        Assert.AreEqual((byte)0x00, report.NotificationEvent);
    }

    [TestMethod]
    public void Report_Parse_StatusReserved_ReturnsNull()
    {
        // CC=0x71, Cmd=0x05, V1=0x00, V1=0x00, Reserved=0x00,
        // Status=0xFE (pull mode queue empty / reserved for push), Type=0x01, Event=0x00, Params=0x00
        byte[] data = [0x71, 0x05, 0x00, 0x00, 0x00, 0xFE, 0x01, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        // 0xFE is not 0x00 or 0xFF, so maps to null
        Assert.IsNull(report.NotificationStatus);
    }

    [TestMethod]
    public void Report_Parse_UnknownEvent_0xFE()
    {
        // CC=0x71, Cmd=0x05, V1=0x00, V1=0x00, Reserved=0x00,
        // Status=0xFF, Type=0x01, Event=0xFE (Unknown), Params=0x00
        byte[] data = [0x71, 0x05, 0x00, 0x00, 0x00, 0xFF, 0x01, 0xFE, 0x00];
        CommandClassFrame frame = new(data);

        NotificationReport report = NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFE, report.NotificationEvent);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x71, Cmd=0x05, only 1 parameter byte
        byte[] data = [0x71, 0x05, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_NoParameters_Throws()
    {
        // CC=0x71, Cmd=0x05, no parameters
        byte[] data = [0x71, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => NotificationCommandClass.NotificationReportCommand.Parse(frame, NullLogger.Instance));
    }
}
