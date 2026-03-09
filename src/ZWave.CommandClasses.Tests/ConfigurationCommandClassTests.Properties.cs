using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ConfigurationCommandClassTests
{
    [TestMethod]
    public void DefaultResetCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationDefaultResetCommand command =
            ConfigurationCommandClass.ConfigurationDefaultResetCommand.Create();

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationDefaultResetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.DefaultReset, ConfigurationCommandClass.ConfigurationDefaultResetCommand.CommandId);
        Assert.AreEqual(0, command.Frame.CommandParameters.Length); // No parameters
    }

    [TestMethod]
    public void PropertiesGetCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationPropertiesGetCommand command =
            ConfigurationCommandClass.ConfigurationPropertiesGetCommand.Create(0);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationPropertiesGetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.PropertiesGet, ConfigurationCommandClass.ConfigurationPropertiesGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void PropertiesGetCommand_Create_HighParameterNumber()
    {
        ConfigurationCommandClass.ConfigurationPropertiesGetCommand command =
            ConfigurationCommandClass.ConfigurationPropertiesGetCommand.Create(0x1234);

        Assert.AreEqual(0x12, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x34, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_V3_Size1_SignedInteger()
    {
        byte[] data =
        [
            0x70, 0x0F,       // CC + Cmd
            0x00, 0x01,       // Parameter Number = 1
            0b0000_0001,      // Format=SignedInteger(0), Size=1
            0x00,             // Min = 0
            0x64,             // Max = 100
            0x32,             // Default = 50
            0x00, 0x02,       // Next Parameter = 2
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)1, props.ParameterNumber);
        Assert.AreEqual(ConfigurationParameterFormat.SignedInteger, props.Format);
        Assert.AreEqual((byte)1, props.Size);
        Assert.AreEqual(0L, props.MinValue);
        Assert.AreEqual(100L, props.MaxValue);
        Assert.AreEqual(50L, props.DefaultValue);
        Assert.AreEqual((ushort)2, nextParameterNumber);
        Assert.IsNull(props.ReadOnly);
        Assert.IsNull(props.AlteringCapabilities);
        Assert.IsNull(props.Advanced);
        Assert.IsNull(props.NoBulkSupport);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_V3_Size2_UnsignedInteger()
    {
        byte[] data =
        [
            0x70, 0x0F,
            0x00, 0x05,       // Parameter Number = 5
            0b0000_1010,      // Format=UnsignedInteger(1), Size=2
            0x00, 0x00,       // Min = 0
            0xFF, 0xFF,       // Max = 65535 (unsigned)
            0x00, 0x0A,       // Default = 10
            0x00, 0x06,       // Next Parameter = 6
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)5, props.ParameterNumber);
        Assert.AreEqual(ConfigurationParameterFormat.UnsignedInteger, props.Format);
        Assert.AreEqual((byte)2, props.Size);
        Assert.AreEqual(0L, props.MinValue);
        Assert.AreEqual(65535L, props.MaxValue);
        Assert.AreEqual(10L, props.DefaultValue);
        Assert.AreEqual((ushort)6, nextParameterNumber);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_V3_Size4_Enumerated()
    {
        byte[] data =
        [
            0x70, 0x0F,
            0x00, 0x0A,       // Parameter Number = 10
            0b0001_0100,      // Format=Enumerated(2), Size=4
            0x00, 0x00, 0x00, 0x00, // Min = 0
            0x00, 0x00, 0x00, 0x03, // Max = 3
            0x00, 0x00, 0x00, 0x01, // Default = 1
            0x00, 0x00,       // Next Parameter = 0 (last)
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)10, props.ParameterNumber);
        Assert.AreEqual(ConfigurationParameterFormat.Enumerated, props.Format);
        Assert.AreEqual((byte)4, props.Size);
        Assert.AreEqual(0L, props.MinValue);
        Assert.AreEqual(3L, props.MaxValue);
        Assert.AreEqual(1L, props.DefaultValue);
        Assert.AreEqual((ushort)0, nextParameterNumber);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_V3_Size0_UnassignedParameter()
    {
        byte[] data =
        [
            0x70, 0x0F,
            0x00, 0x00,       // Parameter Number = 0
            0b0000_0000,      // Format=0, Size=0 (unassigned)
            0x00, 0x05,       // Next Parameter = 5
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)0, props.ParameterNumber);
        Assert.AreEqual((byte)0, props.Size);
        Assert.AreEqual(0L, props.MinValue);
        Assert.AreEqual(0L, props.MaxValue);
        Assert.AreEqual(0L, props.DefaultValue);
        Assert.AreEqual((ushort)5, nextParameterNumber);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_V4_WithFlags()
    {
        byte[] data =
        [
            0x70, 0x0F,
            0x00, 0x01,       // Parameter Number = 1
            0b1100_0001,      // AlteringCapabilities=1, ReadOnly=1, Format=SignedInteger(0), Size=1
            0x00,             // Min = 0
            0x64,             // Max = 100
            0x32,             // Default = 50
            0x00, 0x02,       // Next Parameter = 2
            0b0000_0011,      // NoBulkSupport=1, Advanced=1
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationParameterProperties props, ushort _) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)1, props.ParameterNumber);
        Assert.IsTrue(props.ReadOnly!.Value);
        Assert.IsTrue(props.AlteringCapabilities!.Value);
        Assert.IsTrue(props.Advanced!.Value);
        Assert.IsTrue(props.NoBulkSupport!.Value);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_V4_AllFlagsFalse()
    {
        byte[] data =
        [
            0x70, 0x0F,
            0x00, 0x01,       // Parameter Number = 1
            0b0000_0001,      // AlteringCapabilities=0, ReadOnly=0, Format=SignedInteger(0), Size=1
            0x00,             // Min = 0
            0x64,             // Max = 100
            0x32,             // Default = 50
            0x00, 0x02,       // Next Parameter = 2
            0b0000_0000,      // NoBulkSupport=0, Advanced=0
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationParameterProperties props, ushort _) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(props.ReadOnly!.Value);
        Assert.IsFalse(props.AlteringCapabilities!.Value);
        Assert.IsFalse(props.Advanced!.Value);
        Assert.IsFalse(props.NoBulkSupport!.Value);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_V4_Size0_WithFlags()
    {
        byte[] data =
        [
            0x70, 0x0F,
            0x00, 0x00,       // Parameter Number = 0
            0b0000_0000,      // Size=0, unassigned
            0x00, 0x01,       // Next Parameter = 1
            0b0000_0000,      // V4 flags: all false
        ];
        CommandClassFrame frame = new(data);

        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, props.Size);
        Assert.AreEqual((ushort)1, nextParameterNumber);
        Assert.IsFalse(props.ReadOnly!.Value);
        Assert.IsFalse(props.Advanced!.Value);
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x70, 0x0F, 0x00, 0x01]; // Only param#, missing format/size and next
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void PropertiesReportCommand_Parse_TooShortForDeclaredSize_Throws()
    {
        byte[] data =
        [
            0x70, 0x0F,
            0x00, 0x01,       // Parameter Number = 1
            0b0000_0100,      // Size=4, but not enough bytes follow
            0x00, 0x00,       // Only 2 bytes, need 3*4 + 2 = 14
        ];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void PropertiesReportCommand_Create_V3()
    {
        ConfigurationCommandClass.ConfigurationPropertiesReportCommand command =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Create(
                parameterNumber: 1,
                format: ConfigurationParameterFormat.SignedInteger,
                size: 1,
                minValue: 0,
                maxValue: 100,
                defaultValue: 50,
                nextParameterNumber: 2);

        // Verify we can parse what we created
        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((ushort)1, props.ParameterNumber);
        Assert.AreEqual(ConfigurationParameterFormat.SignedInteger, props.Format);
        Assert.AreEqual((byte)1, props.Size);
        Assert.AreEqual(0L, props.MinValue);
        Assert.AreEqual(100L, props.MaxValue);
        Assert.AreEqual(50L, props.DefaultValue);
        Assert.AreEqual((ushort)2, nextParameterNumber);
        Assert.IsNull(props.ReadOnly);
    }

    [TestMethod]
    public void PropertiesReportCommand_Create_V4()
    {
        ConfigurationCommandClass.ConfigurationPropertiesReportCommand command =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Create(
                parameterNumber: 3,
                format: ConfigurationParameterFormat.BitField,
                size: 2,
                minValue: 0,
                maxValue: 0x00FF,
                defaultValue: 0x000F,
                nextParameterNumber: 0,
                readOnly: true,
                alteringCapabilities: false,
                advanced: true,
                noBulkSupport: false);

        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((ushort)3, props.ParameterNumber);
        Assert.AreEqual(ConfigurationParameterFormat.BitField, props.Format);
        Assert.AreEqual((byte)2, props.Size);
        Assert.AreEqual(0L, props.MinValue);
        Assert.AreEqual(255L, props.MaxValue);
        Assert.AreEqual(15L, props.DefaultValue);
        Assert.AreEqual((ushort)0, nextParameterNumber);
        Assert.IsTrue(props.ReadOnly!.Value);
        Assert.IsFalse(props.AlteringCapabilities!.Value);
        Assert.IsTrue(props.Advanced!.Value);
        Assert.IsFalse(props.NoBulkSupport!.Value);
    }

    [TestMethod]
    public void PropertiesReportCommand_RoundTrip_Size0()
    {
        ConfigurationCommandClass.ConfigurationPropertiesReportCommand command =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Create(
                parameterNumber: 0,
                format: ConfigurationParameterFormat.SignedInteger,
                size: 0,
                minValue: 0,
                maxValue: 0,
                defaultValue: 0,
                nextParameterNumber: 5);

        (ConfigurationParameterProperties props, ushort nextParameterNumber) =
            ConfigurationCommandClass.ConfigurationPropertiesReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((ushort)0, props.ParameterNumber);
        Assert.AreEqual((byte)0, props.Size);
        Assert.AreEqual((ushort)5, nextParameterNumber);
    }
}
