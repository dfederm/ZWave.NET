using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ConfigurationCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_Size1()
    {
        ConfigurationCommandClass.ConfigurationSetCommand command =
            ConfigurationCommandClass.ConfigurationSetCommand.Create(1, 1, 42);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationSetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.Set, ConfigurationCommandClass.ConfigurationSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.CommandParameters.Length); // param# + flags + 1 byte value
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]); // param#
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[1]); // size=1
        Assert.AreEqual(42, command.Frame.CommandParameters.Span[2]); // value
    }

    [TestMethod]
    public void SetCommand_Create_Size2()
    {
        ConfigurationCommandClass.ConfigurationSetCommand command =
            ConfigurationCommandClass.ConfigurationSetCommand.Create(5, 2, 0x1234);

        Assert.AreEqual(4, command.Frame.CommandParameters.Length); // param# + flags + 2 byte value
        Assert.AreEqual(0x05, command.Frame.CommandParameters.Span[0]); // param#
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[1]); // size=2
        Assert.AreEqual(0x12, command.Frame.CommandParameters.Span[2]); // MSB
        Assert.AreEqual(0x34, command.Frame.CommandParameters.Span[3]); // LSB
    }

    [TestMethod]
    public void SetCommand_Create_Size4()
    {
        ConfigurationCommandClass.ConfigurationSetCommand command =
            ConfigurationCommandClass.ConfigurationSetCommand.Create(10, 4, 0x12345678);

        Assert.AreEqual(6, command.Frame.CommandParameters.Length); // param# + flags + 4 byte value
        Assert.AreEqual(0x0A, command.Frame.CommandParameters.Span[0]); // param#
        Assert.AreEqual(0x04, command.Frame.CommandParameters.Span[1]); // size=4
        Assert.AreEqual(0x12, command.Frame.CommandParameters.Span[2]);
        Assert.AreEqual(0x34, command.Frame.CommandParameters.Span[3]);
        Assert.AreEqual(0x56, command.Frame.CommandParameters.Span[4]);
        Assert.AreEqual(0x78, command.Frame.CommandParameters.Span[5]);
    }

    [TestMethod]
    public void SetCommand_CreateDefault_HasDefaultBitSet()
    {
        ConfigurationCommandClass.ConfigurationSetCommand command =
            ConfigurationCommandClass.ConfigurationSetCommand.CreateDefault(7);

        Assert.AreEqual(0x07, command.Frame.CommandParameters.Span[0]); // param#
        Assert.AreNotEqual((byte)0, (byte)(command.Frame.CommandParameters.Span[1] & 0b1000_0000)); // Default bit
    }

    [TestMethod]
    public void SetCommand_Create_NegativeValue_Size1()
    {
        ConfigurationCommandClass.ConfigurationSetCommand command =
            ConfigurationCommandClass.ConfigurationSetCommand.Create(1, 1, -1);

        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[2]); // -1 as unsigned byte
    }

    [TestMethod]
    public void SetCommand_Create_NegativeValue_Size2()
    {
        ConfigurationCommandClass.ConfigurationSetCommand command =
            ConfigurationCommandClass.ConfigurationSetCommand.Create(1, 2, -256);

        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[2]); // -256 = 0xFF00
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[3]);
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationGetCommand command =
            ConfigurationCommandClass.ConfigurationGetCommand.Create(42);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationGetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.Get, ConfigurationCommandClass.ConfigurationGetCommand.CommandId);
        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(42, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size1_PositiveValue()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0x2A]; // CC, Cmd, param#=1, size=1, value=42
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)1, report.ParameterNumber);
        Assert.AreEqual((byte)1, report.Size);
        Assert.AreEqual(42L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size1_NegativeValue()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0xFF]; // value = -1
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(-1L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size2()
    {
        byte[] data = [0x70, 0x06, 0x02, 0x02, 0x01, 0x00]; // param#=2, size=2, value=256
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)2, report.ParameterNumber);
        Assert.AreEqual((byte)2, report.Size);
        Assert.AreEqual(256L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size2_NegativeValue()
    {
        byte[] data = [0x70, 0x06, 0x02, 0x02, 0xFF, 0x00]; // value = -256
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(-256L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size4()
    {
        byte[] data = [0x70, 0x06, 0x03, 0x04, 0x00, 0x01, 0x00, 0x00]; // param#=3, size=4, value=65536
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)3, report.ParameterNumber);
        Assert.AreEqual((byte)4, report.Size);
        Assert.AreEqual(65536L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size4_NegativeValue()
    {
        byte[] data = [0x70, 0x06, 0x03, 0x04, 0xFF, 0xFF, 0xFF, 0xFE]; // value = -2
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(-2L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x70, 0x06]; // CC + Cmd only, no parameters
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ReportCommand_Parse_TooShortForDeclaredSize_Throws()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x04, 0x00]; // size=4 but only 1 value byte
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ReportCommand_Parse_ReservedBitsIgnored()
    {
        // Upper bits of flags byte are reserved but should be ignored, size=1
        byte[] data = [0x70, 0x06, 0x01, 0xF9, 0x2A]; // 0xF9 = reserved bits + size=1
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.Size); // Only lower 3 bits matter
        Assert.AreEqual(42L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_NullFormat_InterpretedAsSigned()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0x2A]; // size=1, value=42
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNull(report.Format);
        Assert.AreEqual(42L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_SignedFormat_InterpretedAsSigned()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0x2A];
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(
            frame, NullLogger.Instance, ConfigurationParameterFormat.SignedInteger);

        Assert.AreEqual(ConfigurationParameterFormat.SignedInteger, report.Format);
        Assert.AreEqual(42L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_UnsignedFormat_InterpretedAsUnsigned()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0x2A];
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(
            frame, NullLogger.Instance, ConfigurationParameterFormat.UnsignedInteger);

        Assert.AreEqual(ConfigurationParameterFormat.UnsignedInteger, report.Format);
        Assert.AreEqual(42L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size1_0xFF_SignedVsUnsigned()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0xFF];
        CommandClassFrame frame = new(data);

        // Without format (null) → signed interpretation
        ConfigurationReport signed = ConfigurationCommandClass.ConfigurationReportCommand.Parse(frame, NullLogger.Instance);
        Assert.AreEqual(-1L, signed.Value);

        // With unsigned format → unsigned interpretation
        ConfigurationReport unsigned = ConfigurationCommandClass.ConfigurationReportCommand.Parse(
            frame, NullLogger.Instance, ConfigurationParameterFormat.UnsignedInteger);
        Assert.AreEqual(255L, unsigned.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_Size2_0xFFFF_UnsignedFormat()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x02, 0xFF, 0xFF]; // param#=1, size=2, value=0xFFFF
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(
            frame, NullLogger.Instance, ConfigurationParameterFormat.UnsignedInteger);

        Assert.AreEqual(65535L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_EnumeratedFormat_InterpretedAsUnsigned()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0x03];
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(
            frame, NullLogger.Instance, ConfigurationParameterFormat.Enumerated);

        Assert.AreEqual(ConfigurationParameterFormat.Enumerated, report.Format);
        Assert.AreEqual(3L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_BitFieldFormat_InterpretedAsUnsigned()
    {
        byte[] data = [0x70, 0x06, 0x01, 0x01, 0xA5];
        CommandClassFrame frame = new(data);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(
            frame, NullLogger.Instance, ConfigurationParameterFormat.BitField);

        Assert.AreEqual(ConfigurationParameterFormat.BitField, report.Format);
        Assert.AreEqual(0xA5L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_Create_Size1()
    {
        ConfigurationCommandClass.ConfigurationReportCommand command =
            ConfigurationCommandClass.ConfigurationReportCommand.Create(1, 1, 42);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationReportCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.Report, ConfigurationCommandClass.ConfigurationReportCommand.CommandId);
        Assert.AreEqual(3, command.Frame.CommandParameters.Length);
    }

    [TestMethod]
    public void ReportCommand_RoundTrip_Size1()
    {
        ConfigurationCommandClass.ConfigurationReportCommand command =
            ConfigurationCommandClass.ConfigurationReportCommand.Create(5, 1, -100);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((ushort)5, report.ParameterNumber);
        Assert.AreEqual((byte)1, report.Size);
        Assert.AreEqual(-100L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_RoundTrip_Size2()
    {
        ConfigurationCommandClass.ConfigurationReportCommand command =
            ConfigurationCommandClass.ConfigurationReportCommand.Create(10, 2, 12345);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((ushort)10, report.ParameterNumber);
        Assert.AreEqual((byte)2, report.Size);
        Assert.AreEqual(12345L, report.Value);
    }

    [TestMethod]
    public void ReportCommand_RoundTrip_Size4()
    {
        ConfigurationCommandClass.ConfigurationReportCommand command =
            ConfigurationCommandClass.ConfigurationReportCommand.Create(255, 4, -100000);

        ConfigurationReport report = ConfigurationCommandClass.ConfigurationReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((ushort)255, report.ParameterNumber);
        Assert.AreEqual((byte)4, report.Size);
        Assert.AreEqual(-100000L, report.Value);
    }
}
