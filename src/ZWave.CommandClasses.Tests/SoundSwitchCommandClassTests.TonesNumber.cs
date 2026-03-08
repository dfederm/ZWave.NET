using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class SoundSwitchCommandClassTests
{
    [TestMethod]
    public void TonesNumberGetCommand_Create_HasCorrectFormat()
    {
        SoundSwitchCommandClass.SoundSwitchTonesNumberGetCommand command =
            SoundSwitchCommandClass.SoundSwitchTonesNumberGetCommand.Create();

        Assert.AreEqual(CommandClassId.SoundSwitch, SoundSwitchCommandClass.SoundSwitchTonesNumberGetCommand.CommandClassId);
        Assert.AreEqual((byte)SoundSwitchCommand.TonesNumberGet, SoundSwitchCommandClass.SoundSwitchTonesNumberGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void TonesNumberReport_Parse_SingleTone()
    {
        // CC=0x79, Cmd=0x02, SupportedTones=1
        byte[] data = [0x79, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        byte tonesCount = SoundSwitchCommandClass.SoundSwitchTonesNumberReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, tonesCount);
    }

    [TestMethod]
    public void TonesNumberReport_Parse_MaxTones()
    {
        // CC=0x79, Cmd=0x02, SupportedTones=254 (max per spec)
        byte[] data = [0x79, 0x02, 0xFE];
        CommandClassFrame frame = new(data);

        byte tonesCount = SoundSwitchCommandClass.SoundSwitchTonesNumberReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)254, tonesCount);
    }

    [TestMethod]
    public void TonesNumberReport_Parse_TooShort_Throws()
    {
        // CC=0x79, Cmd=0x02, no parameters
        byte[] data = [0x79, 0x02];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => SoundSwitchCommandClass.SoundSwitchTonesNumberReportCommand.Parse(frame, NullLogger.Instance));
    }
}
