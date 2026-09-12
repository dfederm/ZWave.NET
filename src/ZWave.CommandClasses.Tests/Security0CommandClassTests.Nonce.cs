using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class Security0CommandClassTests
{
    [TestMethod]
    public void NonceGet_HasCorrectFormat()
    {
        Security0CommandClass.NonceGetCommand command = Security0CommandClass.NonceGetCommand.Create();

        Assert.AreEqual(CommandClassId.Security0, Security0CommandClass.NonceGetCommand.CommandClassId);
        Assert.AreEqual((byte)Security0Command.NonceGet, Security0CommandClass.NonceGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
        Assert.AreEqual(0, command.Frame.CommandParameters.Length);
    }

    [TestMethod]
    public void NonceReport_CreateParseRoundTrip()
    {
        byte[] nonce = [1, 2, 3, 4, 5, 6, 7, 8];
        Security0CommandClass.NonceReportCommand report = Security0CommandClass.NonceReportCommand.Create(nonce);

        Security0Nonce parsed = Security0CommandClass.NonceReportCommand.Parse(report.Frame, NullLogger.Instance);

        Assert.AreEqual(Hex(nonce), Hex(parsed.Nonce.ToArray()));
        Assert.AreEqual((byte)1, parsed.NonceId);
    }

    [TestMethod]
    public void NonceReport_Create_WrongLength_Throws()
    {
        Assert.Throws<ZWaveException>(() => Security0CommandClass.NonceReportCommand.Create(new byte[7]));
    }

    [TestMethod]
    public void NonceReport_Parse_Malformed_Throws()
    {
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.NonceReport, [1, 2, 3]);

        Assert.Throws<ZWaveException>(() => Security0CommandClass.NonceReportCommand.Parse(frame, NullLogger.Instance));
    }
}
