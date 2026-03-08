using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class UserCodeCommandClassTests
{
    [TestMethod]
    public void AdminCodeSetCommand_Create_WithCode()
    {
        UserCodeCommandClass.AdminCodeSetCommand command =
            UserCodeCommandClass.AdminCodeSetCommand.Create("1234");

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.AdminCodeSetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.AdminCodeSet, UserCodeCommandClass.AdminCodeSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (Reserved|Length) + 4 (Code) = 5
        Assert.AreEqual(5, parameters.Length);
        Assert.AreEqual((byte)4, parameters[0]); // Code length
        Assert.AreEqual((byte)0x31, parameters[1]); // '1'
        Assert.AreEqual((byte)0x32, parameters[2]); // '2'
        Assert.AreEqual((byte)0x33, parameters[3]); // '3'
        Assert.AreEqual((byte)0x34, parameters[4]); // '4'
    }

    [TestMethod]
    public void AdminCodeSetCommand_Create_Deactivate()
    {
        UserCodeCommandClass.AdminCodeSetCommand command =
            UserCodeCommandClass.AdminCodeSetCommand.Create(null);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (Reserved|Length=0)
        Assert.AreEqual(1, parameters.Length);
        Assert.AreEqual((byte)0, parameters[0]); // Code length=0
    }

    [TestMethod]
    public void AdminCodeSetCommand_Create_TooShort_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => UserCodeCommandClass.AdminCodeSetCommand.Create("AB"));
    }

    [TestMethod]
    public void AdminCodeSetCommand_Create_NonAscii_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => UserCodeCommandClass.AdminCodeSetCommand.Create("12\u00E934"));
    }

    [TestMethod]
    public void AdminCodeGetCommand_Create_HasCorrectFormat()
    {
        UserCodeCommandClass.AdminCodeGetCommand command =
            UserCodeCommandClass.AdminCodeGetCommand.Create();

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.AdminCodeGetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.AdminCodeGet, UserCodeCommandClass.AdminCodeGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void AdminCodeReport_Parse_WithCode()
    {
        // CC=0x63, Cmd=0x10, Reserved|Length=0x06, Code="123456"
        byte[] data = [0x63, 0x10, 0x06, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36];
        CommandClassFrame frame = new(data);

        string? adminCode = UserCodeCommandClass.AdminCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("123456", adminCode);
    }

    [TestMethod]
    public void AdminCodeReport_Parse_Deactivated()
    {
        // CC=0x63, Cmd=0x10, Reserved|Length=0x00
        byte[] data = [0x63, 0x10, 0x00];
        CommandClassFrame frame = new(data);

        string? adminCode = UserCodeCommandClass.AdminCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNull(adminCode);
    }

    [TestMethod]
    public void AdminCodeReport_Parse_MaxLengthCode()
    {
        // 10-character admin code
        byte[] data = [0x63, 0x10, 0x0A, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39];
        CommandClassFrame frame = new(data);

        string? adminCode = UserCodeCommandClass.AdminCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("0123456789", adminCode);
    }

    [TestMethod]
    public void AdminCodeReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x63, 0x10];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.AdminCodeReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void AdminCodeReport_Parse_TooShortForCode_Throws()
    {
        // Length says 4 but only 2 code bytes follow
        byte[] data = [0x63, 0x10, 0x04, 0x31, 0x32];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.AdminCodeReportCommand.Parse(frame, NullLogger.Instance));
    }
}
