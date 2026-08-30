using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiCommandCommandClassTests
{
    [TestMethod]
    public void CreateEncapsulation_SingleCommand_HasCorrectFormat()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x02);
        CommandClassFrame frame = MultiCommandCommandClass.CreateEncapsulation([innerFrame]);

        Assert.AreEqual(CommandClassId.MultiCommand, frame.CommandClassId);
        Assert.AreEqual((byte)MultiCommandCommand.CommandEncapsulation, frame.CommandId);

        // Parameters: [count=1][length=2][CC=0x20][cmd=0x02]
        ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
        Assert.AreEqual(4, parameters.Length);
        Assert.AreEqual((byte)0x01, parameters[0]);
        Assert.AreEqual((byte)0x02, parameters[1]);
        Assert.AreEqual((byte)0x20, parameters[2]);
        Assert.AreEqual((byte)0x02, parameters[3]);
    }

    [TestMethod]
    public void CreateEncapsulation_MultipleCommands_HasCorrectFormat()
    {
        CommandClassFrame first = CommandClassFrame.Create(CommandClassId.Basic, 0x01, [0xFF]);
        CommandClassFrame second = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x02);
        CommandClassFrame frame = MultiCommandCommandClass.CreateEncapsulation([first, second]);

        // Parameters: [count=2][length=3][0x20][0x01][0xFF][length=2][0x25][0x02]
        ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
        Assert.AreEqual(8, parameters.Length);
        Assert.AreEqual((byte)0x02, parameters[0]);
        Assert.AreEqual((byte)0x03, parameters[1]);
        Assert.AreEqual((byte)0x20, parameters[2]);
        Assert.AreEqual((byte)0x01, parameters[3]);
        Assert.AreEqual((byte)0xFF, parameters[4]);
        Assert.AreEqual((byte)0x02, parameters[5]);
        Assert.AreEqual((byte)0x25, parameters[6]);
        Assert.AreEqual((byte)0x02, parameters[7]);
    }

    [TestMethod]
    public void CreateEncapsulation_RejectsEmptyList()
    {
        Assert.Throws<ArgumentException>(
            () => MultiCommandCommandClass.CreateEncapsulation(Array.Empty<CommandClassFrame>()));
    }

    [TestMethod]
    public void ParseEncapsulation_SingleCommand()
    {
        // [0x8F][0x01][count=1][length=2][0x20][0x02]
        byte[] data = [0x8F, 0x01, 0x01, 0x02, 0x20, 0x02];

        MultiCommandEncapsulation parsed = MultiCommandCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance);

        Assert.HasCount(1, parsed.Commands);
        Assert.AreEqual(CommandClassId.Basic, parsed.Commands[0].CommandClassId);
        Assert.AreEqual((byte)0x02, parsed.Commands[0].CommandId);
    }

    [TestMethod]
    public void ParseEncapsulation_MultipleCommands_PreservesOrder()
    {
        // [0x8F][0x01][count=2][length=3][0x20][0x01][0xFF][length=2][0x25][0x02]
        byte[] data = [0x8F, 0x01, 0x02, 0x03, 0x20, 0x01, 0xFF, 0x02, 0x25, 0x02];

        MultiCommandEncapsulation parsed = MultiCommandCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance);

        Assert.HasCount(2, parsed.Commands);
        Assert.AreEqual(CommandClassId.Basic, parsed.Commands[0].CommandClassId);
        Assert.AreEqual((byte)0x01, parsed.Commands[0].CommandId);

        byte[] expectedParams = [0xFF];
        Assert.IsTrue(parsed.Commands[0].CommandParameters.Span.SequenceEqual(expectedParams));

        Assert.AreEqual(CommandClassId.BinarySwitch, parsed.Commands[1].CommandClassId);
        Assert.AreEqual((byte)0x02, parsed.Commands[1].CommandId);
    }

    [TestMethod]
    public void ParseEncapsulation_RoundTrip_PreservesCommands()
    {
        CommandClassFrame[] originals =
        [
            CommandClassFrame.Create(CommandClassId.Basic, 0x01, [0x00]),
            CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01, [0xFF]),
            CommandClassFrame.Create(CommandClassId.Meter, 0x02, [0x05, 0x00]),
        ];

        CommandClassFrame frame = MultiCommandCommandClass.CreateEncapsulation(originals);
        MultiCommandEncapsulation parsed = MultiCommandCommandClass.ParseEncapsulation(frame, NullLogger.Instance);

        Assert.HasCount(3, parsed.Commands);
        for (int i = 0; i < originals.Length; i++)
        {
            Assert.AreEqual(originals[i].CommandClassId, parsed.Commands[i].CommandClassId);
            Assert.AreEqual(originals[i].CommandId, parsed.Commands[i].CommandId);
            Assert.IsTrue(originals[i].Data.Span.SequenceEqual(parsed.Commands[i].Data.Span));
        }
    }

    [TestMethod]
    public void ParseEncapsulation_TooShort_Throws()
    {
        // CC + command only, no parameters (the count byte is missing).
        byte[] data = [0x8F, 0x01];

        Assert.Throws<ZWaveException>(
            () => MultiCommandCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance));
    }

    [TestMethod]
    public void ParseEncapsulation_Truncated_Throws()
    {
        // Declares 2 commands but only one is present.
        byte[] data = [0x8F, 0x01, 0x02, 0x02, 0x20, 0x02];

        Assert.Throws<ZWaveException>(
            () => MultiCommandCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance));
    }

    [TestMethod]
    public void ParseEncapsulation_ExtendedCommandClass_Throws()
    {
        // The inner command class is 16-bit (0xFF-prefixed) — unsupported.
        byte[] data = [0x8F, 0x01, 0x01, 0x03, 0xFF, 0x70, 0x01];

        Assert.Throws<ZWaveException>(
            () => MultiCommandCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance));
    }

    [TestMethod]
    public void ParseEncapsulation_TrailingBytes_Throws()
    {
        // Declares 1 command of length 2, but a trailing byte remains.
        byte[] data = [0x8F, 0x01, 0x01, 0x02, 0x20, 0x02, 0x00];

        Assert.Throws<ZWaveException>(
            () => MultiCommandCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance));
    }
}
