using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class ZWavePlusInfoCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ZWavePlusInfoCommandClass.ZWavePlusInfoGetCommand command = ZWavePlusInfoCommandClass.ZWavePlusInfoGetCommand.Create();

        Assert.AreEqual(CommandClassId.ZWavePlusInfo, ZWavePlusInfoCommandClass.ZWavePlusInfoGetCommand.CommandClassId);
        Assert.AreEqual((byte)ZWavePlusInfoCommand.Get, ZWavePlusInfoCommandClass.ZWavePlusInfoGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_ValidPayload()
    {
        // CC=0x5E, Cmd=0x02, Version=1, RoleType=CSC(0x00), NodeType=Node(0x00),
        // InstallerIcon=0x0100, UserIcon=0x0200
        byte[] data = [0x5E, 0x02, 0x01, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        ZWavePlusInfoReport report = ZWavePlusInfoCommandClass.ZWavePlusInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.ZWavePlusVersion);
        Assert.AreEqual(ZWavePlusRoleType.CentralStaticController, report.RoleType);
        Assert.AreEqual(ZWavePlusNodeType.Node, report.NodeType);
        Assert.AreEqual((ushort)0x0100, report.InstallerIconType);
        Assert.AreEqual((ushort)0x0200, report.UserIconType);
    }

    [TestMethod]
    public void Report_Parse_AllFieldValues()
    {
        // CC=0x5E, Cmd=0x02, Version=2, RoleType=AOEN(0x05), NodeType=IpGateway(0x02),
        // InstallerIcon=0x0701, UserIcon=0x0700
        byte[] data = [0x5E, 0x02, 0x02, 0x05, 0x02, 0x07, 0x01, 0x07, 0x00];
        CommandClassFrame frame = new(data);

        ZWavePlusInfoReport report = ZWavePlusInfoCommandClass.ZWavePlusInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.ZWavePlusVersion);
        Assert.AreEqual(ZWavePlusRoleType.AlwaysOnEndNode, report.RoleType);
        Assert.AreEqual(ZWavePlusNodeType.IpGateway, report.NodeType);
        Assert.AreEqual((ushort)0x0701, report.InstallerIconType);
        Assert.AreEqual((ushort)0x0700, report.UserIconType);
    }

    [TestMethod]
    public void Report_Parse_WakeOnEventEndNode()
    {
        // CC=0x5E, Cmd=0x02, Version=2, RoleType=WOEEN(0x09), NodeType=Node(0x00),
        // InstallerIcon=0x0000, UserIcon=0x0000
        byte[] data = [0x5E, 0x02, 0x02, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        ZWavePlusInfoReport report = ZWavePlusInfoCommandClass.ZWavePlusInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ZWavePlusRoleType.WakeOnEventEndNode, report.RoleType);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x5E, Cmd=0x02, only 6 parameter bytes (need 7)
        byte[] data = [0x5E, 0x02, 0x01, 0x00, 0x00, 0x01, 0x00, 0x02];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ZWavePlusInfoCommandClass.ZWavePlusInfoReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_ExtraBytes_Succeeds()
    {
        // CC=0x5E, Cmd=0x02, 7 parameter bytes + 2 extra (forward compatibility)
        byte[] data = [0x5E, 0x02, 0x01, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0xAA, 0xBB];
        CommandClassFrame frame = new(data);

        ZWavePlusInfoReport report = ZWavePlusInfoCommandClass.ZWavePlusInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, report.ZWavePlusVersion);
        Assert.AreEqual(ZWavePlusRoleType.CentralStaticController, report.RoleType);
        Assert.AreEqual(ZWavePlusNodeType.Node, report.NodeType);
        Assert.AreEqual((ushort)0x0100, report.InstallerIconType);
        Assert.AreEqual((ushort)0x0200, report.UserIconType);
    }

    [TestMethod]
    public void Report_Parse_EmptyPayload_Throws()
    {
        // CC=0x5E, Cmd=0x02, no parameters
        byte[] data = [0x5E, 0x02];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ZWavePlusInfoCommandClass.ZWavePlusInfoReportCommand.Parse(frame, NullLogger.Instance));
    }
}
