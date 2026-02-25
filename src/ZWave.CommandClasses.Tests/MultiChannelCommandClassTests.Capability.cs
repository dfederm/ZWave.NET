using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelCommandClassTests
{
    [TestMethod]
    public void CapabilityGet_Create_SetsCorrectEndpoint()
    {
        MultiChannelCommandClass.MultiChannelCapabilityGetCommand command = MultiChannelCommandClass.MultiChannelCapabilityGetCommand.Create(5);

        Assert.AreEqual(CommandClassId.MultiChannel, MultiChannelCommandClass.MultiChannelCapabilityGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultiChannelCommand.CapabilityGet, MultiChannelCommandClass.MultiChannelCapabilityGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)5, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void CapabilityGet_Create_RejectsInvalidEndpointIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.MultiChannelCapabilityGetCommand.Create(0x85));
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.MultiChannelCapabilityGetCommand.Create(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.MultiChannelCapabilityGetCommand.Create(128));
    }

    [TestMethod]
    public void CapabilityReport_Parse_StaticEndpoint_WithCCs()
    {
        // EP1, static, Generic=0x10, Specific=0x01, CCs: 0x25 (BinarySwitch), 0x5E (ZWavePlusInfo)
        byte[] data = [0x60, 0x0A, 0x01, 0x10, 0x01, 0x25, 0x5E];
        CommandClassFrame frame = new(data);

        MultiChannelCapabilityReport report = MultiChannelCommandClass.MultiChannelCapabilityReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.IsDynamic);
        Assert.AreEqual((byte)1, report.EndpointIndex);
        Assert.AreEqual((byte)0x10, report.GenericDeviceClass);
        Assert.AreEqual((byte)0x01, report.SpecificDeviceClass);
        Assert.HasCount(2, report.CommandClasses);
        Assert.AreEqual(CommandClassId.BinarySwitch, report.CommandClasses[0].CommandClass);
        Assert.IsTrue(report.CommandClasses[0].IsSupported);
        Assert.IsFalse(report.CommandClasses[0].IsControlled);
        Assert.AreEqual(CommandClassId.ZWavePlusInfo, report.CommandClasses[1].CommandClass);
    }

    [TestMethod]
    public void CapabilityReport_Parse_DynamicEndpoint_NoCCs()
    {
        // EP5, dynamic (bit7=1), Generic=0x21, Specific=0x00, no CCs
        byte[] data = [0x60, 0x0A, 0x85, 0x21, 0x00];
        CommandClassFrame frame = new(data);

        MultiChannelCapabilityReport report = MultiChannelCommandClass.MultiChannelCapabilityReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.IsDynamic);
        Assert.AreEqual((byte)5, report.EndpointIndex);
        Assert.AreEqual((byte)0x21, report.GenericDeviceClass);
        Assert.AreEqual((byte)0x00, report.SpecificDeviceClass);
        Assert.IsEmpty(report.CommandClasses);
    }

    [TestMethod]
    public void CapabilityReport_Parse_WithSupportControlMark()
    {
        // EP2, Generic=0x10, Specific=0x01
        // Supported: 0x25 (BinarySwitch), Controlled: 0x20 (Basic)
        // 0xEF is SupportControlMark
        byte[] data = [0x60, 0x0A, 0x02, 0x10, 0x01, 0x25, 0xEF, 0x20];
        CommandClassFrame frame = new(data);

        MultiChannelCapabilityReport report = MultiChannelCommandClass.MultiChannelCapabilityReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.EndpointIndex);
        Assert.HasCount(2, report.CommandClasses);

        Assert.AreEqual(CommandClassId.BinarySwitch, report.CommandClasses[0].CommandClass);
        Assert.IsTrue(report.CommandClasses[0].IsSupported);
        Assert.IsFalse(report.CommandClasses[0].IsControlled);

        Assert.AreEqual(CommandClassId.Basic, report.CommandClasses[1].CommandClass);
        Assert.IsFalse(report.CommandClasses[1].IsSupported);
        Assert.IsTrue(report.CommandClasses[1].IsControlled);
    }

    [TestMethod]
    public void CapabilityReport_Parse_RemovedDynamicEndpoint()
    {
        // Per spec §4.2.2.6: removed dynamic EP has Generic=0xFF, Specific=0x00, no CCs
        byte[] data = [0x60, 0x0A, 0x83, 0xFF, 0x00];
        CommandClassFrame frame = new(data);

        MultiChannelCapabilityReport report = MultiChannelCommandClass.MultiChannelCapabilityReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.IsDynamic);
        Assert.AreEqual((byte)3, report.EndpointIndex);
        Assert.AreEqual((byte)0xFF, report.GenericDeviceClass);
        Assert.AreEqual((byte)0x00, report.SpecificDeviceClass);
        Assert.IsEmpty(report.CommandClasses);
    }

    [TestMethod]
    public void CapabilityReport_Parse_TooShort_Throws()
    {
        // Only 2 parameter bytes (needs at least 3)
        byte[] data = [0x60, 0x0A, 0x01, 0x10];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultiChannelCommandClass.MultiChannelCapabilityReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CapabilityReport_GetEndpointIndex_ExtractsCorrectly()
    {
        byte[] data = [0x60, 0x0A, 0x07, 0x10, 0x01];
        CommandClassFrame frame = new(data);

        byte endpointIndex = MultiChannelCommandClass.MultiChannelCapabilityReportCommand.GetEndpointIndex(frame);

        Assert.AreEqual((byte)7, endpointIndex);
    }

    [TestMethod]
    public void CapabilityReport_GetEndpointIndex_MasksDynamicBit()
    {
        // EP=3 with dynamic flag (0x83)
        byte[] data = [0x60, 0x0A, 0x83, 0x10, 0x01];
        CommandClassFrame frame = new(data);

        byte endpointIndex = MultiChannelCommandClass.MultiChannelCapabilityReportCommand.GetEndpointIndex(frame);

        Assert.AreEqual((byte)3, endpointIndex);
    }
}
