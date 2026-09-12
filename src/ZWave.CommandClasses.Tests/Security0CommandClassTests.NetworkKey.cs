using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class Security0CommandClassTests
{
    [TestMethod]
    public void NetworkKeySet_CreateParseRoundTrip()
    {
        byte[] key = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        Security0CommandClass.NetworkKeySetCommand command = Security0CommandClass.NetworkKeySetCommand.Create(key);

        byte[] parsed = Security0CommandClass.NetworkKeySetCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(Hex(key), Hex(parsed));
    }

    [TestMethod]
    public void NetworkKeySet_Create_WrongLength_Throws()
    {
        Assert.Throws<ZWaveException>(() => Security0CommandClass.NetworkKeySetCommand.Create(new byte[15]));
    }

    [TestMethod]
    public void NetworkKeySet_Parse_Malformed_Throws()
    {
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.NetworkKeySet, new byte[15]);

        Assert.Throws<ZWaveException>(() => Security0CommandClass.NetworkKeySetCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void NetworkKeyVerify_HasCorrectFormat()
    {
        // Spec §3.5.3.5: Network Key Verify carries no parameters.
        Security0CommandClass.NetworkKeyVerifyCommand command = Security0CommandClass.NetworkKeyVerifyCommand.Create();

        Assert.AreEqual((byte)Security0Command.NetworkKeyVerify, Security0CommandClass.NetworkKeyVerifyCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SchemeInherit_HasCorrectFormat()
    {
        Security0CommandClass.SchemeInheritCommand command = Security0CommandClass.SchemeInheritCommand.Create();

        Assert.AreEqual((byte)Security0Command.SchemeInherit, Security0CommandClass.SchemeInheritCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }
}
