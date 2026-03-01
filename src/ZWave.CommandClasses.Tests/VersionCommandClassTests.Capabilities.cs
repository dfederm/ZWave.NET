using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class VersionCommandClassTests
{
    [TestMethod]
    public void CapabilitiesGetCommand_Create_HasCorrectFormat()
    {
        VersionCommandClass.VersionCapabilitiesGetCommand command =
            VersionCommandClass.VersionCapabilitiesGetCommand.Create();

        Assert.AreEqual(CommandClassId.Version, VersionCommandClass.VersionCapabilitiesGetCommand.CommandClassId);
        Assert.AreEqual((byte)VersionCommand.CapabilitiesGet, VersionCommandClass.VersionCapabilitiesGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_AllFlagsSet()
    {
        // CC=0x86, Cmd=0x16, Capabilities=0b00001111 (V|CC|ZWS|M)
        byte[] data = [0x86, 0x16, 0x0F];
        CommandClassFrame frame = new(data);

        VersionCapabilities capabilities = VersionCommandClass.VersionCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.Version);
        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.CommandClass);
        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.ZWaveSoftware);
        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.MigrationSupport);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_Version3_NoMigration()
    {
        // CC=0x86, Cmd=0x16, Capabilities=0b00000111 (V|CC|ZWS, no M)
        byte[] data = [0x86, 0x16, 0x07];
        CommandClassFrame frame = new(data);

        VersionCapabilities capabilities = VersionCommandClass.VersionCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.Version);
        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.CommandClass);
        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.ZWaveSoftware);
        Assert.AreEqual((VersionCapabilities)0, capabilities & VersionCapabilities.MigrationSupport);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_MinimalFlags()
    {
        // CC=0x86, Cmd=0x16, Capabilities=0b00000011 (V|CC only)
        byte[] data = [0x86, 0x16, 0x03];
        CommandClassFrame frame = new(data);

        VersionCapabilities capabilities = VersionCommandClass.VersionCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.Version);
        Assert.AreNotEqual((VersionCapabilities)0, capabilities & VersionCapabilities.CommandClass);
        Assert.AreEqual((VersionCapabilities)0, capabilities & VersionCapabilities.ZWaveSoftware);
        Assert.AreEqual((VersionCapabilities)0, capabilities & VersionCapabilities.MigrationSupport);
    }

    [TestMethod]
    public void CapabilitiesReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x86, 0x16];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }
}
