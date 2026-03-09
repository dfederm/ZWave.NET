using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ConfigurationCommandClassTests
{
    [TestMethod]
    public void BulkSetCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationBulkSetCommand command =
            ConfigurationCommandClass.ConfigurationBulkSetCommand.Create(
                parameterOffset: 10,
                size: 2,
                values: [100, 200],
                restoreDefault: false,
                handshake: false);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationBulkSetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.BulkSet, ConfigurationCommandClass.ConfigurationBulkSetCommand.CommandId);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, span[0]); // Offset MSB
        Assert.AreEqual(0x0A, span[1]); // Offset LSB = 10
        Assert.AreEqual(0x02, span[2]); // Number of parameters
        Assert.AreEqual(0x02, span[3]); // Flags: size=2, no default, no handshake
        // Parameter 1: 100 = 0x0064
        Assert.AreEqual(0x00, span[4]);
        Assert.AreEqual(0x64, span[5]);
        // Parameter 2: 200 = 0x00C8
        Assert.AreEqual(0x00, span[6]);
        Assert.AreEqual(0xC8, span[7]);
    }

    [TestMethod]
    public void BulkSetCommand_Create_WithDefaultFlag()
    {
        ConfigurationCommandClass.ConfigurationBulkSetCommand command =
            ConfigurationCommandClass.ConfigurationBulkSetCommand.Create(
                parameterOffset: 1,
                size: 1,
                values: [0],
                restoreDefault: true,
                handshake: false);

        byte flags = command.Frame.CommandParameters.Span[3];
        Assert.AreNotEqual((byte)0, (byte)(flags & 0b1000_0000)); // Default bit set
    }

    [TestMethod]
    public void BulkSetCommand_Create_WithHandshakeFlag()
    {
        ConfigurationCommandClass.ConfigurationBulkSetCommand command =
            ConfigurationCommandClass.ConfigurationBulkSetCommand.Create(
                parameterOffset: 1,
                size: 1,
                values: [0],
                restoreDefault: false,
                handshake: true);

        byte flags = command.Frame.CommandParameters.Span[3];
        Assert.AreNotEqual((byte)0, (byte)(flags & 0b0100_0000)); // Handshake bit set
    }

    [TestMethod]
    public void BulkSetCommand_CreateDefault_HasDefaultFlag()
    {
        ConfigurationCommandClass.ConfigurationBulkSetCommand command =
            ConfigurationCommandClass.ConfigurationBulkSetCommand.CreateDefault(5, 3);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, span[0]); // Offset MSB
        Assert.AreEqual(0x05, span[1]); // Offset LSB = 5
        Assert.AreEqual(0x03, span[2]); // Number of parameters = 3
        Assert.AreNotEqual((byte)0, (byte)(span[3] & 0b1000_0000)); // Default bit set
    }

    [TestMethod]
    public void BulkGetCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationBulkGetCommand command =
            ConfigurationCommandClass.ConfigurationBulkGetCommand.Create(256, 5);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationBulkGetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.BulkGet, ConfigurationCommandClass.ConfigurationBulkGetCommand.CommandId);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(3, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x01, span[0]); // Offset MSB = 256 >> 8
        Assert.AreEqual(0x00, span[1]); // Offset LSB
        Assert.AreEqual(0x05, span[2]); // Number of parameters
    }

    [TestMethod]
    public void BulkReportCommand_Parse_SingleParameter()
    {
        byte[] data =
        [
            0x70, 0x09,       // CC + Cmd
            0x00, 0x01,       // Offset = 1
            0x01,             // Number of parameters = 1
            0x00,             // Reports to follow = 0
            0x01,             // Flags: size=1, no default, no handshake
            0x2A,             // Value = 42
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationBulkReport report, byte reportsToFollow) =
            ConfigurationCommandClass.ConfigurationBulkReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)1, report.ParameterOffset);
        Assert.IsFalse(report.IsDefault);
        Assert.IsFalse(report.IsHandshake);
        Assert.AreEqual((byte)1, report.Size);
        Assert.HasCount(1, report.Values);
        Assert.AreEqual(42, report.Values[0]);
        Assert.AreEqual((byte)0, reportsToFollow);
    }

    [TestMethod]
    public void BulkReportCommand_Parse_MultipleParameters()
    {
        byte[] data =
        [
            0x70, 0x09,       // CC + Cmd
            0x00, 0x0A,       // Offset = 10
            0x03,             // Number of parameters = 3
            0x00,             // Reports to follow = 0
            0x02,             // Flags: size=2
            0x00, 0x64,       // Param 10 = 100
            0x00, 0xC8,       // Param 11 = 200
            0x01, 0x2C,       // Param 12 = 300
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationBulkReport report, byte reportsToFollow) =
            ConfigurationCommandClass.ConfigurationBulkReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)10, report.ParameterOffset);
        Assert.AreEqual((byte)2, report.Size);
        Assert.HasCount(3, report.Values);
        Assert.AreEqual(100, report.Values[0]);
        Assert.AreEqual(200, report.Values[1]);
        Assert.AreEqual(300, report.Values[2]);
        Assert.AreEqual((byte)0, reportsToFollow);
    }

    [TestMethod]
    public void BulkReportCommand_Parse_WithFlags()
    {
        byte[] data =
        [
            0x70, 0x09,
            0x00, 0x01,       // Offset = 1
            0x01,             // Number of parameters = 1
            0x02,             // Reports to follow = 2
            0b1100_0001,      // Default=1, Handshake=1, Size=1
            0x00,             // Value
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationBulkReport report, byte reportsToFollow) =
            ConfigurationCommandClass.ConfigurationBulkReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.IsDefault);
        Assert.IsTrue(report.IsHandshake);
        Assert.AreEqual((byte)2, reportsToFollow);
    }

    [TestMethod]
    public void BulkReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x70, 0x09, 0x00, 0x01]; // Only 2 bytes of parameters
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationBulkReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void BulkReportCommand_Parse_TooShortForDeclaredParams_Throws()
    {
        byte[] data =
        [
            0x70, 0x09,
            0x00, 0x01,       // Offset = 1
            0x03,             // Number of parameters = 3
            0x00,             // Reports to follow
            0x04,             // Size = 4 (needs 12 bytes of values, but only 4 present)
            0x00, 0x00, 0x00, 0x01,
        ];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationBulkReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void BulkReportCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationBulkReportCommand command =
            ConfigurationCommandClass.ConfigurationBulkReportCommand.Create(
                parameterOffset: 5,
                reportsToFollow: 1,
                isDefault: false,
                isHandshake: false,
                size: 1,
                values: [10, 20]);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, span[0]); // Offset MSB
        Assert.AreEqual(0x05, span[1]); // Offset LSB
        Assert.AreEqual(0x02, span[2]); // Number of params
        Assert.AreEqual(0x01, span[3]); // Reports to follow
        Assert.AreEqual(0x01, span[4]); // Size=1
        Assert.AreEqual(10, span[5]);
        Assert.AreEqual(20, span[6]);
    }

    [TestMethod]
    public void BulkReportCommand_RoundTrip()
    {
        ConfigurationCommandClass.ConfigurationBulkReportCommand command =
            ConfigurationCommandClass.ConfigurationBulkReportCommand.Create(
                parameterOffset: 100,
                reportsToFollow: 0,
                isDefault: true,
                isHandshake: false,
                size: 2,
                values: [-1, 32767]);

        (ConfigurationBulkReport report, byte reportsToFollow) =
            ConfigurationCommandClass.ConfigurationBulkReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((ushort)100, report.ParameterOffset);
        Assert.IsTrue(report.IsDefault);
        Assert.IsFalse(report.IsHandshake);
        Assert.AreEqual((byte)2, report.Size);
        Assert.HasCount(2, report.Values);
        Assert.AreEqual(-1, report.Values[0]);
        Assert.AreEqual(32767, report.Values[1]);
        Assert.AreEqual((byte)0, reportsToFollow);
    }
}
