using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class UserCodeCommandClassTests
{
    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateBulk_SingleEntry()
    {
        List<ExtendedUserCodeEntry> entries =
        [
            new ExtendedUserCodeEntry(1, UserIdStatus.EnabledGrantAccess, "1234"),
        ];

        UserCodeCommandClass.ExtendedUserCodeSetCommand command =
            UserCodeCommandClass.ExtendedUserCodeSetCommand.CreateBulk(entries);

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.ExtendedUserCodeSetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.ExtendedUserCodeSet, UserCodeCommandClass.ExtendedUserCodeSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (count) + 2 (UserID) + 1 (Status) + 1 (Len) + 4 (Code) = 9
        Assert.AreEqual(9, parameters.Length);
        Assert.AreEqual((byte)1, parameters[0]); // count
        Assert.AreEqual((byte)0x00, parameters[1]); // UserID MSB
        Assert.AreEqual((byte)0x01, parameters[2]); // UserID LSB
        Assert.AreEqual((byte)UserIdStatus.EnabledGrantAccess, parameters[3]); // Status
        Assert.AreEqual((byte)4, parameters[4]); // Code length
        Assert.AreEqual((byte)0x31, parameters[5]); // '1'
        Assert.AreEqual((byte)0x32, parameters[6]); // '2'
        Assert.AreEqual((byte)0x33, parameters[7]); // '3'
        Assert.AreEqual((byte)0x34, parameters[8]); // '4'
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateBulk_MultipleEntries()
    {
        List<ExtendedUserCodeEntry> entries =
        [
            new ExtendedUserCodeEntry(1, UserIdStatus.EnabledGrantAccess, "1234"),
            new ExtendedUserCodeEntry(2, UserIdStatus.Available, null),
        ];

        UserCodeCommandClass.ExtendedUserCodeSetCommand command =
            UserCodeCommandClass.ExtendedUserCodeSetCommand.CreateBulk(entries);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (count) + (2+1+1+4) + (2+1+1+0) = 1 + 8 + 4 = 13
        Assert.AreEqual(13, parameters.Length);
        Assert.AreEqual((byte)2, parameters[0]); // count=2

        // Entry 2: Available, no code
        Assert.AreEqual((byte)0x00, parameters[9]); // UserID MSB
        Assert.AreEqual((byte)0x02, parameters[10]); // UserID LSB
        Assert.AreEqual((byte)UserIdStatus.Available, parameters[11]); // Status
        Assert.AreEqual((byte)0, parameters[12]); // Code length=0
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateBulk_TooShortCode_Throws()
    {
        List<ExtendedUserCodeEntry> entries =
        [
            new ExtendedUserCodeEntry(1, UserIdStatus.EnabledGrantAccess, "12"),
        ];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => UserCodeCommandClass.ExtendedUserCodeSetCommand.CreateBulk(entries));
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateBulk_NonAsciiCode_Throws()
    {
        List<ExtendedUserCodeEntry> entries =
        [
            new ExtendedUserCodeEntry(1, UserIdStatus.EnabledGrantAccess, "12\u00E934"),
        ];

        Assert.ThrowsExactly<ArgumentException>(
            () => UserCodeCommandClass.ExtendedUserCodeSetCommand.CreateBulk(entries));
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateBulk_AllowsNonDigitAscii()
    {
        // V2 Extended commands allow any ASCII, not just digits
        List<ExtendedUserCodeEntry> entries =
        [
            new ExtendedUserCodeEntry(1, UserIdStatus.EnabledGrantAccess, "AB12CD"),
        ];

        UserCodeCommandClass.ExtendedUserCodeSetCommand command =
            UserCodeCommandClass.ExtendedUserCodeSetCommand.CreateBulk(entries);

        // Should not throw — just verify it created successfully
        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.ExtendedUserCodeSetCommand.CommandClassId);
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateSingular_HasCorrectFormat()
    {
        UserCodeCommandClass.ExtendedUserCodeSetCommand command =
            UserCodeCommandClass.ExtendedUserCodeSetCommand.Create(1, UserIdStatus.EnabledGrantAccess, "1234");

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (count) + 2 (UserID) + 1 (Status) + 1 (Len) + 4 (Code) = 9
        Assert.AreEqual(9, parameters.Length);
        Assert.AreEqual((byte)1, parameters[0]); // count
        Assert.AreEqual((byte)0x00, parameters[1]); // UserID MSB
        Assert.AreEqual((byte)0x01, parameters[2]); // UserID LSB
        Assert.AreEqual((byte)UserIdStatus.EnabledGrantAccess, parameters[3]);
        Assert.AreEqual((byte)4, parameters[4]); // Code length
        Assert.AreEqual((byte)0x31, parameters[5]);
        Assert.AreEqual((byte)0x32, parameters[6]);
        Assert.AreEqual((byte)0x33, parameters[7]);
        Assert.AreEqual((byte)0x34, parameters[8]);
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateSingular_LargeUserId()
    {
        UserCodeCommandClass.ExtendedUserCodeSetCommand command =
            UserCodeCommandClass.ExtendedUserCodeSetCommand.Create(500, UserIdStatus.Disabled, "5678");

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)0x01, parameters[1]); // 500 = 0x01F4
        Assert.AreEqual((byte)0xF4, parameters[2]);
        Assert.AreEqual((byte)UserIdStatus.Disabled, parameters[3]);
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateSingular_TooShort_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => UserCodeCommandClass.ExtendedUserCodeSetCommand.Create(1, UserIdStatus.EnabledGrantAccess, "12"));
    }

    [TestMethod]
    public void ExtendedUserCodeSetCommand_CreateClear_HasCorrectFormat()
    {
        UserCodeCommandClass.ExtendedUserCodeSetCommand command =
            UserCodeCommandClass.ExtendedUserCodeSetCommand.CreateClear(42);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // 1 (count) + 2 (UserID) + 1 (Status=Available) + 1 (Len=0) = 5
        Assert.AreEqual(5, parameters.Length);
        Assert.AreEqual((byte)1, parameters[0]); // count
        Assert.AreEqual((byte)0x00, parameters[1]); // UserID MSB
        Assert.AreEqual((byte)42, parameters[2]); // UserID LSB
        Assert.AreEqual((byte)UserIdStatus.Available, parameters[3]);
        Assert.AreEqual((byte)0, parameters[4]); // Code length=0
    }

    [TestMethod]
    public void ExtendedUserCodeGetCommand_Create_WithoutReportMore()
    {
        UserCodeCommandClass.ExtendedUserCodeGetCommand command =
            UserCodeCommandClass.ExtendedUserCodeGetCommand.Create(100, reportMore: false);

        Assert.AreEqual(CommandClassId.UserCode, UserCodeCommandClass.ExtendedUserCodeGetCommand.CommandClassId);
        Assert.AreEqual((byte)UserCodeCommand.ExtendedUserCodeGet, UserCodeCommandClass.ExtendedUserCodeGetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        Assert.AreEqual((byte)0x00, parameters[0]); // UserID MSB
        Assert.AreEqual((byte)100, parameters[1]); // UserID LSB
        Assert.AreEqual((byte)0x00, parameters[2]); // ReportMore=false
    }

    [TestMethod]
    public void ExtendedUserCodeGetCommand_Create_WithReportMore()
    {
        UserCodeCommandClass.ExtendedUserCodeGetCommand command =
            UserCodeCommandClass.ExtendedUserCodeGetCommand.Create(256, reportMore: true);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, parameters.Length);
        Assert.AreEqual((byte)0x01, parameters[0]); // UserID MSB (256 = 0x0100)
        Assert.AreEqual((byte)0x00, parameters[1]); // UserID LSB
        Assert.AreEqual((byte)0x01, parameters[2]); // ReportMore=true
    }

    [TestMethod]
    public void ExtendedUserCodeReport_ParseBulk_SingleEntry()
    {
        // CC=0x63, Cmd=0x0D, Count=1, UserID=0x0001, Status=0x01 (Enabled),
        // Reserved|Len=0x04, Code="5678", NextUserID=0x0002
        byte[] data =
        [
            0x63, 0x0D, // CC + Cmd
            0x01,       // count
            0x00, 0x01, // UserID
            0x01,       // Status (EnabledGrantAccess)
            0x04,       // Code length
            0x35, 0x36, 0x37, 0x38, // "5678"
            0x00, 0x02, // NextUserID
        ];
        CommandClassFrame frame = new(data);

        ExtendedUserCodeReport report =
            UserCodeCommandClass.ExtendedUserCodeReportCommand.ParseBulk(frame, NullLogger.Instance);

        Assert.HasCount(1, report.Entries);
        Assert.AreEqual((ushort)1, report.Entries[0].UserIdentifier);
        Assert.AreEqual(UserIdStatus.EnabledGrantAccess, report.Entries[0].Status);
        Assert.AreEqual("5678", report.Entries[0].UserCode);
        Assert.AreEqual((ushort)2, report.NextUserIdentifier);
    }

    [TestMethod]
    public void ExtendedUserCodeReport_Parse_ReturnsFirstEntry()
    {
        byte[] data =
        [
            0x63, 0x0D, // CC + Cmd
            0x01,       // count
            0x00, 0x01, // UserID
            0x01,       // Status (EnabledGrantAccess)
            0x04,       // Code length
            0x35, 0x36, 0x37, 0x38, // "5678"
            0x00, 0x02, // NextUserID
        ];
        CommandClassFrame frame = new(data);

        ExtendedUserCodeEntry entry =
            UserCodeCommandClass.ExtendedUserCodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)1, entry.UserIdentifier);
        Assert.AreEqual(UserIdStatus.EnabledGrantAccess, entry.Status);
        Assert.AreEqual("5678", entry.UserCode);
    }

    [TestMethod]
    public void ExtendedUserCodeReport_ParseBulk_MultipleEntries()
    {
        // Count=2: Entry1 (ID=1, Enabled, "1234"), Entry2 (ID=3, PassageMode, "9277"), NextUserID=0
        byte[] data =
        [
            0x63, 0x0D, // CC + Cmd
            0x02,       // count
            0x00, 0x01, // UserID 1
            0x01,       // Status (EnabledGrantAccess)
            0x04,       // Code length
            0x31, 0x32, 0x33, 0x34, // "1234"
            0x00, 0x03, // UserID 3
            0x04,       // Status (PassageMode)
            0x04,       // Code length
            0x39, 0x32, 0x37, 0x37, // "9277"
            0x00, 0x00, // NextUserID=0 (no more)
        ];
        CommandClassFrame frame = new(data);

        ExtendedUserCodeReport report =
            UserCodeCommandClass.ExtendedUserCodeReportCommand.ParseBulk(frame, NullLogger.Instance);

        Assert.HasCount(2, report.Entries);

        Assert.AreEqual((ushort)1, report.Entries[0].UserIdentifier);
        Assert.AreEqual(UserIdStatus.EnabledGrantAccess, report.Entries[0].Status);
        Assert.AreEqual("1234", report.Entries[0].UserCode);

        Assert.AreEqual((ushort)3, report.Entries[1].UserIdentifier);
        Assert.AreEqual(UserIdStatus.PassageMode, report.Entries[1].Status);
        Assert.AreEqual("9277", report.Entries[1].UserCode);

        Assert.AreEqual((ushort)0, report.NextUserIdentifier);
    }

    [TestMethod]
    public void ExtendedUserCodeReport_ParseBulk_AvailableStatus()
    {
        // Available status: code length=0, no code bytes
        byte[] data =
        [
            0x63, 0x0D, // CC + Cmd
            0x01,       // count
            0x00, 0x05, // UserID 5
            0x00,       // Status (Available)
            0x00,       // Code length=0
            0x00, 0x06, // NextUserID=6
        ];
        CommandClassFrame frame = new(data);

        ExtendedUserCodeReport report =
            UserCodeCommandClass.ExtendedUserCodeReportCommand.ParseBulk(frame, NullLogger.Instance);

        Assert.HasCount(1, report.Entries);
        Assert.AreEqual((ushort)5, report.Entries[0].UserIdentifier);
        Assert.AreEqual(UserIdStatus.Available, report.Entries[0].Status);
        Assert.IsNull(report.Entries[0].UserCode);
        Assert.AreEqual((ushort)6, report.NextUserIdentifier);
    }

    [TestMethod]
    public void ExtendedUserCodeReport_ParseBulk_StatusNotAvailable()
    {
        // StatusNotAvailable (0xFE): code length=0, NextUserID=0
        byte[] data =
        [
            0x63, 0x0D, // CC + Cmd
            0x01,       // count
            0xFF, 0xFF, // UserID 65535 (invalid)
            0xFE,       // Status (StatusNotAvailable)
            0x00,       // Code length=0
            0x00, 0x00, // NextUserID=0
        ];
        CommandClassFrame frame = new(data);

        ExtendedUserCodeReport report =
            UserCodeCommandClass.ExtendedUserCodeReportCommand.ParseBulk(frame, NullLogger.Instance);

        Assert.HasCount(1, report.Entries);
        Assert.AreEqual(UserIdStatus.StatusNotAvailable, report.Entries[0].Status);
        Assert.IsNull(report.Entries[0].UserCode);
        Assert.AreEqual((ushort)0, report.NextUserIdentifier);
    }

    [TestMethod]
    public void ExtendedUserCodeReport_ParseBulk_TooShort_Throws()
    {
        // Only CC + Cmd + count, no user code blocks
        byte[] data = [0x63, 0x0D, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.ExtendedUserCodeReportCommand.ParseBulk(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ExtendedUserCodeReport_ParseBulk_ZeroBlocks_Throws()
    {
        // Count=0 is invalid per spec
        byte[] data = [0x63, 0x0D, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => UserCodeCommandClass.ExtendedUserCodeReportCommand.ParseBulk(frame, NullLogger.Instance));
    }
}
