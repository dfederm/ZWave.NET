using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class UserCodeCommandClassTests
{
    [TestMethod]
    public void ChecksumGetCommand_Create_HasCorrectFormat()
    {
        UserCodeCommandClass.ChecksumGetCommand command =
            UserCodeCommandClass.ChecksumGetCommand.Create();

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.ChecksumGetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.ChecksumGet, UserCodeCommandClass.ChecksumGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void ChecksumReport_Parse_ValidChecksum()
    {
        // CC=0x63, Cmd=0x12, Checksum=0xEAAD (from spec example)
        byte[] data = [0x63, 0x12, 0xEA, 0xAD];
        CommandClassFrame frame = new(data);

        ushort checksum = UserCodeCommandClass.ChecksumReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)0xEAAD, checksum);
    }

    [TestMethod]
    public void ChecksumReport_Parse_NoCodesSet()
    {
        // Per spec CC:0063.02.12.11.002: MUST be 0x0000 if no User Code is set
        byte[] data = [0x63, 0x12, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        ushort checksum = UserCodeCommandClass.ChecksumReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)0x0000, checksum);
    }

    [TestMethod]
    public void ChecksumReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x63, 0x12, 0xEA];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.ChecksumReportCommand.Parse(frame, NullLogger.Instance));
    }
}
