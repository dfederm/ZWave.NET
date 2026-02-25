using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelCommandClassTests
{
    [TestMethod]
    public void EndpointGet_Create_HasCorrectFormat()
    {
        MultiChannelCommandClass.MultiChannelEndpointGetCommand command = MultiChannelCommandClass.MultiChannelEndpointGetCommand.Create();

        Assert.AreEqual(CommandClassId.MultiChannel, MultiChannelCommandClass.MultiChannelEndpointGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultiChannelCommand.EndpointGet, MultiChannelCommandClass.MultiChannelEndpointGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void EndpointReport_Parse_StaticIdentical_TwoEndpoints()
    {
        // params[0]=0x40 (Identical=1, Dynamic=0), params[1]=0x02 (2 endpoints)
        byte[] data = [0x60, 0x08, 0x40, 0x02];
        CommandClassFrame frame = new(data);

        MultiChannelEndpointReport report = MultiChannelCommandClass.MultiChannelEndpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.IsDynamic);
        Assert.IsTrue(report.AreIdentical);
        Assert.AreEqual((byte)2, report.IndividualEndpointCount);
        Assert.AreEqual((byte)0, report.AggregatedEndpointCount);
    }

    [TestMethod]
    public void EndpointReport_Parse_DynamicNonIdentical_FiveEndpoints()
    {
        // params[0]=0x80 (Dynamic=1), params[1]=0x05 (5 endpoints)
        byte[] data = [0x60, 0x08, 0x80, 0x05];
        CommandClassFrame frame = new(data);

        MultiChannelEndpointReport report = MultiChannelCommandClass.MultiChannelEndpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.IsDynamic);
        Assert.IsFalse(report.AreIdentical);
        Assert.AreEqual((byte)5, report.IndividualEndpointCount);
        Assert.AreEqual((byte)0, report.AggregatedEndpointCount);
    }

    [TestMethod]
    public void EndpointReport_Parse_Version4_WithAggregated()
    {
        // params[0]=0xC0 (Dynamic=1, Identical=1), params[1]=0x03 (3 individual), params[2]=0x01 (1 aggregated)
        byte[] data = [0x60, 0x08, 0xC0, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        MultiChannelEndpointReport report = MultiChannelCommandClass.MultiChannelEndpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.IsDynamic);
        Assert.IsTrue(report.AreIdentical);
        Assert.AreEqual((byte)3, report.IndividualEndpointCount);
        Assert.AreEqual((byte)1, report.AggregatedEndpointCount);
    }

    [TestMethod]
    public void EndpointReport_Parse_MaxEndpoints()
    {
        // 127 individual endpoints (0x7F)
        byte[] data = [0x60, 0x08, 0x00, 0x7F];
        CommandClassFrame frame = new(data);

        MultiChannelEndpointReport report = MultiChannelCommandClass.MultiChannelEndpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)127, report.IndividualEndpointCount);
    }

    [TestMethod]
    public void EndpointReport_Parse_ReservedBitsIgnored()
    {
        // params[0]=0x3F (all reserved bits set, no flags), params[1]=0x82 (reserved bit7 set, 2 endpoints)
        byte[] data = [0x60, 0x08, 0x3F, 0x82];
        CommandClassFrame frame = new(data);

        MultiChannelEndpointReport report = MultiChannelCommandClass.MultiChannelEndpointReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.IsDynamic);
        Assert.IsFalse(report.AreIdentical);
        Assert.AreEqual((byte)2, report.IndividualEndpointCount);
    }

    [TestMethod]
    public void EndpointReport_Parse_TooShort_Throws()
    {
        // Only 1 parameter byte (needs at least 2)
        byte[] data = [0x60, 0x08, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultiChannelCommandClass.MultiChannelEndpointReportCommand.Parse(frame, NullLogger.Instance));
    }
}
