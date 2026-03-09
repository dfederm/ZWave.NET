using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ConfigurationCommandClassTests
{
    [TestMethod]
    public void InfoGetCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationInfoGetCommand command =
            ConfigurationCommandClass.ConfigurationInfoGetCommand.Create(1);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationInfoGetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.InfoGet, ConfigurationCommandClass.ConfigurationInfoGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void InfoReportCommand_ParseInto_SingleReport()
    {
        byte[] infoBytes = Encoding.UTF8.GetBytes("Controls dimming speed");
        byte[] data = new byte[2 + 3 + infoBytes.Length];
        data[0] = 0x70;
        data[1] = 0x0D;
        data[2] = 0x00;
        data[3] = 0x01;
        data[4] = 0x00; // reports to follow = 0
        Array.Copy(infoBytes, 0, data, 5, infoBytes.Length);
        CommandClassFrame frame = new(data);

        List<byte> result = [];
        byte reportsToFollow = ConfigurationCommandClass.ConfigurationInfoReportCommand.ParseInto(frame, result, NullLogger.Instance);

        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.AreEqual("Controls dimming speed", Encoding.UTF8.GetString(result.ToArray()));
    }

    [TestMethod]
    public void InfoReportCommand_ParseInto_EmptyInfo()
    {
        byte[] data = [0x70, 0x0D, 0x00, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        List<byte> result = [];
        byte reportsToFollow = ConfigurationCommandClass.ConfigurationInfoReportCommand.ParseInto(frame, result, NullLogger.Instance);

        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void InfoReportCommand_ParseInto_TooShort_Throws()
    {
        byte[] data = [0x70, 0x0D, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationInfoReportCommand.ParseInto(frame, [], NullLogger.Instance));
    }

    [TestMethod]
    public void InfoReportCommand_Create_HasCorrectFormat()
    {
        byte[] infoBytes = Encoding.UTF8.GetBytes("Info");
        ConfigurationCommandClass.ConfigurationInfoReportCommand command =
            ConfigurationCommandClass.ConfigurationInfoReportCommand.Create(5, 0, infoBytes);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x05, span[1]);
        Assert.AreEqual(0x00, span[2]);
        Assert.AreEqual((byte)'I', span[3]);
    }

    [TestMethod]
    public void InfoReportCommand_RoundTrip()
    {
        byte[] infoBytes = Encoding.UTF8.GetBytes("Threshold value for sensor trigger");
        ConfigurationCommandClass.ConfigurationInfoReportCommand command =
            ConfigurationCommandClass.ConfigurationInfoReportCommand.Create(200, 0, infoBytes);

        List<byte> result = [];
        byte reportsToFollow = ConfigurationCommandClass.ConfigurationInfoReportCommand.ParseInto(
            command.Frame, result, NullLogger.Instance);

        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.AreEqual("Threshold value for sensor trigger", Encoding.UTF8.GetString(result.ToArray()));
    }
}
