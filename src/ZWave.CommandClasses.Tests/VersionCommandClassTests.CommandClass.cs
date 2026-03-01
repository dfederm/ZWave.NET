using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class VersionCommandClassTests
{
    [TestMethod]
    public void CommandClassGetCommand_Create_HasCorrectFormat()
    {
        VersionCommandClass.VersionCommandClassGetCommand command =
            VersionCommandClass.VersionCommandClassGetCommand.Create(CommandClassId.Basic);

        Assert.AreEqual(CommandClassId.Version, VersionCommandClass.VersionCommandClassGetCommand.CommandClassId);
        Assert.AreEqual((byte)VersionCommand.CommandClassGet, VersionCommandClass.VersionCommandClassGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + RequestedCC
        Assert.AreEqual((byte)CommandClassId.Basic, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void CommandClassReport_Parse_SupportedCommandClass()
    {
        // CC=0x86, Cmd=0x14, RequestedCC=0x25(BinarySwitch), Version=2
        byte[] data = [0x86, 0x14, 0x25, 0x02];
        CommandClassFrame frame = new(data);

        (CommandClassId requestedCc, byte version) = VersionCommandClass.VersionCommandClassReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(CommandClassId.BinarySwitch, requestedCc);
        Assert.AreEqual((byte)2, version);
    }

    [TestMethod]
    public void CommandClassReport_Parse_UnsupportedCommandClass_ReturnsVersionZero()
    {
        // CC=0x86, Cmd=0x14, RequestedCC=0x25, Version=0 (not supported)
        byte[] data = [0x86, 0x14, 0x25, 0x00];
        CommandClassFrame frame = new(data);

        (CommandClassId _, byte version) = VersionCommandClass.VersionCommandClassReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, version);
    }

    [TestMethod]
    public void CommandClassReport_Parse_TooShort_Throws()
    {
        // CC=0x86, Cmd=0x14, only 1 byte of parameters (need 2)
        byte[] data = [0x86, 0x14, 0x25];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionCommandClassReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CommandClassReport_Parse_EmptyPayload_Throws()
    {
        byte[] data = [0x86, 0x14];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionCommandClassReportCommand.Parse(frame, NullLogger.Instance));
    }
}
