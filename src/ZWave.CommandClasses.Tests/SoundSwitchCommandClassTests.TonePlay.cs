using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class SoundSwitchCommandClassTests
{
    [TestMethod]
    public void TonePlaySetCommand_Create_Version1_PlayTone()
    {
        SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.Create(1, 0x05, null);

        Assert.AreEqual(CommandClassId.SoundSwitch, SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.CommandClassId);
        Assert.AreEqual((byte)SoundSwitchCommand.TonePlaySet, SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.CommandId);
        // V1: only tone identifier byte
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x05, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void TonePlaySetCommand_Create_Version1_StopTone()
    {
        SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.Create(1, 0x00, null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void TonePlaySetCommand_Create_Version1_PlayDefault()
    {
        SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.Create(1, 0xFF, null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void TonePlaySetCommand_Create_Version2_PlayToneWithVolume()
    {
        SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.Create(2, 0x03, 80);

        // V2: tone identifier + volume byte
        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x03, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)80, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void TonePlaySetCommand_Create_Version2_PlayToneWithConfiguredVolume()
    {
        // Volume=0x00 means "use configured volume"
        SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.Create(2, 0x03, null);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x03, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void TonePlaySetCommand_Create_Version2_PlayDefaultToneWithRestoreVolume()
    {
        // ToneId=0xFF (default), Volume=0xFF (restore non-zero)
        SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.Create(2, 0xFF, 0xFF);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void TonePlaySetCommand_Create_Version2_StopTone_VolumeForced0()
    {
        // Per spec CC:0079.02.08.11.007: volume MUST be 0x00 when tone identifier is 0x00
        SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlaySetCommand.Create(2, 0x00, 80);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void TonePlayGetCommand_Create_HasCorrectFormat()
    {
        SoundSwitchCommandClass.SoundSwitchTonePlayGetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonePlayGetCommand.Create();

        Assert.AreEqual(CommandClassId.SoundSwitch, SoundSwitchCommandClass.SoundSwitchTonePlayGetCommand.CommandClassId);
        Assert.AreEqual((byte)SoundSwitchCommand.TonePlayGet, SoundSwitchCommandClass.SoundSwitchTonePlayGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void TonePlayReport_Parse_Version1_Playing()
    {
        // CC=0x79, Cmd=0x0A, ToneId=3
        byte[] data = [0x79, 0x0A, 0x03];
        CommandClassFrame frame = new(data);

        SoundSwitchTonePlayReport report =
            SoundSwitchCommandClass.SoundSwitchTonePlayReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)3, report.ToneIdentifier);
        Assert.IsNull(report.Volume);
    }

    [TestMethod]
    public void TonePlayReport_Parse_Version1_NotPlaying()
    {
        // CC=0x79, Cmd=0x0A, ToneId=0
        byte[] data = [0x79, 0x0A, 0x00];
        CommandClassFrame frame = new(data);

        SoundSwitchTonePlayReport report =
            SoundSwitchCommandClass.SoundSwitchTonePlayReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, report.ToneIdentifier);
        Assert.IsNull(report.Volume);
    }

    [TestMethod]
    public void TonePlayReport_Parse_Version2_PlayingWithVolume()
    {
        // CC=0x79, Cmd=0x0A, ToneId=5, Volume=80
        byte[] data = [0x79, 0x0A, 0x05, 0x50];
        CommandClassFrame frame = new(data);

        SoundSwitchTonePlayReport report =
            SoundSwitchCommandClass.SoundSwitchTonePlayReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)5, report.ToneIdentifier);
        Assert.IsNotNull(report.Volume);
        Assert.AreEqual((byte)80, report.Volume.Value);
    }

    [TestMethod]
    public void TonePlayReport_Parse_Version2_NotPlayingWithMutedVolume()
    {
        // CC=0x79, Cmd=0x0A, ToneId=0, Volume=0
        byte[] data = [0x79, 0x0A, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        SoundSwitchTonePlayReport report =
            SoundSwitchCommandClass.SoundSwitchTonePlayReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, report.ToneIdentifier);
        Assert.IsNotNull(report.Volume);
        Assert.AreEqual((byte)0, report.Volume.Value);
    }

    [TestMethod]
    public void TonePlayReport_Parse_Version2_MaxVolume()
    {
        // CC=0x79, Cmd=0x0A, ToneId=1, Volume=100 (0x64)
        byte[] data = [0x79, 0x0A, 0x01, 0x64];
        CommandClassFrame frame = new(data);

        SoundSwitchTonePlayReport report =
            SoundSwitchCommandClass.SoundSwitchTonePlayReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.ToneIdentifier);
        Assert.IsNotNull(report.Volume);
        Assert.AreEqual((byte)100, report.Volume.Value);
    }

    [TestMethod]
    public void TonePlayReport_Parse_TooShort_Throws()
    {
        // CC=0x79, Cmd=0x0A, no parameters
        byte[] data = [0x79, 0x0A];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => SoundSwitchCommandClass.SoundSwitchTonePlayReportCommand.Parse(frame, NullLogger.Instance));
    }
}
