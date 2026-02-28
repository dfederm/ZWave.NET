using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class PowerlevelCommandClassTests
{
    [TestMethod]
    public void TestNodeSetCommand_Create_HasCorrectFormat()
    {
        PowerlevelCommandClass.PowerlevelTestNodeSetCommand command =
            PowerlevelCommandClass.PowerlevelTestNodeSetCommand.Create(5, Powerlevel.Minus2dBm, 100);

        Assert.AreEqual(CommandClassId.Powerlevel, PowerlevelCommandClass.PowerlevelTestNodeSetCommand.CommandClassId);
        Assert.AreEqual((byte)PowerlevelCommand.TestNodeSet, PowerlevelCommandClass.PowerlevelTestNodeSetCommand.CommandId);
        Assert.AreEqual(6, command.Frame.Data.Length); // CC + Cmd + NodeID + PowerLevel + FrameCount(2)
        Assert.AreEqual(5, command.Frame.CommandParameters.Span[0]); // Test NodeID
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[1]); // Minus2dBm
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[2]); // Frame count MSB
        Assert.AreEqual(100, command.Frame.CommandParameters.Span[3]); // Frame count LSB
    }

    [TestMethod]
    public void TestNodeSetCommand_Create_LargeFrameCount()
    {
        PowerlevelCommandClass.PowerlevelTestNodeSetCommand command =
            PowerlevelCommandClass.PowerlevelTestNodeSetCommand.Create(10, Powerlevel.Normal, 1000);

        Assert.AreEqual(10, command.Frame.CommandParameters.Span[0]); // Test NodeID
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[1]); // Normal
        Assert.AreEqual(0x03, command.Frame.CommandParameters.Span[2]); // 1000 = 0x03E8 MSB
        Assert.AreEqual(0xE8, command.Frame.CommandParameters.Span[3]); // 1000 = 0x03E8 LSB
    }

    [TestMethod]
    public void TestNodeSetCommand_Create_MaxFrameCount()
    {
        PowerlevelCommandClass.PowerlevelTestNodeSetCommand command =
            PowerlevelCommandClass.PowerlevelTestNodeSetCommand.Create(1, Powerlevel.Minus9dBm, 65535);

        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[2]); // 65535 MSB
        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[3]); // 65535 LSB
    }

    [TestMethod]
    public void TestNodeGetCommand_Create_HasCorrectFormat()
    {
        PowerlevelCommandClass.PowerlevelTestNodeGetCommand command =
            PowerlevelCommandClass.PowerlevelTestNodeGetCommand.Create();

        Assert.AreEqual(CommandClassId.Powerlevel, PowerlevelCommandClass.PowerlevelTestNodeGetCommand.CommandClassId);
        Assert.AreEqual((byte)PowerlevelCommand.TestNodeGet, PowerlevelCommandClass.PowerlevelTestNodeGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void TestNodeReport_Parse_TestSuccess()
    {
        // CC=0x73, Cmd=0x06, NodeID=5, Status=0x01 (Success), AckCount=0x0032 (50)
        byte[] data = [0x73, 0x06, 5, 0x01, 0x00, 50];
        CommandClassFrame frame = new(data);

        PowerlevelTestResult? result = PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)5, result.Value.NodeId);
        Assert.AreEqual(PowerlevelTestStatus.Success, result.Value.Status);
        Assert.AreEqual((ushort)50, result.Value.FrameAcknowledgedCount);
    }

    [TestMethod]
    public void TestNodeReport_Parse_TestFailed()
    {
        // CC=0x73, Cmd=0x06, NodeID=10, Status=0x00 (Failed), AckCount=0x0000
        byte[] data = [0x73, 0x06, 10, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        PowerlevelTestResult? result = PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)10, result.Value.NodeId);
        Assert.AreEqual(PowerlevelTestStatus.Failed, result.Value.Status);
        Assert.AreEqual((ushort)0, result.Value.FrameAcknowledgedCount);
    }

    [TestMethod]
    public void TestNodeReport_Parse_TestInProgress()
    {
        // CC=0x73, Cmd=0x06, NodeID=3, Status=0x02 (InProgress), AckCount=0x000A (10)
        byte[] data = [0x73, 0x06, 3, 0x02, 0x00, 10];
        CommandClassFrame frame = new(data);

        PowerlevelTestResult? result = PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)3, result.Value.NodeId);
        Assert.AreEqual(PowerlevelTestStatus.InProgress, result.Value.Status);
        Assert.AreEqual((ushort)10, result.Value.FrameAcknowledgedCount);
    }

    [TestMethod]
    public void TestNodeReport_Parse_NoTestPerformed_ReturnsNull()
    {
        // CC=0x73, Cmd=0x06, NodeID=0 (no test), Status=0x00, AckCount=0x0000
        byte[] data = [0x73, 0x06, 0, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        PowerlevelTestResult? result = PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TestNodeReport_Parse_LargeAckCount()
    {
        // CC=0x73, Cmd=0x06, NodeID=1, Status=0x01 (Success), AckCount=0xFFFF (65535)
        byte[] data = [0x73, 0x06, 1, 0x01, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        PowerlevelTestResult? result = PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)65535, result.Value.FrameAcknowledgedCount);
    }

    [TestMethod]
    public void TestNodeReport_Parse_TooShort_Throws()
    {
        // CC=0x73, Cmd=0x06, only 3 parameter bytes (need 4)
        byte[] data = [0x73, 0x06, 5, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void TestNodeReport_Parse_NoParameters_Throws()
    {
        // CC=0x73, Cmd=0x06, no parameters
        byte[] data = [0x73, 0x06];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void TestNodeReport_Parse_ReservedStatus_Preserved()
    {
        // CC=0x73, Cmd=0x06, NodeID=5, Status=0x03 (reserved), AckCount=0x0001
        byte[] data = [0x73, 0x06, 5, 0x03, 0x00, 0x01];
        CommandClassFrame frame = new(data);

        PowerlevelTestResult? result = PowerlevelCommandClass.PowerlevelTestNodeReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNotNull(result);
        // Reserved status value is preserved (forward compatibility)
        Assert.AreEqual((PowerlevelTestStatus)0x03, result.Value.Status);
    }
}
