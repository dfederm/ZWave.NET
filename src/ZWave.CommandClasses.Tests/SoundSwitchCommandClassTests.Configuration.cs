using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class SoundSwitchCommandClassTests
{
    [TestMethod]
    public void ConfigurationSetCommand_Create_HasCorrectFormat()
    {
        SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand command =
            SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand.Create(50, 3);

        Assert.AreEqual(CommandClassId.SoundSwitch, SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand.CommandClassId);
        Assert.AreEqual((byte)SoundSwitchCommand.ConfigurationSet, SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)50, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)3, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ConfigurationSetCommand_Create_MuteVolume()
    {
        SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand command =
            SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand.Create(0x00, 1);

        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)1, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ConfigurationSetCommand_Create_RestoreVolume_NoDefaultToneChange()
    {
        // Volume=0xFF (restore), DefaultTone=0x00 (don't change)
        SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand command =
            SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand.Create(0xFF, 0x00);

        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ConfigurationSetCommand_Create_FullVolume()
    {
        SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand command =
            SoundSwitchCommandClass.SoundSwitchConfigurationSetCommand.Create(100, 5);

        Assert.AreEqual((byte)100, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)5, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ConfigurationGetCommand_Create_HasCorrectFormat()
    {
        SoundSwitchCommandClass.SoundSwitchConfigurationGetCommand command =
            SoundSwitchCommandClass.SoundSwitchConfigurationGetCommand.Create();

        Assert.AreEqual(CommandClassId.SoundSwitch, SoundSwitchCommandClass.SoundSwitchConfigurationGetCommand.CommandClassId);
        Assert.AreEqual((byte)SoundSwitchCommand.ConfigurationGet, SoundSwitchCommandClass.SoundSwitchConfigurationGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_NormalValues()
    {
        // CC=0x79, Cmd=0x07, Volume=75, DefaultTone=2
        byte[] data = [0x79, 0x07, 0x4B, 0x02];
        CommandClassFrame frame = new(data);

        SoundSwitchConfigurationReport report =
            SoundSwitchCommandClass.SoundSwitchConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)75, report.Volume);
        Assert.AreEqual((byte)2, report.DefaultToneIdentifier);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_MutedVolume()
    {
        // CC=0x79, Cmd=0x07, Volume=0, DefaultTone=1
        byte[] data = [0x79, 0x07, 0x00, 0x01];
        CommandClassFrame frame = new(data);

        SoundSwitchConfigurationReport report =
            SoundSwitchCommandClass.SoundSwitchConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, report.Volume);
        Assert.AreEqual((byte)1, report.DefaultToneIdentifier);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_MaxVolume()
    {
        // CC=0x79, Cmd=0x07, Volume=100, DefaultTone=10
        byte[] data = [0x79, 0x07, 0x64, 0x0A];
        CommandClassFrame frame = new(data);

        SoundSwitchConfigurationReport report =
            SoundSwitchCommandClass.SoundSwitchConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)100, report.Volume);
        Assert.AreEqual((byte)10, report.DefaultToneIdentifier);
    }

    [TestMethod]
    public void ConfigurationReport_Parse_TooShort_Throws()
    {
        // CC=0x79, Cmd=0x07, only 1 parameter byte
        byte[] data = [0x79, 0x07, 0x50];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => SoundSwitchCommandClass.SoundSwitchConfigurationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ConfigurationReport_Parse_NoParameters_Throws()
    {
        // CC=0x79, Cmd=0x07, no parameters
        byte[] data = [0x79, 0x07];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => SoundSwitchCommandClass.SoundSwitchConfigurationReportCommand.Parse(frame, NullLogger.Instance));
    }
}
