using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class HumidityControlOperatingStateCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateGetCommand command =
            HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateGetCommand.Create();

        Assert.AreEqual(CommandClassId.HumidityControlOperatingState, HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateGetCommand.CommandClassId);
        Assert.AreEqual((byte)HumidityControlOperatingStateCommand.Get, HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Idle()
    {
        // CC=0x6E, Cmd=0x02, OperatingState=0x00 (Idle)
        byte[] data = [0x6E, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        HumidityControlOperatingState state = HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlOperatingState.Idle, state);
    }

    [TestMethod]
    public void Report_Parse_Humidifying()
    {
        // CC=0x6E, Cmd=0x02, OperatingState=0x01 (Humidifying)
        byte[] data = [0x6E, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        HumidityControlOperatingState state = HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlOperatingState.Humidifying, state);
    }

    [TestMethod]
    public void Report_Parse_Dehumidifying()
    {
        // CC=0x6E, Cmd=0x02, OperatingState=0x02 (Dehumidifying)
        byte[] data = [0x6E, 0x02, 0x02];
        CommandClassFrame frame = new(data);

        HumidityControlOperatingState state = HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlOperatingState.Dehumidifying, state);
    }

    [TestMethod]
    public void Report_Parse_ReservedBitsIgnored()
    {
        // Upper 4 bits are reserved and should be ignored
        // CC=0x6E, Cmd=0x02, 0xF1 = reserved bits set + Humidifying
        byte[] data = [0x6E, 0x02, 0xF1];
        CommandClassFrame frame = new(data);

        HumidityControlOperatingState state = HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(HumidityControlOperatingState.Humidifying, state);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x6E, Cmd=0x02, no parameters
        byte[] data = [0x6E, 0x02];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => HumidityControlOperatingStateCommandClass.HumidityControlOperatingStateReportCommand.Parse(frame, NullLogger.Instance));
    }
}
