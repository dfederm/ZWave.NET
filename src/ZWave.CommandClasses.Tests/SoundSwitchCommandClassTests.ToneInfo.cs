using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class SoundSwitchCommandClassTests
{
    [TestMethod]
    public void ToneInfoGetCommand_Create_HasCorrectFormat()
    {
        SoundSwitchCommandClass.SoundSwitchToneInfoGetCommand command =
            SoundSwitchCommandClass.SoundSwitchToneInfoGetCommand.Create(5);

        Assert.AreEqual(CommandClassId.SoundSwitch, SoundSwitchCommandClass.SoundSwitchToneInfoGetCommand.CommandClassId);
        Assert.AreEqual((byte)SoundSwitchCommand.ToneInfoGet, SoundSwitchCommandClass.SoundSwitchToneInfoGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)5, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ToneInfoGetCommand_Create_ToneIdentifier1()
    {
        SoundSwitchCommandClass.SoundSwitchToneInfoGetCommand command =
            SoundSwitchCommandClass.SoundSwitchToneInfoGetCommand.Create(1);

        Assert.AreEqual((byte)1, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ToneInfoReport_Parse_ShortName()
    {
        // CC=0x79, Cmd=0x04, ToneId=1, Duration=0x000A (10 sec), NameLen=4, Name="Ding"
        byte[] data = [0x79, 0x04, 0x01, 0x00, 0x0A, 0x04, 0x44, 0x69, 0x6E, 0x67];
        CommandClassFrame frame = new(data);

        SoundSwitchToneInfo info = SoundSwitchCommandClass.SoundSwitchToneInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, info.ToneIdentifier);
        Assert.AreEqual((ushort)10, info.DurationSeconds);
        Assert.AreEqual("Ding", info.Name);
    }

    [TestMethod]
    public void ToneInfoReport_Parse_LongerName()
    {
        // CC=0x79, Cmd=0x04, ToneId=3, Duration=0x001E (30 sec), NameLen=11, Name="Fire Alarm!"
        byte[] nameBytes = "Fire Alarm!"u8.ToArray();
        byte[] data = [0x79, 0x04, 0x03, 0x00, 0x1E, (byte)nameBytes.Length, .. nameBytes];
        CommandClassFrame frame = new(data);

        SoundSwitchToneInfo info = SoundSwitchCommandClass.SoundSwitchToneInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)3, info.ToneIdentifier);
        Assert.AreEqual((ushort)30, info.DurationSeconds);
        Assert.AreEqual("Fire Alarm!", info.Name);
    }

    [TestMethod]
    public void ToneInfoReport_Parse_Utf8MultiByte()
    {
        // CC=0x79, Cmd=0x04, ToneId=2, Duration=0x0005 (5 sec), Name with UTF-8 multi-byte chars "Tö"
        byte[] nameBytes = "Tö"u8.ToArray();
        byte[] data = [0x79, 0x04, 0x02, 0x00, 0x05, (byte)nameBytes.Length, .. nameBytes];
        CommandClassFrame frame = new(data);

        SoundSwitchToneInfo info = SoundSwitchCommandClass.SoundSwitchToneInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, info.ToneIdentifier);
        Assert.AreEqual((ushort)5, info.DurationSeconds);
        Assert.AreEqual("Tö", info.Name);
    }

    [TestMethod]
    public void ToneInfoReport_Parse_ZeroDurationAndEmptyName()
    {
        // Per spec: unsupported tone identifier returns zero duration and zero-length name
        // CC=0x79, Cmd=0x04, ToneId=0, Duration=0x0000, NameLen=0
        byte[] data = [0x79, 0x04, 0x00, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        SoundSwitchToneInfo info = SoundSwitchCommandClass.SoundSwitchToneInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, info.ToneIdentifier);
        Assert.AreEqual((ushort)0, info.DurationSeconds);
        Assert.AreEqual(string.Empty, info.Name);
    }

    [TestMethod]
    public void ToneInfoReport_Parse_MaxDuration()
    {
        // CC=0x79, Cmd=0x04, ToneId=1, Duration=0xFFFF (65535 sec), NameLen=1, Name="X"
        byte[] data = [0x79, 0x04, 0x01, 0xFF, 0xFF, 0x01, 0x58];
        CommandClassFrame frame = new(data);

        SoundSwitchToneInfo info = SoundSwitchCommandClass.SoundSwitchToneInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, info.ToneIdentifier);
        Assert.AreEqual((ushort)65535, info.DurationSeconds);
        Assert.AreEqual("X", info.Name);
    }

    [TestMethod]
    public void ToneInfoReport_Parse_TooShort_Throws()
    {
        // CC=0x79, Cmd=0x04, only 2 parameter bytes (need at least 4)
        byte[] data = [0x79, 0x04, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => SoundSwitchCommandClass.SoundSwitchToneInfoReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ToneInfoReport_Parse_NameLengthExceedsPayload_Throws()
    {
        // CC=0x79, Cmd=0x04, ToneId=1, Duration=0x000A, NameLen=10 but only 3 name bytes
        byte[] data = [0x79, 0x04, 0x01, 0x00, 0x0A, 0x0A, 0x41, 0x42, 0x43];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => SoundSwitchCommandClass.SoundSwitchToneInfoReportCommand.Parse(frame, NullLogger.Instance));
    }
}
