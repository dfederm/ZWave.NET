using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class Crc16EncapsulationCommandClassTests
{
    [TestMethod]
    public void CreateEncapsulation_BasicGet_MatchesSpecTable1()
    {
        // Spec SDS13783 §3.1.2 Table 1: encapsulating a Basic Get yields
        // [0x56, 0x01, 0x20, 0x02, 0x4D, 0x26].
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x02);
        CommandClassFrame frame = Crc16EncapsulationCommandClass.CreateEncapsulation(innerFrame);

        Assert.AreEqual(CommandClassId.Crc16Encapsulation, frame.CommandClassId);
        Assert.AreEqual((byte)Crc16EncapsulationCommand.CommandEncapsulation, frame.CommandId);

        byte[] expected = [0x56, 0x01, 0x20, 0x02, 0x4D, 0x26];
        Assert.IsTrue(frame.Data.Span.SequenceEqual(expected));
    }

    [TestMethod]
    public void ParseEncapsulation_RoundTrip_PreservesInnerFrame()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01, [0x25, 0x06]);
        CommandClassFrame frame = Crc16EncapsulationCommandClass.CreateEncapsulation(innerFrame);

        Crc16Encapsulation? parsed = Crc16EncapsulationCommandClass.ParseEncapsulation(frame, NullLogger.Instance);

        Assert.IsNotNull(parsed);
        Assert.AreEqual(CommandClassId.BinarySwitch, parsed.Value.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x01, parsed.Value.EncapsulatedFrame.CommandId);

        byte[] expectedParams = [0x25, 0x06];
        Assert.IsTrue(parsed.Value.EncapsulatedFrame.CommandParameters.Span.SequenceEqual(expectedParams));
    }

    [TestMethod]
    public void ParseEncapsulation_ChecksumMismatch_ReturnsNull()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.Basic, 0x02);
        CommandClassFrame frame = Crc16EncapsulationCommandClass.CreateEncapsulation(innerFrame);

        // Corrupt the checksum LSB.
        byte[] data = frame.Data.ToArray();
        data[^1] ^= 0x01;

        Assert.IsNull(Crc16EncapsulationCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance));
    }

    [TestMethod]
    public void ParseEncapsulation_TooShort_ReturnsNull()
    {
        // 5 bytes: CC + Cmd + inner CC + inner Cmd + 1 checksum byte (6 required).
        byte[] data = [0x56, 0x01, 0x20, 0x02, 0x00];

        Assert.IsNull(Crc16EncapsulationCommandClass.ParseEncapsulation(new CommandClassFrame(data), NullLogger.Instance));
    }

    [TestMethod]
    public void ParseEncapsulation_ExtendedCommandClass_ReturnsNull()
    {
        // Inner command class is 16-bit (0xFF-prefixed) — unsupported, so parse returns null
        // even though the checksum is valid.
        CommandClassFrame innerFrame = new(new byte[] { 0xFF, 0x70, 0x01 });
        CommandClassFrame frame = Crc16EncapsulationCommandClass.CreateEncapsulation(innerFrame);

        Assert.IsNull(Crc16EncapsulationCommandClass.ParseEncapsulation(frame, NullLogger.Instance));
    }
}
