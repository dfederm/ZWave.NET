using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class NodeNamingAndLocationCommandClassTests
{
    [TestMethod]
    public void NodeLocationSetCommand_Create_AsciiString()
    {
        var command = NodeNamingAndLocationCommandClass.NodeLocationSetCommand.Create("Room");

        Assert.AreEqual(CommandClassId.NodeNamingAndLocation, NodeNamingAndLocationCommandClass.NodeLocationSetCommand.CommandClassId);
        Assert.AreEqual((byte)NodeNamingAndLocationCommand.NodeLocationSet, NodeNamingAndLocationCommandClass.NodeLocationSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(5, parameters.Length); // 1 byte char presentation + 4 text bytes
        Assert.AreEqual((byte)CharPresentation.Ascii, parameters[0]);
        Assert.AreEqual((byte)'R', parameters[1]);
        Assert.AreEqual((byte)'o', parameters[2]);
        Assert.AreEqual((byte)'o', parameters[3]);
        Assert.AreEqual((byte)'m', parameters[4]);
    }

    [TestMethod]
    public void NodeLocationSetCommand_Create_NonAsciiString_UsesUtf16()
    {
        var command = NodeNamingAndLocationCommandClass.NodeLocationSetCommand.Create("B\u00FCro");

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(9, parameters.Length); // 1 byte char presentation + 8 text bytes (4 UTF-16 chars)
        Assert.AreEqual((byte)CharPresentation.Utf16, parameters[0]);
    }

    [TestMethod]
    public void NodeLocationSetCommand_Create_EmptyLocation()
    {
        var command = NodeNamingAndLocationCommandClass.NodeLocationSetCommand.Create(string.Empty);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(1, parameters.Length); // Only char presentation byte
        Assert.AreEqual((byte)CharPresentation.Ascii, parameters[0]);
    }

    [TestMethod]
    public void NodeLocationSetCommand_Create_TooLongLocation_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => NodeNamingAndLocationCommandClass.NodeLocationSetCommand.Create(
                "This location name is way too long"));
    }

    [TestMethod]
    public void NodeLocationGetCommand_Create_HasCorrectFormat()
    {
        NodeNamingAndLocationCommandClass.NodeLocationGetCommand command =
            NodeNamingAndLocationCommandClass.NodeLocationGetCommand.Create();

        Assert.AreEqual(CommandClassId.NodeNamingAndLocation, NodeNamingAndLocationCommandClass.NodeLocationGetCommand.CommandClassId);
        Assert.AreEqual((byte)NodeNamingAndLocationCommand.NodeLocationGet, NodeNamingAndLocationCommandClass.NodeLocationGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void NodeLocationReport_Parse_AsciiLocation()
    {
        // CC=0x77, Cmd=0x06, CharPres=0x00 (ASCII), "Room"
        byte[] data = [0x77, 0x06, 0x00, (byte)'R', (byte)'o', (byte)'o', (byte)'m'];
        CommandClassFrame frame = new(data);

        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("Room", location);
    }

    [TestMethod]
    public void NodeLocationReport_Parse_Utf16Location()
    {
        // CC=0x77, Cmd=0x06, CharPres=0x02 (UTF-16), "Hi" in UTF-16 BE
        byte[] data = [0x77, 0x06, 0x02, 0x00, 0x48, 0x00, 0x69];
        CommandClassFrame frame = new(data);

        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("Hi", location);
    }

    [TestMethod]
    public void NodeLocationReport_Parse_OemExtendedAscii()
    {
        // CC=0x77, Cmd=0x06, CharPres=0x01 (OEM), decoded as ASCII
        byte[] data = [0x77, 0x06, 0x01, (byte)'R', (byte)'o', (byte)'o', (byte)'m'];
        CommandClassFrame frame = new(data);

        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("Room", location);
    }

    [TestMethod]
    public void NodeLocationReport_Parse_EmptyLocation()
    {
        // CC=0x77, Cmd=0x06, CharPres=0x00 (ASCII), no text bytes
        byte[] data = [0x77, 0x06, 0x00];
        CommandClassFrame frame = new(data);

        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(string.Empty, location);
    }

    [TestMethod]
    public void NodeLocationReport_Parse_ReservedBitsIgnored()
    {
        // CharPres byte = 0xF9. Reserved bits set, charPresentation = 0x01 (OEM Extended)
        byte[] data = [0x77, 0x06, 0xF9, (byte)'X'];
        CommandClassFrame frame = new(data);

        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("X", location);
    }

    [TestMethod]
    public void NodeLocationReport_Parse_TooShort_Throws()
    {
        // CC=0x77, Cmd=0x06, no parameters
        byte[] data = [0x77, 0x06];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void NodeLocationSetCommand_RoundTrips_AsciiThroughReportParse()
    {
        var setCommand = NodeNamingAndLocationCommandClass.NodeLocationSetCommand.Create("Kitchen");

        // Build a report frame from the same parameter bytes
        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x77; // CC
        reportData[1] = 0x06; // Report command
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual("Kitchen", location);
    }

    [TestMethod]
    public void NodeLocationSetCommand_RoundTrips_NonAsciiThroughReportParse()
    {
        var setCommand = NodeNamingAndLocationCommandClass.NodeLocationSetCommand.Create("B\u00FCro");

        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x77;
        reportData[1] = 0x06;
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual("B\u00FCro", location);
    }

    [TestMethod]
    public void NodeLocationReport_Parse_Max16ByteAsciiLocation()
    {
        // CC=0x77, Cmd=0x06, CharPres=0x00 (ASCII), 16 bytes of text
        byte[] data = new byte[2 + 1 + 16];
        data[0] = 0x77;
        data[1] = 0x06;
        data[2] = 0x00; // ASCII
        for (int i = 0; i < 16; i++)
        {
            data[3 + i] = (byte)('a' + (i % 26));
        }

        CommandClassFrame frame = new(data);
        string location = NodeNamingAndLocationCommandClass.NodeLocationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(16, location.Length);
    }
}
