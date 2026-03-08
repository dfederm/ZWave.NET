using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class UserCodeCommandClassTests
{
    [TestMethod]
    public void KeypadModeSetCommand_Create_HasCorrectFormat()
    {
        UserCodeCommandClass.KeypadModeSetCommand command =
            UserCodeCommandClass.KeypadModeSetCommand.Create(UserCodeKeypadMode.Vacation);

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.KeypadModeSetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.KeypadModeSet, UserCodeCommandClass.KeypadModeSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)UserCodeKeypadMode.Vacation, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void KeypadModeGetCommand_Create_HasCorrectFormat()
    {
        UserCodeCommandClass.KeypadModeGetCommand command =
            UserCodeCommandClass.KeypadModeGetCommand.Create();

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.KeypadModeGetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.KeypadModeGet, UserCodeCommandClass.KeypadModeGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void KeypadModeReport_Parse_NormalMode()
    {
        byte[] data = [0x63, 0x0A, 0x00];
        CommandClassFrame frame = new(data);

        UserCodeKeypadMode mode = UserCodeCommandClass.KeypadModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(UserCodeKeypadMode.Normal, mode);
    }

    [TestMethod]
    public void KeypadModeReport_Parse_LockedOutMode()
    {
        byte[] data = [0x63, 0x0A, 0x03];
        CommandClassFrame frame = new(data);

        UserCodeKeypadMode mode = UserCodeCommandClass.KeypadModeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(UserCodeKeypadMode.LockedOut, mode);
    }

    [TestMethod]
    public void KeypadModeReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x63, 0x0A];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.KeypadModeReportCommand.Parse(frame, NullLogger.Instance));
    }
}
