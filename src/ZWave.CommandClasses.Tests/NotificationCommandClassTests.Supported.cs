using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class NotificationCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        NotificationCommandClass.NotificationSupportedGetCommand command =
            NotificationCommandClass.NotificationSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.Notification, NotificationCommandClass.NotificationSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)NotificationCommand.SupportedGet, NotificationCommandClass.NotificationSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_SingleType_NoV1()
    {
        // CC=0x71, Cmd=0x08, V1Alarm=0|NumBitmasks=1, Bitmask: bit1=SmokeAlarm
        byte[] data = [0x71, 0x08, 0x01, 0x02]; // bitmask byte 0x02 = bit 1 set
        CommandClassFrame frame = new(data);

        SupportedNotifications result = NotificationCommandClass.NotificationSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(result.SupportsV1Alarm);
        Assert.Contains(NotificationType.SmokeAlarm, result.SupportedNotificationTypes);
        Assert.HasCount(1, result.SupportedNotificationTypes);
    }

    [TestMethod]
    public void SupportedReport_Parse_MultipleTypes_WithV1()
    {
        // CC=0x71, Cmd=0x08, V1Alarm=1|NumBitmasks=1, Bitmask: bits 1,2,6 = Smoke,CO,AccessControl
        byte[] data = [0x71, 0x08, 0x81, 0x46]; // 0x81 = V1+1 bitmask, 0x46 = 0b01000110
        CommandClassFrame frame = new(data);

        SupportedNotifications result = NotificationCommandClass.NotificationSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(result.SupportsV1Alarm);
        Assert.Contains(NotificationType.SmokeAlarm, result.SupportedNotificationTypes);   // bit 1
        Assert.Contains(NotificationType.COAlarm, result.SupportedNotificationTypes);       // bit 2
        Assert.Contains(NotificationType.AccessControl, result.SupportedNotificationTypes); // bit 6
        Assert.HasCount(3, result.SupportedNotificationTypes);
    }

    [TestMethod]
    public void SupportedReport_Parse_TwoBitmaskBytes()
    {
        // CC=0x71, Cmd=0x08, V1Alarm=0|NumBitmasks=2, Byte0: bit7=HomeSecurity, Byte1: bit0=PowerManagement
        byte[] data = [0x71, 0x08, 0x02, 0x80, 0x01]; // 0x80 = bit7, 0x01 = bit0 of byte1
        CommandClassFrame frame = new(data);

        SupportedNotifications result = NotificationCommandClass.NotificationSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.Contains(NotificationType.HomeSecurity, result.SupportedNotificationTypes);    // byte0 bit7 = type 7
        Assert.Contains(NotificationType.PowerManagement, result.SupportedNotificationTypes); // byte1 bit0 = type 8
        Assert.HasCount(2, result.SupportedNotificationTypes);
    }

    [TestMethod]
    public void SupportedReport_Parse_EmptyBitmask()
    {
        // CC=0x71, Cmd=0x08, V1Alarm=0|NumBitmasks=1, Bitmask=0x00 (no types)
        byte[] data = [0x71, 0x08, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        SupportedNotifications result = NotificationCommandClass.NotificationSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(result.SupportsV1Alarm);
        Assert.IsEmpty(result.SupportedNotificationTypes);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        // CC=0x71, Cmd=0x08, no parameters
        byte[] data = [0x71, 0x08];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => NotificationCommandClass.NotificationSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_TruncatedBitmask_Throws()
    {
        // CC=0x71, Cmd=0x08, NumBitmasks=2 but only 1 bitmask byte
        byte[] data = [0x71, 0x08, 0x02, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => NotificationCommandClass.NotificationSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
