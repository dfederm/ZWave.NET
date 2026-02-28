using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class NotificationCommandClassTests
{
    [TestMethod]
    public void EventSupportedGetCommand_Create_HasCorrectFormat()
    {
        NotificationCommandClass.NotificationEventSupportedGetCommand command =
            NotificationCommandClass.NotificationEventSupportedGetCommand.Create(NotificationType.SmokeAlarm);

        Assert.AreEqual(CommandClassId.Notification, NotificationCommandClass.NotificationEventSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)NotificationCommand.EventSupportedGet, NotificationCommandClass.NotificationEventSupportedGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)NotificationType.SmokeAlarm, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void EventSupportedGetCommand_Create_HomeSecurity()
    {
        NotificationCommandClass.NotificationEventSupportedGetCommand command =
            NotificationCommandClass.NotificationEventSupportedGetCommand.Create(NotificationType.HomeSecurity);

        Assert.AreEqual((byte)NotificationType.HomeSecurity, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_SingleEvent()
    {
        // CC=0x71, Cmd=0x02, Type=0x01 (Smoke), NumBitmasks=1, Bitmask: bit1=event 1
        byte[] data = [0x71, 0x02, 0x01, 0x01, 0x02]; // 0x02 = bit 1 set
        CommandClassFrame frame = new(data);

        SupportedNotificationEvents result = NotificationCommandClass.NotificationEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(NotificationType.SmokeAlarm, result.NotificationType);
        Assert.Contains((byte)1, result.SupportedEvents);
        Assert.HasCount(1, result.SupportedEvents);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_MultipleEvents()
    {
        // CC=0x71, Cmd=0x02, Type=0x07 (HomeSecurity), NumBitmasks=1,
        // Bitmask: 0x8E = bits 1,2,3,7 = events 1,2,3,7
        byte[] data = [0x71, 0x02, 0x07, 0x01, 0x8E];
        CommandClassFrame frame = new(data);

        SupportedNotificationEvents result = NotificationCommandClass.NotificationEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(NotificationType.HomeSecurity, result.NotificationType);
        Assert.Contains((byte)1, result.SupportedEvents);
        Assert.Contains((byte)2, result.SupportedEvents);
        Assert.Contains((byte)3, result.SupportedEvents);
        Assert.Contains((byte)7, result.SupportedEvents);
        Assert.HasCount(4, result.SupportedEvents);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_ZeroBitmasks_NotSupported()
    {
        // CC=0x71, Cmd=0x02, Type=0x01, NumBitmasks=0 (type not supported)
        byte[] data = [0x71, 0x02, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        SupportedNotificationEvents result = NotificationCommandClass.NotificationEventSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(NotificationType.SmokeAlarm, result.NotificationType);
        Assert.IsEmpty(result.SupportedEvents);
    }

    [TestMethod]
    public void EventSupportedReport_Parse_TooShort_Throws()
    {
        // CC=0x71, Cmd=0x02, only 1 parameter byte
        byte[] data = [0x71, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => NotificationCommandClass.NotificationEventSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void EventSupportedReport_Parse_TruncatedBitmask_Throws()
    {
        // CC=0x71, Cmd=0x02, Type=0x01, NumBitmasks=2 but only 1 bitmask byte
        byte[] data = [0x71, 0x02, 0x01, 0x02, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => NotificationCommandClass.NotificationEventSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
