using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class CommandDataParsingHelpersTests
{
    [TestMethod]
    public void ParseCommandClasses_EmptyInput_ReturnsEmpty()
    {
        IReadOnlyList<CommandClassInfo> result = CommandDataParsingHelpers.ParseCommandClasses([]);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ParseCommandClasses_SupportedOnly()
    {
        // Two supported CCs, no mark
        byte[] data =
        [
            (byte)CommandClassId.Basic,
            (byte)CommandClassId.BinarySwitch
        ];

        IReadOnlyList<CommandClassInfo> result = CommandDataParsingHelpers.ParseCommandClasses(data);

        Assert.HasCount(2, result);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Basic, IsSupported: true, IsControlled: false), result[0]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.BinarySwitch, IsSupported: true, IsControlled: false), result[1]);
    }

    [TestMethod]
    public void ParseCommandClasses_SupportedAndControlled()
    {
        // Supported CCs, then the mark, then controlled CCs
        byte[] data =
        [
            (byte)CommandClassId.Basic,
            (byte)CommandClassId.SupportControlMark,
            (byte)CommandClassId.BinarySwitch,
        ];

        IReadOnlyList<CommandClassInfo> result = CommandDataParsingHelpers.ParseCommandClasses(data);

        Assert.HasCount(2, result);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Basic, IsSupported: true, IsControlled: false), result[0]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.BinarySwitch, IsSupported: false, IsControlled: true), result[1]);
    }

    [TestMethod]
    public void ParseCommandClasses_ControlledOnly()
    {
        // Mark immediately, then controlled CCs
        byte[] data =
        [
            (byte)CommandClassId.SupportControlMark,
            (byte)CommandClassId.MultilevelSensor,
        ];

        IReadOnlyList<CommandClassInfo> result = CommandDataParsingHelpers.ParseCommandClasses(data);

        Assert.HasCount(1, result);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.MultilevelSensor, IsSupported: false, IsControlled: true), result[0]);
    }

    [TestMethod]
    public void ParseCommandClasses_MarkNotIncludedInResult()
    {
        byte[] data =
        [
            (byte)CommandClassId.Basic,
            (byte)CommandClassId.SupportControlMark,
        ];

        IReadOnlyList<CommandClassInfo> result = CommandDataParsingHelpers.ParseCommandClasses(data);

        Assert.HasCount(1, result);
        Assert.IsFalse(result.Any(cc => cc.CommandClass == CommandClassId.SupportControlMark));
    }

    [TestMethod]
    public void ParseNodeBitmask_EmptyBitmask_ReturnsEmpty()
    {
        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask([], baseNodeId: 1);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ParseNodeBitmask_AllZeros_ReturnsEmpty()
    {
        byte[] bitMask = [0x00, 0x00, 0x00];

        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ParseNodeBitmask_ClassicNodes_BaseNodeId1()
    {
        // 0x05 = 0b00000101 → bits 0 and 2 set → nodes 1 and 3
        byte[] bitMask = [0x05];

        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);

        CollectionAssert.AreEquivalent(new ushort[] { 1, 3 }, result.ToList());
    }

    [TestMethod]
    public void ParseNodeBitmask_ClassicNodes_MultipleBytes()
    {
        // Byte 0: 0x01 = bit 0 → node 1
        // Byte 1: 0x80 = bit 7 → node 1 + 1*8 + 7 = 16
        // Byte 2: 0x03 = bits 0,1 → nodes 1 + 2*8 + 0 = 17, 1 + 2*8 + 1 = 18
        byte[] bitMask = [0x01, 0x80, 0x03];

        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);

        CollectionAssert.AreEquivalent(new ushort[] { 1, 16, 17, 18 }, result.ToList());
    }

    [TestMethod]
    public void ParseNodeBitmask_AllBitsSet_ReturnsAllNodes()
    {
        byte[] bitMask = [0xFF, 0xFF];

        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);

        Assert.HasCount(16, result);
        for (ushort i = 1; i <= 16; i++)
        {
            Assert.Contains(i, result);
        }
    }

    [TestMethod]
    public void ParseNodeBitmask_LongRangeNodes_BaseNodeId256()
    {
        // LR nodes start at 256 (BITMASK_OFFSET = 0)
        // 0x01 = bit 0 → node 256
        // 0x04 = bit 2 → node 256 + 1*8 + 2 = 266
        byte[] bitMask = [0x01, 0x04];

        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 256);

        CollectionAssert.AreEquivalent(new ushort[] { 256, 266 }, result.ToList());
    }

    [TestMethod]
    public void ParseNodeBitmask_LongRangeNodes_NonZeroBitmaskOffset()
    {
        // BITMASK_OFFSET = 128 → baseNodeId = 256 + 128*8 = 1280
        // 0x02 = bit 1 → node 1280 + 0*8 + 1 = 1281
        byte[] bitMask = [0x02];

        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1280);

        CollectionAssert.AreEquivalent(new ushort[] { 1281 }, result.ToList());
    }

    [TestMethod]
    public void ParseNodeBitmask_MatchesGetInitDataExpectedOutput()
    {
        // This is the same bitmask and expected output from GetInitDataTests.Response
        byte[] bitMask =
        [
            0x07, 0x5a, 0xae, 0xf9,
            0x7b, 0x1a, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00
        ];

        HashSet<ushort> result = CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);

        HashSet<ushort> expected = [1, 2, 3, 10, 12, 13, 15, 18, 19, 20, 22, 24, 25, 28, 29, 30, 31, 32, 33, 34, 36, 37, 38, 39, 42, 44, 45];
        Assert.IsTrue(result.SetEquals(expected), $"Expected: {string.Join(", ", expected.Order())}\nActual: {string.Join(", ", result.Order())}");
    }
}
