using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class ThermostatOperatingStateCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ThermostatOperatingStateCommandClass.ThermostatOperatingStateGetCommand command =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateGetCommand.Create();

        Assert.AreEqual(CommandClassId.ThermostatOperatingState, ThermostatOperatingStateCommandClass.ThermostatOperatingStateGetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatOperatingStateCommand.Get, ThermostatOperatingStateCommandClass.ThermostatOperatingStateGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Idle()
    {
        // CC=0x42, Cmd=0x03, State=0x00 (Idle)
        byte[] data = [0x42, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        ThermostatOperatingStateReport report =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatOperatingState.Idle, report.OperatingState);
    }

    [TestMethod]
    public void Report_Parse_Heating()
    {
        // CC=0x42, Cmd=0x03, State=0x01 (Heating)
        byte[] data = [0x42, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        ThermostatOperatingStateReport report =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatOperatingState.Heating, report.OperatingState);
    }

    [TestMethod]
    public void Report_Parse_Cooling()
    {
        // CC=0x42, Cmd=0x03, State=0x02 (Cooling)
        byte[] data = [0x42, 0x03, 0x02];
        CommandClassFrame frame = new(data);

        ThermostatOperatingStateReport report =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatOperatingState.Cooling, report.OperatingState);
    }

    [TestMethod]
    public void Report_Parse_VentEconomizer()
    {
        // CC=0x42, Cmd=0x03, State=0x06 (Vent/Economizer)
        byte[] data = [0x42, 0x03, 0x06];
        CommandClassFrame frame = new(data);

        ThermostatOperatingStateReport report =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatOperatingState.VentEconomizer, report.OperatingState);
    }

    [TestMethod]
    public void Report_Parse_AuxHeating_V2()
    {
        // CC=0x42, Cmd=0x03, State=0x07 (Aux Heating, added in V2)
        byte[] data = [0x42, 0x03, 0x07];
        CommandClassFrame frame = new(data);

        ThermostatOperatingStateReport report =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatOperatingState.AuxHeating, report.OperatingState);
    }

    [TestMethod]
    public void Report_Parse_ThirdStageAuxHeat_V2()
    {
        // CC=0x42, Cmd=0x03, State=0x0B (3rd Stage Aux Heat, added in V2)
        byte[] data = [0x42, 0x03, 0x0B];
        CommandClassFrame frame = new(data);

        ThermostatOperatingStateReport report =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatOperatingState.ThirdStageAuxHeat, report.OperatingState);
    }

    [TestMethod]
    public void Report_Parse_ReservedValue_Preserved()
    {
        // Forward compatibility: reserved/unknown values should be preserved
        // CC=0x42, Cmd=0x03, State=0x0F (reserved)
        byte[] data = [0x42, 0x03, 0x0F];
        CommandClassFrame frame = new(data);

        ThermostatOperatingStateReport report =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ThermostatOperatingState)0x0F, report.OperatingState);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x42, Cmd=0x03, no parameters
        byte[] data = [0x42, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ThermostatOperatingStateCommandClass.ThermostatOperatingStateReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void LoggingSupportedGetCommand_Create_HasCorrectFormat()
    {
        ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedGetCommand command =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.ThermostatOperatingState, ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatOperatingStateCommand.LoggingSupportedGet, ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void LoggingSupportedReport_Parse_HeatingAndCooling()
    {
        // CC=0x42, Cmd=0x04, BitMask=0b0000_0110 (bits 1 and 2 = Heating and Cooling)
        // Per spec: bit 0 is NOT allocated and must be zero
        byte[] data = [0x42, 0x04, 0x06];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatOperatingState> supported =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(ThermostatOperatingState.Heating, supported);
        Assert.Contains(ThermostatOperatingState.Cooling, supported);
    }

    [TestMethod]
    public void LoggingSupportedReport_Parse_MultipleV2States()
    {
        // BitMask1: 0b0000_0010 (bit 1 = Heating)
        // BitMask2: 0b0000_0001 (bit 8 = SecondStageHeating)
        byte[] data = [0x42, 0x04, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatOperatingState> supported =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(ThermostatOperatingState.Heating, supported);
        Assert.Contains(ThermostatOperatingState.SecondStageHeating, supported);
    }

    [TestMethod]
    public void LoggingSupportedReport_Parse_Bit0NotAllocated_Skipped()
    {
        // Per spec: bit 0 is not allocated. Even if set, startBit:1 causes it to be skipped.
        // CC=0x42, Cmd=0x04, BitMask=0b0000_0011 (bits 0 and 1)
        byte[] data = [0x42, 0x04, 0x03];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatOperatingState> supported =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedReportCommand.Parse(frame, NullLogger.Instance);

        // Only Heating (bit 1) should be included; bit 0 is skipped
        Assert.HasCount(1, supported);
        Assert.Contains(ThermostatOperatingState.Heating, supported);
    }

    [TestMethod]
    public void LoggingSupportedReport_Parse_EmptyBitmask_ReturnsEmpty()
    {
        // CC=0x42, Cmd=0x04, no bitmask bytes
        byte[] data = [0x42, 0x04];
        CommandClassFrame frame = new(data);

        HashSet<ThermostatOperatingState> supported =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void LoggingGetCommand_Create_HeatingAndCooling()
    {
        HashSet<ThermostatOperatingState> requestedStates =
        [
            ThermostatOperatingState.Heating,
            ThermostatOperatingState.Cooling,
        ];

        ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingGetCommand command =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingGetCommand.Create(requestedStates);

        Assert.AreEqual(CommandClassId.ThermostatOperatingState, ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingGetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatOperatingStateCommand.LoggingGet, ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingGetCommand.CommandId);

        // Heating=1 (bit 1), Cooling=2 (bit 2) → 0b0000_0110 = 0x06
        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x06, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void LoggingGetCommand_Create_SecondStageHeating()
    {
        HashSet<ThermostatOperatingState> requestedStates =
        [
            ThermostatOperatingState.SecondStageHeating,
        ];

        ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingGetCommand command =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingGetCommand.Create(requestedStates);

        // SecondStageHeating=8 (bit 8) → byte 0: 0x00, byte 1: 0x01
        Assert.AreEqual(2, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_SingleEntry()
    {
        // CC=0x42, Cmd=0x06
        // ReportsToFollow=0x00
        // Entry 1: Reserved(4bits)+LogType=0x01 (Heating), TodayH=2, TodayM=30, YestH=5, YestM=45
        byte[] data = [0x42, 0x06, 0x00, 0x01, 0x02, 0x1E, 0x05, 0x2D];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        byte reportsToFollow =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance);

        Assert.AreEqual(0, reportsToFollow);
        Assert.HasCount(1, entries);
        Assert.AreEqual(ThermostatOperatingState.Heating, entries[0].OperatingState);
        Assert.AreEqual(new TimeSpan(2, 30, 0), entries[0].UsageToday);
        Assert.AreEqual(new TimeSpan(5, 45, 0), entries[0].UsageYesterday);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_MultipleEntries()
    {
        // CC=0x42, Cmd=0x06
        // ReportsToFollow=0x00
        // Entry 1: LogType=0x01 (Heating), TodayH=1, TodayM=15, YestH=3, YestM=30
        // Entry 2: LogType=0x02 (Cooling), TodayH=0, TodayM=45, YestH=2, YestM=0
        byte[] data = [0x42, 0x06, 0x00, 0x01, 0x01, 0x0F, 0x03, 0x1E, 0x02, 0x00, 0x2D, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        byte reportsToFollow =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance);

        Assert.AreEqual(0, reportsToFollow);
        Assert.HasCount(2, entries);

        Assert.AreEqual(ThermostatOperatingState.Heating, entries[0].OperatingState);
        Assert.AreEqual(new TimeSpan(1, 15, 0), entries[0].UsageToday);
        Assert.AreEqual(new TimeSpan(3, 30, 0), entries[0].UsageYesterday);

        Assert.AreEqual(ThermostatOperatingState.Cooling, entries[1].OperatingState);
        Assert.AreEqual(new TimeSpan(0, 45, 0), entries[1].UsageToday);
        Assert.AreEqual(new TimeSpan(2, 0, 0), entries[1].UsageYesterday);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_WithReportsToFollow()
    {
        // CC=0x42, Cmd=0x06
        // ReportsToFollow=0x02
        // Entry 1: LogType=0x03 (FanOnly), TodayH=0, TodayM=10, YestH=0, YestM=20
        byte[] data = [0x42, 0x06, 0x02, 0x03, 0x00, 0x0A, 0x00, 0x14];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        byte reportsToFollow =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance);

        Assert.AreEqual(2, reportsToFollow);
        Assert.HasCount(1, entries);
        Assert.AreEqual(ThermostatOperatingState.FanOnly, entries[0].OperatingState);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_ZeroUsage()
    {
        // CC=0x42, Cmd=0x06
        // ReportsToFollow=0x00
        // Entry 1: LogType=0x01 (Heating), all zeros
        byte[] data = [0x42, 0x06, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        byte reportsToFollow =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance);

        Assert.AreEqual(0, reportsToFollow);
        Assert.HasCount(1, entries);
        Assert.AreEqual(TimeSpan.Zero, entries[0].UsageToday);
        Assert.AreEqual(TimeSpan.Zero, entries[0].UsageYesterday);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_NoEntries()
    {
        // CC=0x42, Cmd=0x06, ReportsToFollow=0x00, no entries
        byte[] data = [0x42, 0x06, 0x00];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        byte reportsToFollow =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance);

        Assert.AreEqual(0, reportsToFollow);
        Assert.IsEmpty(entries);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_PartialEntry_Ignored()
    {
        // CC=0x42, Cmd=0x06, ReportsToFollow=0x00, incomplete entry (only 3 of 5 bytes)
        byte[] data = [0x42, 0x06, 0x00, 0x01, 0x02, 0x1E];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        byte reportsToFollow =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance);

        // Partial entries should be skipped (not enough bytes for a complete entry)
        Assert.AreEqual(0, reportsToFollow);
        Assert.IsEmpty(entries);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_TooShort_Throws()
    {
        // CC=0x42, Cmd=0x06, no parameters
        byte[] data = [0x42, 0x06];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        Assert.Throws<ZWaveException>(
            () => ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance));
    }

    [TestMethod]
    public void LoggingReport_ParseInto_ReservedBitsInLogType_Ignored()
    {
        // CC=0x42, Cmd=0x06
        // ReportsToFollow=0x00
        // Entry 1: upper nibble=0xF0 (reserved) + LogType=0x01 → byte=0xF1
        // TodayH=1, TodayM=0, YestH=2, YestM=0
        byte[] data = [0x42, 0x06, 0x00, 0xF1, 0x01, 0x00, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        List<ThermostatOperatingStateLogEntry> entries = [];
        byte reportsToFollow =
            ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame, entries, NullLogger.Instance);

        Assert.HasCount(1, entries);
        // Only lower 4 bits are the log type
        Assert.AreEqual(ThermostatOperatingState.Heating, entries[0].OperatingState);
        Assert.AreEqual(new TimeSpan(1, 0, 0), entries[0].UsageToday);
        Assert.AreEqual(new TimeSpan(2, 0, 0), entries[0].UsageYesterday);
    }

    [TestMethod]
    public void LoggingReport_ParseInto_AppendsToExistingList()
    {
        // Verify ParseInto appends to an existing list (simulating multi-frame aggregation)
        List<ThermostatOperatingStateLogEntry> entries = [];

        // First frame: Heating entry, 1 report to follow
        byte[] data1 = [0x42, 0x06, 0x01, 0x01, 0x01, 0x00, 0x02, 0x00];
        CommandClassFrame frame1 = new(data1);
        byte reportsToFollow = ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame1, entries, NullLogger.Instance);
        Assert.AreEqual(1, reportsToFollow);
        Assert.HasCount(1, entries);

        // Second frame: Cooling entry, 0 reports to follow
        byte[] data2 = [0x42, 0x06, 0x00, 0x02, 0x03, 0x00, 0x04, 0x00];
        CommandClassFrame frame2 = new(data2);
        reportsToFollow = ThermostatOperatingStateCommandClass.ThermostatOperatingStateLoggingReportCommand.ParseInto(frame2, entries, NullLogger.Instance);
        Assert.AreEqual(0, reportsToFollow);

        // Both entries accumulated in the same list
        Assert.HasCount(2, entries);
        Assert.AreEqual(ThermostatOperatingState.Heating, entries[0].OperatingState);
        Assert.AreEqual(ThermostatOperatingState.Cooling, entries[1].OperatingState);
    }
}
