using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class IndicatorCommandClassTests
{
    #region Get Command

    [TestMethod]
    public void GetCommand_Create_V1_HasCorrectFormat()
    {
        IndicatorCommandClass.IndicatorGetCommand command =
            IndicatorCommandClass.IndicatorGetCommand.Create();

        Assert.AreEqual(CommandClassId.Indicator, IndicatorCommandClass.IndicatorGetCommand.CommandClassId);
        Assert.AreEqual((byte)IndicatorCommand.Get, IndicatorCommandClass.IndicatorGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void GetCommand_Create_V2_HasCorrectFormat()
    {
        IndicatorCommandClass.IndicatorGetCommand command =
            IndicatorCommandClass.IndicatorGetCommand.Create(IndicatorId.NodeIdentify);

        Assert.AreEqual(CommandClassId.Indicator, IndicatorCommandClass.IndicatorGetCommand.CommandClassId);
        Assert.AreEqual((byte)IndicatorCommand.Get, IndicatorCommandClass.IndicatorGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)IndicatorId.NodeIdentify, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_V2_Armed()
    {
        IndicatorCommandClass.IndicatorGetCommand command =
            IndicatorCommandClass.IndicatorGetCommand.Create(IndicatorId.Armed);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)IndicatorId.Armed, command.Frame.CommandParameters.Span[0]);
    }

    #endregion

    #region Set Command

    [TestMethod]
    public void SetCommand_Create_V1_HasCorrectFormat()
    {
        IndicatorCommandClass.IndicatorSetCommand command =
            IndicatorCommandClass.IndicatorSetCommand.Create(0xFF);

        Assert.AreEqual(CommandClassId.Indicator, IndicatorCommandClass.IndicatorSetCommand.CommandClassId);
        Assert.AreEqual((byte)IndicatorCommand.Set, IndicatorCommandClass.IndicatorSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_V1_Off()
    {
        IndicatorCommandClass.IndicatorSetCommand command =
            IndicatorCommandClass.IndicatorSetCommand.Create(0x00);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_V1_Level()
    {
        IndicatorCommandClass.IndicatorSetCommand command =
            IndicatorCommandClass.IndicatorSetCommand.Create(0x32);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x32, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_V2_SingleObject()
    {
        IndicatorObject[] objects =
        [
            new(IndicatorId.Armed, IndicatorPropertyId.Binary, 0xFF),
        ];

        IndicatorCommandClass.IndicatorSetCommand command =
            IndicatorCommandClass.IndicatorSetCommand.Create(objects);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Indicator0Value(1) + Reserved|Count(1) + 1 object(3) = 5
        Assert.AreEqual(5, parameters.Length);
        Assert.AreEqual(0x00, parameters[0]); // Indicator 0 Value
        Assert.AreEqual(0x01, parameters[1] & 0b0001_1111); // Object count
        Assert.AreEqual(0x00, parameters[1] & 0b1110_0000); // Reserved bits
        Assert.AreEqual((byte)IndicatorId.Armed, parameters[2]);
        Assert.AreEqual((byte)IndicatorPropertyId.Binary, parameters[3]);
        Assert.AreEqual(0xFF, parameters[4]);
    }

    [TestMethod]
    public void SetCommand_Create_V2_MultipleObjects()
    {
        IndicatorObject[] objects =
        [
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnOffPeriod, 0x08),
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnOffCycles, 0x03),
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnTimeWithinOnOffPeriod, 0x06),
        ];

        IndicatorCommandClass.IndicatorSetCommand command =
            IndicatorCommandClass.IndicatorSetCommand.Create(objects);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        // Indicator0Value(1) + Reserved|Count(1) + 3 objects(9) = 11
        Assert.AreEqual(11, parameters.Length);
        Assert.AreEqual(0x00, parameters[0]); // Indicator 0 Value
        Assert.AreEqual(0x03, parameters[1] & 0b0001_1111); // Object count

        // Object 1: NodeIdentify, OnOffPeriod, 0x08
        Assert.AreEqual((byte)IndicatorId.NodeIdentify, parameters[2]);
        Assert.AreEqual((byte)IndicatorPropertyId.OnOffPeriod, parameters[3]);
        Assert.AreEqual(0x08, parameters[4]);

        // Object 2: NodeIdentify, OnOffCycles, 0x03
        Assert.AreEqual((byte)IndicatorId.NodeIdentify, parameters[5]);
        Assert.AreEqual((byte)IndicatorPropertyId.OnOffCycles, parameters[6]);
        Assert.AreEqual(0x03, parameters[7]);

        // Object 3: NodeIdentify, OnTimeWithinOnOffPeriod, 0x06
        Assert.AreEqual((byte)IndicatorId.NodeIdentify, parameters[8]);
        Assert.AreEqual((byte)IndicatorPropertyId.OnTimeWithinOnOffPeriod, parameters[9]);
        Assert.AreEqual(0x06, parameters[10]);
    }

    [TestMethod]
    public void SetCommand_Create_IdentifyPattern()
    {
        // Verify the exact Identify pattern from spec Table 6.45
        IndicatorObject[] objects =
        [
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnOffPeriod, 0x08),
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnOffCycles, 0x03),
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnTimeWithinOnOffPeriod, 0x06),
        ];

        IndicatorCommandClass.IndicatorSetCommand command =
            IndicatorCommandClass.IndicatorSetCommand.Create(objects);

        // Verify the exact wire bytes match spec Table 6.45
        ReadOnlySpan<byte> data = command.Frame.Data.Span;
        Assert.AreEqual(0x87, data[0]); // CC = COMMAND_CLASS_INDICATOR
        Assert.AreEqual(0x01, data[1]); // Command = INDICATOR_SET
        Assert.AreEqual(0x00, data[2]); // Indicator 0 Value = 0x00
        Assert.AreEqual(0x03, data[3]); // Object count = 3
        Assert.AreEqual(0x50, data[4]); // Indicator ID 1 = Node Identify
        Assert.AreEqual(0x03, data[5]); // Property ID 1 = On/Off Period
        Assert.AreEqual(0x08, data[6]); // Value 1 = 0x08
        Assert.AreEqual(0x50, data[7]); // Indicator ID 2 = Node Identify
        Assert.AreEqual(0x04, data[8]); // Property ID 2 = On/Off Cycles
        Assert.AreEqual(0x03, data[9]); // Value 2 = 0x03
        Assert.AreEqual(0x50, data[10]); // Indicator ID 3 = Node Identify
        Assert.AreEqual(0x05, data[11]); // Property ID 3 = On time within On/Off period
        Assert.AreEqual(0x06, data[12]); // Value 3 = 0x06
    }

    #endregion

    #region Report Parse

    [TestMethod]
    public void Report_Parse_V1_On()
    {
        byte[] data = [0x87, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        IndicatorReport report = IndicatorCommandClass.IndicatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFF, report.Indicator0Value);
        Assert.IsEmpty(report.Objects);
    }

    [TestMethod]
    public void Report_Parse_V1_Off()
    {
        byte[] data = [0x87, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        IndicatorReport report = IndicatorCommandClass.IndicatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.Indicator0Value);
        Assert.IsEmpty(report.Objects);
    }

    [TestMethod]
    public void Report_Parse_V1_MidLevel()
    {
        byte[] data = [0x87, 0x03, 0x32];
        CommandClassFrame frame = new(data);

        IndicatorReport report = IndicatorCommandClass.IndicatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x32, report.Indicator0Value);
        Assert.IsEmpty(report.Objects);
    }

    [TestMethod]
    public void Report_Parse_V2_WithObjects()
    {
        byte[] data =
        [
            0x87, 0x03, // CC + Command
            0x00,       // Indicator 0 Value
            0x02,       // Object count = 2
            0x01, 0x01, 0x63, // Indicator=Armed, Property=Multilevel, Value=99
            0x01, 0x02, 0xFF, // Indicator=Armed, Property=Binary, Value=0xFF
        ];
        CommandClassFrame frame = new(data);

        IndicatorReport report = IndicatorCommandClass.IndicatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.Indicator0Value);
        Assert.HasCount(2, report.Objects);

        Assert.AreEqual(IndicatorId.Armed, report.Objects[0].IndicatorId);
        Assert.AreEqual(IndicatorPropertyId.Multilevel, report.Objects[0].PropertyId);
        Assert.AreEqual((byte)0x63, report.Objects[0].Value);

        Assert.AreEqual(IndicatorId.Armed, report.Objects[1].IndicatorId);
        Assert.AreEqual(IndicatorPropertyId.Binary, report.Objects[1].PropertyId);
        Assert.AreEqual((byte)0xFF, report.Objects[1].Value);
    }

    [TestMethod]
    public void Report_Parse_V2_ZeroObjectCount()
    {
        // V2 report with object count = 0 (just the Indicator 0 Value field is relevant)
        byte[] data = [0x87, 0x03, 0x63, 0x00];
        CommandClassFrame frame = new(data);

        IndicatorReport report = IndicatorCommandClass.IndicatorReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x63, report.Indicator0Value);
        Assert.IsEmpty(report.Objects);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        byte[] data = [0x87, 0x03]; // No parameters
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => IndicatorCommandClass.IndicatorReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_V2_ObjectCountExceedsData_Throws()
    {
        // Declares 2 objects but only has data for 1
        byte[] data =
        [
            0x87, 0x03,
            0x00,       // Indicator 0 Value
            0x02,       // Object count = 2
            0x01, 0x01, 0x63, // Only 1 object provided
        ];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => IndicatorCommandClass.IndicatorReportCommand.Parse(frame, NullLogger.Instance));
    }

    #endregion

    #region Report Create (Bidirectional)

    [TestMethod]
    public void ReportCommand_Create_V1()
    {
        IndicatorCommandClass.IndicatorReportCommand command =
            IndicatorCommandClass.IndicatorReportCommand.Create(0xFF);

        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ReportCommand_Create_V2_WithObjects()
    {
        IndicatorObject[] objects =
        [
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.Binary, 0xFF),
        ];

        IndicatorCommandClass.IndicatorReportCommand command =
            IndicatorCommandClass.IndicatorReportCommand.Create(0x00, objects);

        ReadOnlySpan<byte> parameters = command.Frame.CommandParameters.Span;
        Assert.AreEqual(5, parameters.Length);
        Assert.AreEqual(0x00, parameters[0]); // Indicator 0 Value
        Assert.AreEqual(0x01, parameters[1]); // Object count
        Assert.AreEqual((byte)IndicatorId.NodeIdentify, parameters[2]);
        Assert.AreEqual((byte)IndicatorPropertyId.Binary, parameters[3]);
        Assert.AreEqual(0xFF, parameters[4]);
    }

    #endregion

    #region Report Round-trip

    [TestMethod]
    public void Report_RoundTrip_V1()
    {
        IndicatorCommandClass.IndicatorReportCommand command =
            IndicatorCommandClass.IndicatorReportCommand.Create(0x63);

        IndicatorReport report = IndicatorCommandClass.IndicatorReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x63, report.Indicator0Value);
        Assert.IsEmpty(report.Objects);
    }

    [TestMethod]
    public void Report_RoundTrip_V2()
    {
        IndicatorObject[] objects =
        [
            new(IndicatorId.Armed, IndicatorPropertyId.Multilevel, 0x32),
            new(IndicatorId.Armed, IndicatorPropertyId.Binary, 0xFF),
        ];

        IndicatorCommandClass.IndicatorReportCommand command =
            IndicatorCommandClass.IndicatorReportCommand.Create(0x00, objects);

        IndicatorReport report = IndicatorCommandClass.IndicatorReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, report.Indicator0Value);
        Assert.HasCount(2, report.Objects);
        Assert.AreEqual(IndicatorId.Armed, report.Objects[0].IndicatorId);
        Assert.AreEqual(IndicatorPropertyId.Multilevel, report.Objects[0].PropertyId);
        Assert.AreEqual((byte)0x32, report.Objects[0].Value);
        Assert.AreEqual(IndicatorId.Armed, report.Objects[1].IndicatorId);
        Assert.AreEqual(IndicatorPropertyId.Binary, report.Objects[1].PropertyId);
        Assert.AreEqual((byte)0xFF, report.Objects[1].Value);
    }

    #endregion
}
