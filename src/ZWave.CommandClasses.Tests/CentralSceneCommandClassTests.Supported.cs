using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class CentralSceneCommandClassTests
{
    [TestMethod]
    public void SupportedGet_Create_HasCorrectFormat()
    {
        CentralSceneCommandClass.CentralSceneSupportedGetCommand command =
            CentralSceneCommandClass.CentralSceneSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.CentralScene, CentralSceneCommandClass.CentralSceneSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)CentralSceneCommand.SupportedGet, CentralSceneCommandClass.CentralSceneSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void SupportedReport_Parse_Version1_SupportedScenesOnly()
    {
        // CC=0x5B, Cmd=0x02, SupportedScenes=4
        byte[] data = [0x5B, 0x02, 0x04];
        CommandClassFrame frame = new(data);

        CentralSceneSupportedReport report =
            CentralSceneCommandClass.CentralSceneSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)4, report.SupportedScenes);
        Assert.IsNull(report.SlowRefreshSupport);
        Assert.IsNull(report.Identical);
        Assert.IsNull(report.SupportedKeyAttributesPerScene);
    }

    [TestMethod]
    public void SupportedReport_Parse_Version2_IdenticalScenes()
    {
        // CC=0x5B, Cmd=0x02, SupportedScenes=3, Properties1=0x03 (Identical=1, BitMaskBytes=1),
        // KeyAttributes for all scenes: 0b00001111 (KeyPressed, KeyReleased, KeyHeldDown, KeyPressed2Times)
        byte[] data = [0x5B, 0x02, 0x03, 0x03, 0x0F];
        CommandClassFrame frame = new(data);

        CentralSceneSupportedReport report =
            CentralSceneCommandClass.CentralSceneSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)3, report.SupportedScenes);
        Assert.IsNotNull(report.SlowRefreshSupport);
        Assert.IsFalse(report.SlowRefreshSupport.Value);
        Assert.IsNotNull(report.Identical);
        Assert.IsTrue(report.Identical.Value);
        Assert.IsNotNull(report.SupportedKeyAttributesPerScene);
        Assert.HasCount(1, report.SupportedKeyAttributesPerScene); // Identical=true, only 1 set
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyReleased, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyHeldDown, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed2Times, report.SupportedKeyAttributesPerScene[0]);
    }

    [TestMethod]
    public void SupportedReport_Parse_Version2_DifferentScenes()
    {
        // CC=0x5B, Cmd=0x02, SupportedScenes=2, Properties1=0x02 (Identical=0, BitMaskBytes=1),
        // Scene 1: 0b00000111 (KeyPressed, KeyReleased, KeyHeldDown)
        // Scene 2: 0b00000001 (KeyPressed only)
        byte[] data = [0x5B, 0x02, 0x02, 0x02, 0x07, 0x01];
        CommandClassFrame frame = new(data);

        CentralSceneSupportedReport report =
            CentralSceneCommandClass.CentralSceneSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.SupportedScenes);
        Assert.IsNotNull(report.Identical);
        Assert.IsFalse(report.Identical.Value);
        Assert.IsNotNull(report.SupportedKeyAttributesPerScene);
        Assert.HasCount(2, report.SupportedKeyAttributesPerScene);

        // Scene 1
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyReleased, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyHeldDown, report.SupportedKeyAttributesPerScene[0]);
        Assert.HasCount(3, report.SupportedKeyAttributesPerScene[0]);

        // Scene 2
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed, report.SupportedKeyAttributesPerScene[1]);
        Assert.HasCount(1, report.SupportedKeyAttributesPerScene[1]);
    }

    [TestMethod]
    public void SupportedReport_Parse_Version3_SlowRefreshSupport()
    {
        // CC=0x5B, Cmd=0x02, SupportedScenes=2, Properties1=0x83 (SlowRefresh=1, Identical=1, BitMaskBytes=1),
        // KeyAttributes: 0b00000111
        byte[] data = [0x5B, 0x02, 0x02, 0x83, 0x07];
        CommandClassFrame frame = new(data);

        CentralSceneSupportedReport report =
            CentralSceneCommandClass.CentralSceneSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.SupportedScenes);
        Assert.IsNotNull(report.SlowRefreshSupport);
        Assert.IsTrue(report.SlowRefreshSupport.Value);
        Assert.IsNotNull(report.Identical);
        Assert.IsTrue(report.Identical.Value);
    }

    [TestMethod]
    public void SupportedReport_Parse_MultiByteBitmask()
    {
        // CC=0x5B, Cmd=0x02, SupportedScenes=1, Properties1=0x05 (Identical=1, BitMaskBytes=2),
        // KeyAttributes: 0b01111111 0b00000000 (bits 0-6 set in first byte)
        byte[] data = [0x5B, 0x02, 0x01, 0x05, 0x7F, 0x00];
        CommandClassFrame frame = new(data);

        CentralSceneSupportedReport report =
            CentralSceneCommandClass.CentralSceneSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.SupportedScenes);
        Assert.IsNotNull(report.SupportedKeyAttributesPerScene);
        Assert.HasCount(1, report.SupportedKeyAttributesPerScene);
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyReleased, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyHeldDown, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed2Times, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed3Times, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed4Times, report.SupportedKeyAttributesPerScene[0]);
        Assert.Contains(CentralSceneKeyAttribute.KeyPressed5Times, report.SupportedKeyAttributesPerScene[0]);
        Assert.HasCount(7, report.SupportedKeyAttributesPerScene[0]);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        // CC=0x5B, Cmd=0x02, no parameters
        byte[] data = [0x5B, 0x02];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => CentralSceneCommandClass.CentralSceneSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_BitmaskDataTooShort_Throws()
    {
        // CC=0x5B, Cmd=0x02, SupportedScenes=2, Properties1=0x02 (Identical=0, BitMaskBytes=1)
        // Missing bitmask data for 2 scenes
        byte[] data = [0x5B, 0x02, 0x02, 0x02, 0x07]; // Only 1 bitmask byte, need 2
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => CentralSceneCommandClass.CentralSceneSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
