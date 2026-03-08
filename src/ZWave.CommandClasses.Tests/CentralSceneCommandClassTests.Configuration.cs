using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class CentralSceneCommandClassTests
{
    [TestMethod]
    public void ConfigurationSet_Create_SlowRefreshEnabled()
    {
        CentralSceneCommandClass.CentralSceneConfigurationSetCommand command =
            CentralSceneCommandClass.CentralSceneConfigurationSetCommand.Create(slowRefresh: true);

        Assert.AreEqual(CommandClassId.CentralScene, CentralSceneCommandClass.CentralSceneConfigurationSetCommand.CommandClassId);
        Assert.AreEqual((byte)CentralSceneCommand.ConfigurationSet, CentralSceneCommandClass.CentralSceneConfigurationSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + Properties1
        Assert.AreEqual((byte)0x80, command.Frame.CommandParameters.Span[0]); // Bit 7 set
    }

    [TestMethod]
    public void ConfigurationSet_Create_SlowRefreshDisabled()
    {
        CentralSceneCommandClass.CentralSceneConfigurationSetCommand command =
            CentralSceneCommandClass.CentralSceneConfigurationSetCommand.Create(slowRefresh: false);

        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]); // Bit 7 clear
    }

    [TestMethod]
    public void ConfigurationGet_Create_HasCorrectFormat()
    {
        CentralSceneCommandClass.CentralSceneConfigurationGetCommand command =
            CentralSceneCommandClass.CentralSceneConfigurationGetCommand.Create();

        Assert.AreEqual(CommandClassId.CentralScene, CentralSceneCommandClass.CentralSceneConfigurationGetCommand.CommandClassId);
        Assert.AreEqual((byte)CentralSceneCommand.ConfigurationGet, CentralSceneCommandClass.CentralSceneConfigurationGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void ConfigurationReport_Parse_SlowRefreshEnabled()
    {
        // CC=0x5B, Cmd=0x06, Properties1=0x80 (SlowRefresh=1)
        byte[] data = [0x5B, 0x06, 0x80];
        CommandClassFrame frame = new(data);

        CentralSceneConfigurationReport report =
            CentralSceneCommandClass.CentralSceneConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.SlowRefresh);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_SlowRefreshDisabled()
    {
        // CC=0x5B, Cmd=0x06, Properties1=0x00 (SlowRefresh=0)
        byte[] data = [0x5B, 0x06, 0x00];
        CommandClassFrame frame = new(data);

        CentralSceneConfigurationReport report =
            CentralSceneCommandClass.CentralSceneConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.SlowRefresh);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_ReservedBitsIgnored()
    {
        // CC=0x5B, Cmd=0x06, Properties1=0x7F (SlowRefresh=0, Reserved bits set)
        byte[] data = [0x5B, 0x06, 0x7F];
        CommandClassFrame frame = new(data);

        CentralSceneConfigurationReport report =
            CentralSceneCommandClass.CentralSceneConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.SlowRefresh);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_TooShort_Throws()
    {
        // CC=0x5B, Cmd=0x06, no parameters
        byte[] data = [0x5B, 0x06];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => CentralSceneCommandClass.CentralSceneConfigurationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ConfigurationSet_Create_RoundTrips()
    {
        CentralSceneCommandClass.CentralSceneConfigurationSetCommand command =
            CentralSceneCommandClass.CentralSceneConfigurationSetCommand.Create(slowRefresh: true);

        CentralSceneConfigurationReport report =
            CentralSceneCommandClass.CentralSceneConfigurationReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.IsTrue(report.SlowRefresh);
    }
}
