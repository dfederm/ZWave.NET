using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelCommandClassTests
{
    [TestMethod]
    public void EndpointFind_Create_HasCorrectFormat()
    {
        MultiChannelCommandClass.EndpointFindCommand command = MultiChannelCommandClass.EndpointFindCommand.Create(0x10, 0x01);

        Assert.AreEqual(CommandClassId.MultiChannel, MultiChannelCommandClass.EndpointFindCommand.CommandClassId);
        Assert.AreEqual((byte)MultiChannelCommand.EndpointFind, MultiChannelCommandClass.EndpointFindCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x10, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0x01, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void EndpointFind_Create_WildcardAll()
    {
        MultiChannelCommandClass.EndpointFindCommand command = MultiChannelCommandClass.EndpointFindCommand.Create(0xFF, 0xFF);

        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void EndpointFind_Create_RejectsGenericWildcardWithoutSpecificWildcard()
    {
        Assert.Throws<ArgumentException>(() => MultiChannelCommandClass.EndpointFindCommand.Create(0xFF, 0x01));
    }

    [TestMethod]
    public void EndpointFindReport_Parse_MultipleEndpoints()
    {
        // params[0]=0 (reports to follow), params[1]=0x10 (generic), params[2]=0x01 (specific),
        // params[3]=0x01 (EP1), params[4]=0x03 (EP3), params[5]=0x05 (EP5)
        byte[] data = [0x60, 0x0C, 0x00, 0x10, 0x01, 0x01, 0x03, 0x05];
        CommandClassFrame frame = new(data);
        List<byte> endpointIndices = new();

        byte reportsToFollow = MultiChannelCommandClass.EndpointFindReportCommand.Parse(frame, endpointIndices, NullLogger.Instance);

        Assert.AreEqual((byte)0, reportsToFollow);
        Assert.HasCount(3, endpointIndices);
        Assert.AreEqual((byte)1, endpointIndices[0]);
        Assert.AreEqual((byte)3, endpointIndices[1]);
        Assert.AreEqual((byte)5, endpointIndices[2]);
    }

    [TestMethod]
    public void EndpointFindReport_Parse_NoMatches()
    {
        // No matching endpoints: single 0x00 entry per spec §4.2.2.8
        byte[] data = [0x60, 0x0C, 0x00, 0xFF, 0xFF, 0x00];
        CommandClassFrame frame = new(data);
        List<byte> endpointIndices = new();

        MultiChannelCommandClass.EndpointFindReportCommand.Parse(frame, endpointIndices, NullLogger.Instance);

        Assert.IsEmpty(endpointIndices);
    }

    [TestMethod]
    public void EndpointFindReport_Parse_MasksReservedBit()
    {
        // EP byte 0x82 = reserved bit set + EP 2
        byte[] data = [0x60, 0x0C, 0x00, 0x10, 0x01, 0x82];
        CommandClassFrame frame = new(data);
        List<byte> endpointIndices = new();

        MultiChannelCommandClass.EndpointFindReportCommand.Parse(frame, endpointIndices, NullLogger.Instance);

        Assert.HasCount(1, endpointIndices);
        Assert.AreEqual((byte)2, endpointIndices[0]);
    }

    [TestMethod]
    public void EndpointFindReport_Parse_AppendsToExistingList()
    {
        byte[] data = [0x60, 0x0C, 0x00, 0x10, 0x01, 0x03];
        CommandClassFrame frame = new(data);
        List<byte> endpointIndices = new() { 1, 2 };

        MultiChannelCommandClass.EndpointFindReportCommand.Parse(frame, endpointIndices, NullLogger.Instance);

        Assert.HasCount(3, endpointIndices);
        Assert.AreEqual((byte)1, endpointIndices[0]);
        Assert.AreEqual((byte)2, endpointIndices[1]);
        Assert.AreEqual((byte)3, endpointIndices[2]);
    }

    [TestMethod]
    public void EndpointFindReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x60, 0x0C, 0x00, 0x10];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultiChannelCommandClass.EndpointFindReportCommand.Parse(frame, new List<byte>(), NullLogger.Instance));
    }

    [TestMethod]
    public void EndpointFindReport_Parse_NonZeroReportsToFollow()
    {
        // params[0]=0x02 (2 more reports to follow), params[1]=0x10, params[2]=0x01, params[3]=0x01 (EP1)
        byte[] data = [0x60, 0x0C, 0x02, 0x10, 0x01, 0x01];
        CommandClassFrame frame = new CommandClassFrame(data);
        List<byte> endpointIndices = new List<byte>();

        byte reportsToFollow = MultiChannelCommandClass.EndpointFindReportCommand.Parse(frame, endpointIndices, NullLogger.Instance);

        Assert.AreEqual((byte)2, reportsToFollow);
        Assert.HasCount(1, endpointIndices);
        Assert.AreEqual((byte)1, endpointIndices[0]);
    }

    [TestMethod]
    public void EndpointFind_Create_SpecificWildcardWithValidGeneric()
    {
        // Generic=0x10, Specific=0xFF is valid (match all specific within a generic class)
        MultiChannelCommandClass.EndpointFindCommand command = MultiChannelCommandClass.EndpointFindCommand.Create(0x10, 0xFF);

        Assert.AreEqual((byte)0x10, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[1]);
    }
}
