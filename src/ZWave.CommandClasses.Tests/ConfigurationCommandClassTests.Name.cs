using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ConfigurationCommandClassTests
{
    [TestMethod]
    public void NameGetCommand_Create_HasCorrectFormat()
    {
        ConfigurationCommandClass.ConfigurationNameGetCommand command =
            ConfigurationCommandClass.ConfigurationNameGetCommand.Create(256);

        Assert.AreEqual(CommandClassId.Configuration, ConfigurationCommandClass.ConfigurationNameGetCommand.CommandClassId);
        Assert.AreEqual((byte)ConfigurationCommand.NameGet, ConfigurationCommandClass.ConfigurationNameGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]); // MSB
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[1]); // LSB
    }

    [TestMethod]
    public void NameReportCommand_ParseInto_SingleReport()
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes("Dimming Rate");
        byte[] data = new byte[2 + 3 + nameBytes.Length]; // CC + Cmd + 2(param#) + 1(reports) + name
        data[0] = 0x70;
        data[1] = 0x0B;
        data[2] = 0x00; // param# MSB
        data[3] = 0x01; // param# LSB = 1
        data[4] = 0x00; // reports to follow = 0
        Array.Copy(nameBytes, 0, data, 5, nameBytes.Length);
        CommandClassFrame frame = new(data);

        List<byte> result = [];
        byte reportsToFollow = ConfigurationCommandClass.ConfigurationNameReportCommand.ParseInto(frame, result, NullLogger.Instance);

        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.AreEqual("Dimming Rate", Encoding.UTF8.GetString(result.ToArray()));
    }

    [TestMethod]
    public void NameReportCommand_ParseInto_MultipleReports()
    {
        // First report: "Hello " with 1 report to follow
        byte[] part1 = Encoding.UTF8.GetBytes("Hello ");
        byte[] data1 = new byte[2 + 3 + part1.Length];
        data1[0] = 0x70;
        data1[1] = 0x0B;
        data1[2] = 0x00;
        data1[3] = 0x01;
        data1[4] = 0x01; // 1 report to follow
        Array.Copy(part1, 0, data1, 5, part1.Length);

        // Second report: "World" with 0 reports to follow
        byte[] part2 = Encoding.UTF8.GetBytes("World");
        byte[] data2 = new byte[2 + 3 + part2.Length];
        data2[0] = 0x70;
        data2[1] = 0x0B;
        data2[2] = 0x00;
        data2[3] = 0x01;
        data2[4] = 0x00; // last report
        Array.Copy(part2, 0, data2, 5, part2.Length);

        List<byte> result = [];

        byte reportsToFollow1 = ConfigurationCommandClass.ConfigurationNameReportCommand.ParseInto(
            new CommandClassFrame(data1), result, NullLogger.Instance);
        Assert.AreEqual((byte)1, reportsToFollow1);

        byte reportsToFollow2 = ConfigurationCommandClass.ConfigurationNameReportCommand.ParseInto(
            new CommandClassFrame(data2), result, NullLogger.Instance);
        Assert.AreEqual((byte)0, reportsToFollow2);

        Assert.AreEqual("Hello World", Encoding.UTF8.GetString(result.ToArray()));
    }

    [TestMethod]
    public void NameReportCommand_ParseInto_EmptyName()
    {
        byte[] data = [0x70, 0x0B, 0x00, 0x01, 0x00]; // CC + Cmd + param# + reports=0, no name bytes
        CommandClassFrame frame = new(data);

        List<byte> result = [];
        byte reportsToFollow = ConfigurationCommandClass.ConfigurationNameReportCommand.ParseInto(frame, result, NullLogger.Instance);

        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void NameReportCommand_ParseInto_TooShort_Throws()
    {
        byte[] data = [0x70, 0x0B, 0x00]; // Only param# MSB, missing LSB and reports
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ConfigurationCommandClass.ConfigurationNameReportCommand.ParseInto(frame, [], NullLogger.Instance));
    }

    [TestMethod]
    public void NameReportCommand_Create_HasCorrectFormat()
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes("Test");
        ConfigurationCommandClass.ConfigurationNameReportCommand command =
            ConfigurationCommandClass.ConfigurationNameReportCommand.Create(42, 0, nameBytes);

        ReadOnlySpan<byte> span = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, span[0]); // param# MSB
        Assert.AreEqual(0x2A, span[1]); // param# LSB = 42
        Assert.AreEqual(0x00, span[2]); // reports to follow
        Assert.AreEqual((byte)'T', span[3]);
        Assert.AreEqual((byte)'e', span[4]);
        Assert.AreEqual((byte)'s', span[5]);
        Assert.AreEqual((byte)'t', span[6]);
    }

    [TestMethod]
    public void NameReportCommand_RoundTrip()
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes("Sensitivity");
        ConfigurationCommandClass.ConfigurationNameReportCommand command =
            ConfigurationCommandClass.ConfigurationNameReportCommand.Create(100, 0, nameBytes);

        List<byte> result = [];
        byte reportsToFollow = ConfigurationCommandClass.ConfigurationNameReportCommand.ParseInto(
            command.Frame, result, NullLogger.Instance);

        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.AreEqual("Sensitivity", Encoding.UTF8.GetString(result.ToArray()));
    }
}
