using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class NodeNamingAndLocationCommandClassTests
{
    [TestMethod]
    public void NodeNameSetCommand_Create_AsciiString()
    {
        var command = NodeNamingAndLocationCommandClass.NodeNameSetCommand.Create("Test");

        Assert.AreEqual(CommandClassId.NodeNamingAndLocation, NodeNamingAndLocationCommandClass.NodeNameSetCommand.CommandClassId);
        Assert.AreEqual((byte)NodeNamingAndLocationCommand.NodeNameSet, NodeNamingAndLocationCommandClass.NodeNameSetCommand.CommandId);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(5, parameters.Length); // 1 byte char presentation + 4 text bytes
        Assert.AreEqual((byte)CharPresentation.Ascii, parameters[0]);
        Assert.AreEqual((byte)'T', parameters[1]);
        Assert.AreEqual((byte)'e', parameters[2]);
        Assert.AreEqual((byte)'s', parameters[3]);
        Assert.AreEqual((byte)'t', parameters[4]);
    }

    [TestMethod]
    public void NodeNameSetCommand_Create_NonAsciiString_UsesUtf16()
    {
        var command = NodeNamingAndLocationCommandClass.NodeNameSetCommand.Create("Caf\u00E9");

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(9, parameters.Length); // 1 byte char presentation + 8 text bytes (4 UTF-16 chars)
        Assert.AreEqual((byte)CharPresentation.Utf16, parameters[0]);
        // "Café" in UTF-16 BE
        Assert.AreEqual(0x00, parameters[1]);
        Assert.AreEqual(0x43, parameters[2]);
    }

    [TestMethod]
    public void NodeNameSetCommand_Create_EmptyName()
    {
        var command = NodeNamingAndLocationCommandClass.NodeNameSetCommand.Create(string.Empty);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(1, parameters.Length); // Only char presentation byte
        Assert.AreEqual((byte)CharPresentation.Ascii, parameters[0]);
    }

    [TestMethod]
    public void NodeNameSetCommand_Create_TooLongName_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => NodeNamingAndLocationCommandClass.NodeNameSetCommand.Create(
                "This name is way too long for the limit"));
    }

    [TestMethod]
    public void NodeNameSetCommand_Create_ReservedBitsAreZero()
    {
        // Use a non-ASCII string to force UTF-16 (charPresentation = 0x02)
        var command = NodeNamingAndLocationCommandClass.NodeNameSetCommand.Create("\u00C0");

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x02, parameters[0]); // Only CharPresentation bits set
        Assert.AreEqual(0, parameters[0] & 0b1111_1000); // Reserved bits must be zero
    }

    [TestMethod]
    public void NodeNameGetCommand_Create_HasCorrectFormat()
    {
        NodeNamingAndLocationCommandClass.NodeNameGetCommand command =
            NodeNamingAndLocationCommandClass.NodeNameGetCommand.Create();

        Assert.AreEqual(CommandClassId.NodeNamingAndLocation, NodeNamingAndLocationCommandClass.NodeNameGetCommand.CommandClassId);
        Assert.AreEqual((byte)NodeNamingAndLocationCommand.NodeNameGet, NodeNamingAndLocationCommandClass.NodeNameGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void NodeNameReport_Parse_AsciiName()
    {
        // CC=0x77, Cmd=0x03, CharPres=0x00 (ASCII), "Test"
        byte[] data = [0x77, 0x03, 0x00, (byte)'T', (byte)'e', (byte)'s', (byte)'t'];
        CommandClassFrame frame = new(data);

        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("Test", name);
    }

    [TestMethod]
    public void NodeNameReport_Parse_Utf16Name()
    {
        // CC=0x77, Cmd=0x03, CharPres=0x02 (UTF-16), "Hi" in UTF-16 BE
        byte[] data = [0x77, 0x03, 0x02, 0x00, 0x48, 0x00, 0x69];
        CommandClassFrame frame = new(data);

        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("Hi", name);
    }

    [TestMethod]
    public void NodeNameReport_Parse_OemExtendedAscii()
    {
        // CC=0x77, Cmd=0x03, CharPres=0x01 (OEM), decoded as ASCII
        byte[] data = [0x77, 0x03, 0x01, (byte)'c', (byte)'a', (byte)'f', (byte)'e'];
        CommandClassFrame frame = new(data);

        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("cafe", name);
    }

    [TestMethod]
    public void NodeNameReport_Parse_EmptyName()
    {
        // CC=0x77, Cmd=0x03, CharPres=0x00 (ASCII), no text bytes
        byte[] data = [0x77, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(string.Empty, name);
    }

    [TestMethod]
    public void NodeNameReport_Parse_ReservedBitsIgnored()
    {
        // CharPres byte = 0xF8 | 0x00 = 0xF8. Reserved bits set, charPresentation = 0x00 (ASCII)
        byte[] data = [0x77, 0x03, 0xF8, (byte)'A'];
        CommandClassFrame frame = new(data);

        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual("A", name);
    }

    [TestMethod]
    public void NodeNameReport_Parse_TooShort_Throws()
    {
        // CC=0x77, Cmd=0x03, no parameters
        byte[] data = [0x77, 0x03];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void NodeNameSetCommand_RoundTrips_AsciiThroughReportParse()
    {
        var setCommand = NodeNamingAndLocationCommandClass.NodeNameSetCommand.Create("MyNode");

        // Build a report frame from the same parameter bytes
        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x77; // CC
        reportData[1] = 0x03; // Report command
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual("MyNode", name);
    }

    [TestMethod]
    public void NodeNameSetCommand_RoundTrips_Utf16ThroughReportParse()
    {
        var setCommand = NodeNamingAndLocationCommandClass.NodeNameSetCommand.Create("Caf\u00E9");

        byte[] reportData = new byte[2 + setCommand.Frame.CommandParameters.Length];
        reportData[0] = 0x77;
        reportData[1] = 0x03;
        setCommand.Frame.CommandParameters.Span.CopyTo(reportData.AsSpan(2));
        CommandClassFrame reportFrame = new(reportData);

        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(reportFrame, NullLogger.Instance);

        Assert.AreEqual("Caf\u00E9", name);
    }

    [TestMethod]
    public void NodeNameReport_Parse_Max16ByteAsciiName()
    {
        // CC=0x77, Cmd=0x03, CharPres=0x00 (ASCII), 16 bytes of text
        byte[] data = new byte[2 + 1 + 16];
        data[0] = 0x77;
        data[1] = 0x03;
        data[2] = 0x00; // ASCII
        for (int i = 0; i < 16; i++)
        {
            data[3 + i] = (byte)('A' + (i % 26));
        }

        CommandClassFrame frame = new(data);
        string name = NodeNamingAndLocationCommandClass.NodeNameReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(16, name.Length);
    }
}
