using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class VersionCommandClassTests
{
    [TestMethod]
    public void VersionGetCommand_Create_HasCorrectFormat()
    {
        VersionCommandClass.VersionGetCommand command = VersionCommandClass.VersionGetCommand.Create();

        Assert.AreEqual(CommandClassId.Version, VersionCommandClass.VersionGetCommand.CommandClassId);
        Assert.AreEqual((byte)VersionCommand.Get, VersionCommandClass.VersionGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void VersionReport_Parse_Version1_MinimalPayload()
    {
        // CC=0x86, Cmd=0x12, LibraryType=0x06, ProtocolVersion=7, ProtocolSubVersion=21,
        // Firmware0Version=1, Firmware0SubVersion=5
        byte[] data = [0x86, 0x12, 0x06, 0x07, 0x15, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        VersionReport report = VersionCommandClass.VersionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ZWaveLibraryType.RoutingEndNode, report.LibraryType);
        Assert.AreEqual(new Version(7, 21), report.ProtocolVersion);
        Assert.HasCount(1, report.FirmwareVersions);
        Assert.AreEqual(new Version(1, 5), report.FirmwareVersions[0]);
        Assert.IsNull(report.HardwareVersion);
    }

    [TestMethod]
    public void VersionReport_Parse_Version2_WithHardwareVersion()
    {
        // CC=0x86, Cmd=0x12, LibraryType=0x03, ProtocolVersion=6, ProtocolSubVersion=4,
        // Firmware0Version=2, Firmware0SubVersion=1,
        // HardwareVersion=3, NumFirmwareTargets=0
        byte[] data = [0x86, 0x12, 0x03, 0x06, 0x04, 0x02, 0x01, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        VersionReport report = VersionCommandClass.VersionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ZWaveLibraryType.EnhancedEndNode, report.LibraryType);
        Assert.AreEqual(new Version(6, 4), report.ProtocolVersion);
        Assert.HasCount(1, report.FirmwareVersions);
        Assert.AreEqual(new Version(2, 1), report.FirmwareVersions[0]);
        Assert.AreEqual((byte)3, report.HardwareVersion);
    }

    [TestMethod]
    public void VersionReport_Parse_Version2_MultipleFirmwareVersions()
    {
        // CC=0x86, Cmd=0x12, LibraryType=0x07, ProtocolVersion=7, ProtocolSubVersion=18,
        // Firmware0Version=5, Firmware0SubVersion=3,
        // HardwareVersion=1, NumFirmwareTargets=2,
        // Firmware1Version=3, Firmware1SubVersion=7,
        // Firmware2Version=4, Firmware2SubVersion=2
        byte[] data = [0x86, 0x12, 0x07, 0x07, 0x12, 0x05, 0x03, 0x01, 0x02, 0x03, 0x07, 0x04, 0x02];
        CommandClassFrame frame = new(data);

        VersionReport report = VersionCommandClass.VersionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ZWaveLibraryType.BridgeController, report.LibraryType);
        Assert.AreEqual(new Version(7, 18), report.ProtocolVersion);
        Assert.HasCount(3, report.FirmwareVersions);
        Assert.AreEqual(new Version(5, 3), report.FirmwareVersions[0]);
        Assert.AreEqual(new Version(3, 7), report.FirmwareVersions[1]);
        Assert.AreEqual(new Version(4, 2), report.FirmwareVersions[2]);
        Assert.AreEqual((byte)1, report.HardwareVersion);
    }

    [TestMethod]
    public void VersionReport_Parse_AllLibraryTypes()
    {
        // Verify all library type byte values parse correctly
        byte[][] testCases =
        [
            [0x86, 0x12, 0x00, 0x01, 0x00, 0x01, 0x00], // NotApplicable
            [0x86, 0x12, 0x01, 0x01, 0x00, 0x01, 0x00], // StaticController
            [0x86, 0x12, 0x02, 0x01, 0x00, 0x01, 0x00], // Controller
            [0x86, 0x12, 0x03, 0x01, 0x00, 0x01, 0x00], // EnhancedEndNode
            [0x86, 0x12, 0x04, 0x01, 0x00, 0x01, 0x00], // EndNode
            [0x86, 0x12, 0x05, 0x01, 0x00, 0x01, 0x00], // Installer
            [0x86, 0x12, 0x06, 0x01, 0x00, 0x01, 0x00], // RoutingEndNode
            [0x86, 0x12, 0x07, 0x01, 0x00, 0x01, 0x00], // BridgeController
            [0x86, 0x12, 0x08, 0x01, 0x00, 0x01, 0x00], // DeviceUnderTest
            [0x86, 0x12, 0x09, 0x01, 0x00, 0x01, 0x00], // NotApplicable2
            [0x86, 0x12, 0x0A, 0x01, 0x00, 0x01, 0x00], // AvRemote
            [0x86, 0x12, 0x0B, 0x01, 0x00, 0x01, 0x00], // AvDevice
        ];

        ZWaveLibraryType[] expected =
        [
            ZWaveLibraryType.NotApplicable,
            ZWaveLibraryType.StaticController,
            ZWaveLibraryType.Controller,
            ZWaveLibraryType.EnhancedEndNode,
            ZWaveLibraryType.EndNode,
            ZWaveLibraryType.Installer,
            ZWaveLibraryType.RoutingEndNode,
            ZWaveLibraryType.BridgeController,
            ZWaveLibraryType.DeviceUnderTest,
            ZWaveLibraryType.NotApplicable2,
            ZWaveLibraryType.AvRemote,
            ZWaveLibraryType.AvDevice,
        ];

        for (int i = 0; i < testCases.Length; i++)
        {
            CommandClassFrame frame = new(testCases[i]);
            VersionReport report = VersionCommandClass.VersionReportCommand.Parse(frame, NullLogger.Instance);
            Assert.AreEqual(expected[i], report.LibraryType, $"Library type mismatch for test case {i}");
        }
    }

    [TestMethod]
    public void VersionReport_Parse_TooShort_Throws()
    {
        // CC=0x86, Cmd=0x12, only 4 bytes of parameters (need 5)
        byte[] data = [0x86, 0x12, 0x06, 0x07, 0x15, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void VersionReport_Parse_EmptyPayload_Throws()
    {
        byte[] data = [0x86, 0x12];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void VersionReport_Parse_TruncatedFirmwareVersions_Throws()
    {
        // CC=0x86, Cmd=0x12, LibraryType=0x03, ProtocolVersion=6, ProtocolSubVersion=4,
        // Firmware0Version=2, Firmware0SubVersion=1,
        // HardwareVersion=1, NumFirmwareTargets=2 (declares 2 extra firmware versions),
        // but only 1 extra firmware version is present (payload truncated)
        byte[] data = [0x86, 0x12, 0x03, 0x06, 0x04, 0x02, 0x01, 0x01, 0x02, 0x03, 0x07];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionReportCommand.Parse(frame, NullLogger.Instance));
    }
}
