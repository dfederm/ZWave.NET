using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class CentralSceneCommandClassTests
{
    [TestMethod]
    public void Notification_Parse_Version1_KeyPressed()
    {
        // CC=0x5B, Cmd=0x03, SeqNum=0x01, Properties1=0x00 (KeyPressed), SceneNumber=0x01
        byte[] data = [0x5B, 0x03, 0x01, 0x00, 0x01];
        CommandClassFrame frame = new(data);

        CentralSceneNotification notification =
            CentralSceneCommandClass.CentralSceneNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x01, notification.SequenceNumber);
        Assert.AreEqual(CentralSceneKeyAttribute.KeyPressed, notification.KeyAttribute);
        Assert.AreEqual((byte)0x01, notification.SceneNumber);
        Assert.IsFalse(notification.SlowRefresh);
    }

    [TestMethod]
    public void Notification_Parse_KeyReleased()
    {
        // CC=0x5B, Cmd=0x03, SeqNum=0x05, Properties1=0x01 (KeyReleased), SceneNumber=0x03
        byte[] data = [0x5B, 0x03, 0x05, 0x01, 0x03];
        CommandClassFrame frame = new(data);

        CentralSceneNotification notification =
            CentralSceneCommandClass.CentralSceneNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x05, notification.SequenceNumber);
        Assert.AreEqual(CentralSceneKeyAttribute.KeyReleased, notification.KeyAttribute);
        Assert.AreEqual((byte)0x03, notification.SceneNumber);
    }

    [TestMethod]
    public void Notification_Parse_KeyHeldDown()
    {
        // CC=0x5B, Cmd=0x03, SeqNum=0x0A, Properties1=0x02 (KeyHeldDown), SceneNumber=0x02
        byte[] data = [0x5B, 0x03, 0x0A, 0x02, 0x02];
        CommandClassFrame frame = new(data);

        CentralSceneNotification notification =
            CentralSceneCommandClass.CentralSceneNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x0A, notification.SequenceNumber);
        Assert.AreEqual(CentralSceneKeyAttribute.KeyHeldDown, notification.KeyAttribute);
        Assert.AreEqual((byte)0x02, notification.SceneNumber);
    }

    [TestMethod]
    public void Notification_Parse_Version2_KeyPressed2Times()
    {
        // CC=0x5B, Cmd=0x03, SeqNum=0x10, Properties1=0x03 (KeyPressed2Times), SceneNumber=0x01
        byte[] data = [0x5B, 0x03, 0x10, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        CentralSceneNotification notification =
            CentralSceneCommandClass.CentralSceneNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x10, notification.SequenceNumber);
        Assert.AreEqual(CentralSceneKeyAttribute.KeyPressed2Times, notification.KeyAttribute);
        Assert.AreEqual((byte)0x01, notification.SceneNumber);
    }

    [TestMethod]
    public void Notification_Parse_Version3_SlowRefreshEnabled()
    {
        // CC=0x5B, Cmd=0x03, SeqNum=0x20, Properties1=0x82 (SlowRefresh=1, KeyHeldDown=0x02), SceneNumber=0x01
        byte[] data = [0x5B, 0x03, 0x20, 0x82, 0x01];
        CommandClassFrame frame = new(data);

        CentralSceneNotification notification =
            CentralSceneCommandClass.CentralSceneNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x20, notification.SequenceNumber);
        Assert.AreEqual(CentralSceneKeyAttribute.KeyHeldDown, notification.KeyAttribute);
        Assert.AreEqual((byte)0x01, notification.SceneNumber);
        Assert.IsTrue(notification.SlowRefresh);
    }

    [TestMethod]
    public void Notification_Parse_KeyAttributeExtractedFromLower3Bits()
    {
        // Properties1=0xFD: bits 7-3 = 11111, bits 2-0 = 101 (KeyPressed4Times=0x05)
        byte[] data = [0x5B, 0x03, 0x01, 0xFD, 0x01];
        CommandClassFrame frame = new(data);

        CentralSceneNotification notification =
            CentralSceneCommandClass.CentralSceneNotificationCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(CentralSceneKeyAttribute.KeyPressed4Times, notification.KeyAttribute);
    }

    [TestMethod]
    public void Notification_Parse_TooShort_Throws()
    {
        // CC=0x5B, Cmd=0x03, only 2 parameter bytes (need 3)
        byte[] data = [0x5B, 0x03, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => CentralSceneCommandClass.CentralSceneNotificationCommand.Parse(frame, NullLogger.Instance));
    }
}
