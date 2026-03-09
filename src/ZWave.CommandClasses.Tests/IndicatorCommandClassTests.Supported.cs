using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class IndicatorCommandClassTests
{
    #region Supported Get Command

    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        IndicatorCommandClass.IndicatorSupportedGetCommand command =
            IndicatorCommandClass.IndicatorSupportedGetCommand.Create(IndicatorId.NodeIdentify);

        Assert.AreEqual(CommandClassId.Indicator, IndicatorCommandClass.IndicatorSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)IndicatorCommand.SupportedGet, IndicatorCommandClass.IndicatorSupportedGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)IndicatorId.NodeIdentify, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SupportedGetCommand_Create_Discovery()
    {
        // Indicator ID 0x00 is used to discover the first supported indicator.
        IndicatorCommandClass.IndicatorSupportedGetCommand command =
            IndicatorCommandClass.IndicatorSupportedGetCommand.Create(0);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    #endregion

    #region Supported Report Parse

    [TestMethod]
    public void SupportedReport_Parse_WithProperties()
    {
        byte[] data =
        [
            0x87, 0x05,    // CC + Command
            0x50,          // Indicator ID = Node Identify
            0xF0,          // Next Indicator ID = Buzzer
            0x01,          // Bitmask length = 1 byte
            0b0001_1110,   // Bits 1-4 set = Multilevel, Binary, OnOffPeriod, OnOffCycles
        ];
        CommandClassFrame frame = new(data);

        (IndicatorId nextIndicatorId, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(IndicatorId.Buzzer, nextIndicatorId);
        Assert.HasCount(4, propertyIds);
        Assert.Contains(IndicatorPropertyId.Multilevel, propertyIds);
        Assert.Contains(IndicatorPropertyId.Binary, propertyIds);
        Assert.Contains(IndicatorPropertyId.OnOffPeriod, propertyIds);
        Assert.Contains(IndicatorPropertyId.OnOffCycles, propertyIds);
    }

    [TestMethod]
    public void SupportedReport_Parse_NoMoreIndicators()
    {
        byte[] data =
        [
            0x87, 0x05,
            0x01,       // Indicator ID = Armed
            0x00,       // Next Indicator ID = 0x00 (none)
            0x01,       // Bitmask length = 1
            0b0000_0110, // Bits 1,2 = Multilevel, Binary
        ];
        CommandClassFrame frame = new(data);

        (IndicatorId nextIndicatorId, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((IndicatorId)0x00, nextIndicatorId);
        Assert.HasCount(2, propertyIds);
        Assert.Contains(IndicatorPropertyId.Multilevel, propertyIds);
        Assert.Contains(IndicatorPropertyId.Binary, propertyIds);
    }

    [TestMethod]
    public void SupportedReport_Parse_UnsupportedIndicator()
    {
        // Per spec: if unsupported Indicator ID, all fields set to 0x00.
        byte[] data =
        [
            0x87, 0x05,
            0x00,       // Indicator ID = 0x00
            0x00,       // Next Indicator ID = 0x00
            0x00,       // Bitmask length = 0
        ];
        CommandClassFrame frame = new(data);

        (IndicatorId nextIndicatorId, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((IndicatorId)0x00, nextIndicatorId);
        Assert.IsEmpty(propertyIds);
    }

    [TestMethod]
    public void SupportedReport_Parse_MultiByteBitmask()
    {
        // Bitmask spanning 2 bytes to include property IDs > 7
        byte[] data =
        [
            0x87, 0x05,
            0x50,           // Indicator ID = Node Identify
            0x00,           // Next Indicator ID = 0x00
            0x02,           // Bitmask length = 2 bytes
            0b0001_1110,    // Byte 0: bits 1-4 = Multilevel(1), Binary(2), OnOffPeriod(3), OnOffCycles(4)
            0b0000_0010,    // Byte 1: bit 1 (= property 9) = SoundLevel
        ];
        CommandClassFrame frame = new(data);

        (_, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(5, propertyIds);
        Assert.Contains(IndicatorPropertyId.Multilevel, propertyIds);
        Assert.Contains(IndicatorPropertyId.Binary, propertyIds);
        Assert.Contains(IndicatorPropertyId.OnOffPeriod, propertyIds);
        Assert.Contains(IndicatorPropertyId.OnOffCycles, propertyIds);
        Assert.Contains(IndicatorPropertyId.SoundLevel, propertyIds);
    }

    [TestMethod]
    public void SupportedReport_Parse_ReservedBit0Ignored()
    {
        // Bit 0 in the first bitmask byte is reserved and MUST be set to zero per spec.
        // Even if set, it should not produce a property ID.
        byte[] data =
        [
            0x87, 0x05,
            0x50,           // Indicator ID = Node Identify
            0x00,           // Next Indicator ID = 0x00
            0x01,           // Bitmask length = 1
            0b0000_0111,    // Bit 0 (reserved) + bits 1,2 = Multilevel, Binary
        ];
        CommandClassFrame frame = new(data);

        (_, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(frame, NullLogger.Instance);

        // Bit 0 is skipped due to startBit: 1 in ParseBitMask
        Assert.HasCount(2, propertyIds);
        Assert.Contains(IndicatorPropertyId.Multilevel, propertyIds);
        Assert.Contains(IndicatorPropertyId.Binary, propertyIds);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x87, 0x05, 0x50]; // Only 1 parameter byte (need at least 3)
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    #endregion

    #region Supported Report Create (Bidirectional)

    [TestMethod]
    public void SupportedReportCommand_Create_HasCorrectFormat()
    {
        HashSet<IndicatorPropertyId> properties =
        [
            IndicatorPropertyId.Multilevel,
            IndicatorPropertyId.Binary,
        ];

        IndicatorCommandClass.IndicatorSupportedReportCommand command =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Create(
                IndicatorId.NodeIdentify,
                IndicatorId.Buzzer,
                properties);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual((byte)IndicatorId.NodeIdentify, parameters[0]);
        Assert.AreEqual((byte)IndicatorId.Buzzer, parameters[1]);
        int bitmaskLength = parameters[2] & 0b0001_1111;
        Assert.AreEqual(1, bitmaskLength);
        // Bits 1 and 2 should be set for Multilevel(1) and Binary(2)
        Assert.AreEqual(0b0000_0110, parameters[3]);
    }

    [TestMethod]
    public void SupportedReportCommand_Create_EmptyProperties()
    {
        HashSet<IndicatorPropertyId> properties = [];

        IndicatorCommandClass.IndicatorSupportedReportCommand command =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Create(
                (IndicatorId)0x00,
                (IndicatorId)0x00,
                properties);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(0x00, parameters[0]); // Indicator ID
        Assert.AreEqual(0x00, parameters[1]); // Next Indicator ID
        Assert.AreEqual(0x00, parameters[2] & 0b0001_1111); // Bitmask length = 0
    }

    #endregion

    #region Supported Report Round-trip

    [TestMethod]
    public void SupportedReport_RoundTrip()
    {
        HashSet<IndicatorPropertyId> properties =
        [
            IndicatorPropertyId.Multilevel,
            IndicatorPropertyId.Binary,
            IndicatorPropertyId.OnOffPeriod,
            IndicatorPropertyId.OnOffCycles,
            IndicatorPropertyId.OnTimeWithinOnOffPeriod,
        ];

        IndicatorCommandClass.IndicatorSupportedReportCommand command =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Create(
                IndicatorId.NodeIdentify,
                IndicatorId.Buzzer,
                properties);

        (IndicatorId nextIndicatorId, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(IndicatorId.Buzzer, nextIndicatorId);
        Assert.HasCount(5, propertyIds);
        Assert.Contains(IndicatorPropertyId.Multilevel, propertyIds);
        Assert.Contains(IndicatorPropertyId.Binary, propertyIds);
        Assert.Contains(IndicatorPropertyId.OnOffPeriod, propertyIds);
        Assert.Contains(IndicatorPropertyId.OnOffCycles, propertyIds);
        Assert.Contains(IndicatorPropertyId.OnTimeWithinOnOffPeriod, propertyIds);
    }

    [TestMethod]
    public void SupportedReport_RoundTrip_WithSoundLevel()
    {
        HashSet<IndicatorPropertyId> properties =
        [
            IndicatorPropertyId.Multilevel,
            IndicatorPropertyId.SoundLevel,
        ];

        IndicatorCommandClass.IndicatorSupportedReportCommand command =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Create(
                IndicatorId.Buzzer,
                (IndicatorId)0x00,
                properties);

        (IndicatorId nextIndicatorId, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((IndicatorId)0x00, nextIndicatorId);
        Assert.HasCount(2, propertyIds);
        Assert.Contains(IndicatorPropertyId.Multilevel, propertyIds);
        Assert.Contains(IndicatorPropertyId.SoundLevel, propertyIds);
    }

    [TestMethod]
    public void SupportedReport_RoundTrip_Empty()
    {
        HashSet<IndicatorPropertyId> properties = [];

        IndicatorCommandClass.IndicatorSupportedReportCommand command =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Create(
                (IndicatorId)0x00,
                (IndicatorId)0x00,
                properties);

        (IndicatorId nextIndicatorId, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorCommandClass.IndicatorSupportedReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((IndicatorId)0x00, nextIndicatorId);
        Assert.IsEmpty(propertyIds);
    }

    #endregion
}
