using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class VersionCommandClassTests
{
    [TestMethod]
    public void ZWaveSoftwareGetCommand_Create_HasCorrectFormat()
    {
        VersionCommandClass.VersionZWaveSoftwareGetCommand command =
            VersionCommandClass.VersionZWaveSoftwareGetCommand.Create();

        Assert.AreEqual(CommandClassId.Version, VersionCommandClass.VersionZWaveSoftwareGetCommand.CommandClassId);
        Assert.AreEqual((byte)VersionCommand.ZWaveSoftwareGet, VersionCommandClass.VersionZWaveSoftwareGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void ZWaveSoftwareReport_Parse_AllFieldsPopulated()
    {
        // CC=0x86, Cmd=0x18,
        // SDK: 6.81.3
        // AppFramework: 2.0.1, BuildNumber: 1234 (0x04D2)
        // HostInterface: 1.2.3, BuildNumber: 5678 (0x162E)
        // Protocol: 7.18.1, BuildNumber: 9012 (0x2334)
        // Application: 5.3.0, BuildNumber: 42 (0x002A)
        byte[] data =
        [
            0x86, 0x18,
            0x06, 0x51, 0x03,       // SDK version: 6.81.3
            0x02, 0x00, 0x01,       // App Framework API version: 2.0.1
            0x04, 0xD2,             // App Framework build number: 1234
            0x01, 0x02, 0x03,       // Host Interface version: 1.2.3
            0x16, 0x2E,             // Host Interface build number: 5678
            0x07, 0x12, 0x01,       // Protocol version: 7.18.1
            0x23, 0x34,             // Protocol build number: 9012
            0x05, 0x03, 0x00,       // Application version: 5.3.0
            0x00, 0x2A,             // Application build number: 42
        ];
        CommandClassFrame frame = new(data);

        VersionSoftwareInfo info = VersionCommandClass.VersionZWaveSoftwareReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new Version(6, 81, 3), info.SdkVersion);
        Assert.AreEqual(new Version(2, 0, 1), info.ApplicationFrameworkApiVersion);
        Assert.AreEqual((ushort)1234, info.ApplicationFrameworkBuildNumber);
        Assert.AreEqual(new Version(1, 2, 3), info.HostInterfaceVersion);
        Assert.AreEqual((ushort)5678, info.HostInterfaceBuildNumber);
        Assert.AreEqual(new Version(7, 18, 1), info.ZWaveProtocolVersion);
        Assert.AreEqual((ushort)9012, info.ZWaveProtocolBuildNumber);
        Assert.AreEqual(new Version(5, 3, 0), info.ApplicationVersion);
        Assert.AreEqual((ushort)42, info.ApplicationBuildNumber);
    }

    [TestMethod]
    public void ZWaveSoftwareReport_Parse_UnusedFieldsAreNull()
    {
        // All version fields set to 0 (unused) and all build numbers set to 0 (unused)
        byte[] data =
        [
            0x86, 0x18,
            0x00, 0x00, 0x00,       // SDK version: unused
            0x00, 0x00, 0x00,       // App Framework API version: unused
            0x00, 0x00,             // App Framework build number: unused
            0x00, 0x00, 0x00,       // Host Interface version: unused
            0x00, 0x00,             // Host Interface build number: unused
            0x00, 0x00, 0x00,       // Protocol version: unused
            0x00, 0x00,             // Protocol build number: unused
            0x00, 0x00, 0x00,       // Application version: unused
            0x00, 0x00,             // Application build number: unused
        ];
        CommandClassFrame frame = new(data);

        VersionSoftwareInfo info = VersionCommandClass.VersionZWaveSoftwareReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNull(info.SdkVersion);
        Assert.IsNull(info.ApplicationFrameworkApiVersion);
        Assert.IsNull(info.ApplicationFrameworkBuildNumber);
        Assert.IsNull(info.HostInterfaceVersion);
        Assert.IsNull(info.HostInterfaceBuildNumber);
        Assert.IsNull(info.ZWaveProtocolVersion);
        Assert.IsNull(info.ZWaveProtocolBuildNumber);
        Assert.IsNull(info.ApplicationVersion);
        Assert.IsNull(info.ApplicationBuildNumber);
    }

    [TestMethod]
    public void ZWaveSoftwareReport_Parse_MixedUsedAndUnused()
    {
        // Only SDK and Protocol versions populated; rest unused
        byte[] data =
        [
            0x86, 0x18,
            0x06, 0x51, 0x09,       // SDK version: 6.81.9
            0x00, 0x00, 0x00,       // App Framework API version: unused
            0x00, 0x00,             // App Framework build number: unused
            0x00, 0x00, 0x00,       // Host Interface version: unused
            0x00, 0x00,             // Host Interface build number: unused
            0x07, 0x12, 0x01,       // Protocol version: 7.18.1
            0x00, 0x00,             // Protocol build number: unused
            0x00, 0x00, 0x00,       // Application version: unused
            0x00, 0x00,             // Application build number: unused
        ];
        CommandClassFrame frame = new(data);

        VersionSoftwareInfo info = VersionCommandClass.VersionZWaveSoftwareReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(new Version(6, 81, 9), info.SdkVersion);
        Assert.IsNull(info.ApplicationFrameworkApiVersion);
        Assert.IsNull(info.ApplicationFrameworkBuildNumber);
        Assert.IsNull(info.HostInterfaceVersion);
        Assert.IsNull(info.HostInterfaceBuildNumber);
        Assert.AreEqual(new Version(7, 18, 1), info.ZWaveProtocolVersion);
        Assert.IsNull(info.ZWaveProtocolBuildNumber);
        Assert.IsNull(info.ApplicationVersion);
        Assert.IsNull(info.ApplicationBuildNumber);
    }

    [TestMethod]
    public void ZWaveSoftwareReport_Parse_TooShort_Throws()
    {
        // Only 22 bytes of parameters (need 23)
        byte[] data =
        [
            0x86, 0x18,
            0x06, 0x51, 0x03,
            0x02, 0x00, 0x01,
            0x04, 0xD2,
            0x01, 0x02, 0x03,
            0x16, 0x2E,
            0x07, 0x12, 0x01,
            0x23, 0x34,
            0x05, 0x03, 0x00,
            0x00,                   // Missing second byte of ApplicationBuildNumber
        ];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionZWaveSoftwareReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ZWaveSoftwareReport_Parse_EmptyPayload_Throws()
    {
        byte[] data = [0x86, 0x18];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionZWaveSoftwareReportCommand.Parse(frame, NullLogger.Instance));
    }
}
