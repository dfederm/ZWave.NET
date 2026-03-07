using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class EntryControlCommandClassTests
{
    [TestMethod]
    public void ConfigurationGetCommand_Create_HasCorrectFormat()
    {
        EntryControlCommandClass.EntryControlConfigurationGetCommand command =
            EntryControlCommandClass.EntryControlConfigurationGetCommand.Create();

        Assert.AreEqual(CommandClassId.EntryControl, EntryControlCommandClass.EntryControlConfigurationGetCommand.CommandClassId);
        Assert.AreEqual((byte)EntryControlCommand.ConfigurationGet, EntryControlCommandClass.EntryControlConfigurationGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void ConfigurationSetCommand_Create_HasCorrectFormat()
    {
        EntryControlCommandClass.EntryControlConfigurationSetCommand command =
            EntryControlCommandClass.EntryControlConfigurationSetCommand.Create(keyCacheSize: 4, keyCacheTimeout: 2);

        Assert.AreEqual(CommandClassId.EntryControl, EntryControlCommandClass.EntryControlConfigurationSetCommand.CommandClassId);
        Assert.AreEqual((byte)EntryControlCommand.ConfigurationSet, EntryControlCommandClass.EntryControlConfigurationSetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)4, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)2, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ConfigurationSetCommand_Create_MaxValues()
    {
        EntryControlCommandClass.EntryControlConfigurationSetCommand command =
            EntryControlCommandClass.EntryControlConfigurationSetCommand.Create(keyCacheSize: 32, keyCacheTimeout: 10);

        Assert.AreEqual((byte)32, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)10, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_DefaultValues()
    {
        // CC=0x6F, Cmd=0x08, KeyCacheSize=4, KeyCacheTimeout=2
        byte[] data = [0x6F, 0x08, 0x04, 0x02];
        CommandClassFrame frame = new(data);

        EntryControlConfigurationReport report =
            EntryControlCommandClass.EntryControlConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)4, report.KeyCacheSize);
        Assert.AreEqual((byte)2, report.KeyCacheTimeout);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_SingleKeyCache()
    {
        // KeyCacheSize=1, KeyCacheTimeout=1
        byte[] data = [0x6F, 0x08, 0x01, 0x01];
        CommandClassFrame frame = new(data);

        EntryControlConfigurationReport report =
            EntryControlCommandClass.EntryControlConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.KeyCacheSize);
        Assert.AreEqual((byte)1, report.KeyCacheTimeout);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_MaxValues()
    {
        // KeyCacheSize=32, KeyCacheTimeout=10
        byte[] data = [0x6F, 0x08, 0x20, 0x0A];
        CommandClassFrame frame = new(data);

        EntryControlConfigurationReport report =
            EntryControlCommandClass.EntryControlConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)32, report.KeyCacheSize);
        Assert.AreEqual((byte)10, report.KeyCacheTimeout);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_TooShort_Throws()
    {
        // Only 1 command parameter byte (need at least 2)
        byte[] data = [0x6F, 0x08, 0x04];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => EntryControlCommandClass.EntryControlConfigurationReportCommand.Parse(frame, NullLogger.Instance));
    }
}
