using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class UserCodeCommandClassTests
{
    [TestMethod]
    public void UserCodeSetCommand_Create_AvailableStatus()
    {
        UserCodeCommandClass.UserCodeSetCommand command =
            UserCodeCommandClass.UserCodeSetCommand.CreateClear(1);

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.UserCodeSetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.Set, UserCodeCommandClass.UserCodeSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // UserIdentifier(1) + Status(1) + Code(4 zero bytes)
        Assert.AreEqual(6, parameters.Length);
        Assert.AreEqual((byte)1, parameters[0]);
        Assert.AreEqual((byte)UserIdStatus.Available, parameters[1]);
        // Per spec CC:0063.01.01.11.009: code MUST be 0x00000000 when status is Available
        Assert.AreEqual((byte)0x00, parameters[2]);
        Assert.AreEqual((byte)0x00, parameters[3]);
        Assert.AreEqual((byte)0x00, parameters[4]);
        Assert.AreEqual((byte)0x00, parameters[5]);
    }

    [TestMethod]
    public void UserCodeSetCommand_Create_OccupiedStatus()
    {
        UserCodeCommandClass.UserCodeSetCommand command =
            UserCodeCommandClass.UserCodeSetCommand.Create(5, UserIdStatus.EnabledGrantAccess, "1234");

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // UserIdentifier(1) + Status(1) + Code(4)
        Assert.AreEqual(6, parameters.Length);
        Assert.AreEqual((byte)5, parameters[0]);
        Assert.AreEqual((byte)UserIdStatus.EnabledGrantAccess, parameters[1]);
        // ASCII "1234" = 0x31, 0x32, 0x33, 0x34
        Assert.AreEqual((byte)0x31, parameters[2]);
        Assert.AreEqual((byte)0x32, parameters[3]);
        Assert.AreEqual((byte)0x33, parameters[4]);
        Assert.AreEqual((byte)0x34, parameters[5]);
    }

    [TestMethod]
    public void UserCodeSetCommand_CreateClear_AllUsers()
    {
        UserCodeCommandClass.UserCodeSetCommand command =
            UserCodeCommandClass.UserCodeSetCommand.CreateClear(0);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0, parameters[0]);
        Assert.AreEqual((byte)UserIdStatus.Available, parameters[1]);
    }

    [TestMethod]
    public void UserCodeGetCommand_Create_HasCorrectFormat()
    {
        UserCodeCommandClass.UserCodeGetCommand command =
            UserCodeCommandClass.UserCodeGetCommand.Create(3);

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.UserCodeGetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.Get, UserCodeCommandClass.UserCodeGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)3, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void UserCodeSetCommand_Create_TooShort_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => UserCodeCommandClass.UserCodeSetCommand.Create(1, UserIdStatus.EnabledGrantAccess, "123"));
    }

    [TestMethod]
    public void UserCodeSetCommand_Create_TooLong_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => UserCodeCommandClass.UserCodeSetCommand.Create(1, UserIdStatus.EnabledGrantAccess, "12345678901"));
    }

    [TestMethod]
    public void UserCodeSetCommand_Create_NonDigit_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => UserCodeCommandClass.UserCodeSetCommand.Create(1, UserIdStatus.EnabledGrantAccess, "12AB"));
    }

    [TestMethod]
    public void UserCodeReport_Parse_OccupiedStatus()
    {
        // CC=0x63, Cmd=0x03, UserID=1, Status=0x01 (Occupied), Code="1234" (0x31-0x34)
        byte[] data = [0x63, 0x03, 0x01, 0x01, 0x31, 0x32, 0x33, 0x34];
        CommandClassFrame frame = new(data);

        UserCodeReport report = UserCodeCommandClass.UserCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.UserIdentifier);
        Assert.AreEqual(UserIdStatus.EnabledGrantAccess, report.Status);
        Assert.AreEqual("1234", report.UserCode);
    }

    [TestMethod]
    public void UserCodeReport_Parse_AvailableStatus()
    {
        // CC=0x63, Cmd=0x03, UserID=2, Status=0x00 (Available), Code=0x00000000
        byte[] data = [0x63, 0x03, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        UserCodeReport report = UserCodeCommandClass.UserCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.UserIdentifier);
        Assert.AreEqual(UserIdStatus.Available, report.Status);
        Assert.IsNull(report.UserCode);
    }

    [TestMethod]
    public void UserCodeReport_Parse_StatusNotAvailable()
    {
        // CC=0x63, Cmd=0x03, UserID=255, Status=0xFE (StatusNotAvailable)
        byte[] data = [0x63, 0x03, 0xFF, 0xFE];
        CommandClassFrame frame = new(data);

        UserCodeReport report = UserCodeCommandClass.UserCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)255, report.UserIdentifier);
        Assert.AreEqual(UserIdStatus.StatusNotAvailable, report.Status);
        Assert.IsNull(report.UserCode);
    }

    [TestMethod]
    public void UserCodeReport_Parse_LongCode()
    {
        // 10-digit code "1234567890"
        byte[] data = [0x63, 0x03, 0x01, 0x01, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30];
        CommandClassFrame frame = new(data);

        UserCodeReport report = UserCodeCommandClass.UserCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("1234567890", report.UserCode);
    }

    [TestMethod]
    public void UserCodeReport_Parse_TooShort_Throws()
    {
        // Only CC and Cmd, no parameters
        byte[] data = [0x63, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.UserCodeReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void UsersNumberGetCommand_Create_HasCorrectFormat()
    {
        UserCodeCommandClass.UsersNumberGetCommand command =
            UserCodeCommandClass.UsersNumberGetCommand.Create();

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.UsersNumberGetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.UsersNumberGet, UserCodeCommandClass.UsersNumberGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void UsersNumberReport_Parse_V1()
    {
        // CC=0x63, Cmd=0x05, SupportedUsers=10
        byte[] data = [0x63, 0x05, 0x0A];
        CommandClassFrame frame = new(data);

        ushort count = UserCodeCommandClass.UsersNumberReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)10, count);
    }

    [TestMethod]
    public void UsersNumberReport_Parse_V2_Under256()
    {
        // CC=0x63, Cmd=0x05, SupportedUsers=50, ExtendedSupportedUsers=0x0032 (50)
        byte[] data = [0x63, 0x05, 0x32, 0x00, 0x32];
        CommandClassFrame frame = new(data);

        ushort count = UserCodeCommandClass.UsersNumberReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)50, count);
    }

    [TestMethod]
    public void UsersNumberReport_Parse_V2_Over255()
    {
        // CC=0x63, Cmd=0x05, SupportedUsers=255, ExtendedSupportedUsers=0x01F4 (500)
        byte[] data = [0x63, 0x05, 0xFF, 0x01, 0xF4];
        CommandClassFrame frame = new(data);

        ushort count = UserCodeCommandClass.UsersNumberReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)500, count);
    }

    [TestMethod]
    public void UsersNumberReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x63, 0x05];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.UsersNumberReportCommand.Parse(frame, NullLogger.Instance));
    }
}
