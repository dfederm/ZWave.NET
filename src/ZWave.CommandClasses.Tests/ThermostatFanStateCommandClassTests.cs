using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class ThermostatFanStateCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ThermostatFanStateCommandClass.ThermostatFanStateGetCommand command =
            ThermostatFanStateCommandClass.ThermostatFanStateGetCommand.Create();

        Assert.AreEqual(CommandClassId.ThermostatFanState, ThermostatFanStateCommandClass.ThermostatFanStateGetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatFanStateCommand.Get, ThermostatFanStateCommandClass.ThermostatFanStateGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Idle()
    {
        // CC=0x45, Cmd=0x03, State=0x00 (Idle)
        byte[] data = [0x45, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        ThermostatFanStateReport report =
            ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanOperatingState.Idle, report.FanOperatingState);
    }

    [TestMethod]
    public void Report_Parse_RunningLow()
    {
        // CC=0x45, Cmd=0x03, State=0x01 (Running Low)
        byte[] data = [0x45, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        ThermostatFanStateReport report =
            ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanOperatingState.RunningLow, report.FanOperatingState);
    }

    [TestMethod]
    public void Report_Parse_RunningHigh()
    {
        // CC=0x45, Cmd=0x03, State=0x02 (Running High)
        byte[] data = [0x45, 0x03, 0x02];
        CommandClassFrame frame = new(data);

        ThermostatFanStateReport report =
            ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanOperatingState.RunningHigh, report.FanOperatingState);
    }

    [TestMethod]
    public void Report_Parse_RunningMedium_V2()
    {
        // CC=0x45, Cmd=0x03, State=0x03 (Running Medium, added in V2)
        byte[] data = [0x45, 0x03, 0x03];
        CommandClassFrame frame = new(data);

        ThermostatFanStateReport report =
            ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanOperatingState.RunningMedium, report.FanOperatingState);
    }

    [TestMethod]
    public void Report_Parse_QuietCirculationMode_V2()
    {
        // CC=0x45, Cmd=0x03, State=0x08 (Quiet Circulation Mode, added in V2)
        byte[] data = [0x45, 0x03, 0x08];
        CommandClassFrame frame = new(data);

        ThermostatFanStateReport report =
            ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatFanOperatingState.QuietCirculationMode, report.FanOperatingState);
    }

    [TestMethod]
    public void Report_Parse_ReservedValue_Preserved()
    {
        // A reserved/unknown value should be preserved (forward compatibility)
        // CC=0x45, Cmd=0x03, State=0x0F (reserved)
        byte[] data = [0x45, 0x03, 0x0F];
        CommandClassFrame frame = new(data);

        ThermostatFanStateReport report =
            ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ThermostatFanOperatingState)0x0F, report.FanOperatingState);
    }

    [TestMethod]
    public void Report_Parse_FullByteValue_ForwardCompatible()
    {
        // V1 defines the state as 4 bits, but we parse the full byte for forward compatibility.
        // CC=0x45, Cmd=0x03, State=0x10 (upper nibble set)
        byte[] data = [0x45, 0x03, 0x10];
        CommandClassFrame frame = new(data);

        ThermostatFanStateReport report =
            ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ThermostatFanOperatingState)0x10, report.FanOperatingState);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x45, Cmd=0x03, no parameters
        byte[] data = [0x45, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ThermostatFanStateCommandClass.ThermostatFanStateReportCommand.Parse(frame, NullLogger.Instance));
    }
}
