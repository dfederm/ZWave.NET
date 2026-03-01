using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultiChannelCommandClassTests
{
    [TestMethod]
    public void CommandEncapsulation_Create_HasCorrectFormat()
    {
        // Encapsulate a Binary Switch Get (0x25, 0x02) from EP0 to EP1
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x02);
        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(0, 1, innerFrame);

        Assert.AreEqual(CommandClassId.MultiChannel, encapFrame.CommandClassId);
        Assert.AreEqual((byte)MultiChannelCommand.CommandEncapsulation, encapFrame.CommandId);
        ReadOnlySpan<byte> parameters = encapFrame.CommandParameters.Span;
        Assert.AreEqual((byte)0x00, parameters[0]); // source EP 0
        Assert.AreEqual((byte)0x01, parameters[1]); // destination EP 1, no bit address
        Assert.AreEqual((byte)0x25, parameters[2]); // Binary Switch CC
        Assert.AreEqual((byte)0x02, parameters[3]); // Get command
    }

    [TestMethod]
    public void CommandEncapsulation_CreateBitAddress_HasCorrectFormat()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01);
        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(0, (ReadOnlySpan<byte>)[1, 2, 3], innerFrame);

        ReadOnlySpan<byte> parameters = encapFrame.CommandParameters.Span;
        Assert.AreEqual((byte)0x00, parameters[0]); // source EP 0
        Assert.AreEqual((byte)0x87, parameters[1]); // bit address flag | 0x07 (EPs 1, 2, 3)
    }

    [TestMethod]
    public void CommandEncapsulation_CreateBitAddress_RejectsEndpointZero()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01);
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.CreateEncapsulation(0, (ReadOnlySpan<byte>)[0, 1], innerFrame));
    }

    [TestMethod]
    public void CommandEncapsulation_CreateBitAddress_RejectsEndpointAbove7()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01);
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.CreateEncapsulation(0, (ReadOnlySpan<byte>)[1, 8], innerFrame));
    }

    [TestMethod]
    public void CommandEncapsulation_Create_RejectsBothZero()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x02);
        Assert.Throws<ArgumentException>(() => MultiChannelCommandClass.CreateEncapsulation(0, 0, innerFrame));
    }

    [TestMethod]
    public void CommandEncapsulation_Create_RejectsSourceAbove127()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x02);
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.CreateEncapsulation(128, 1, innerFrame));
    }

    [TestMethod]
    public void CommandEncapsulation_Parse_SingleDestination()
    {
        // Source EP=2, Destination EP=1 (no bit address), encapsulated: Binary Switch Report (0x25, 0x03, 0xFF)
        byte[] data = [0x60, 0x0D, 0x02, 0x01, 0x25, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        MultiChannelCommandEncapsulation encap = MultiChannelCommandClass.ParseEncapsulation(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, encap.SourceEndpoint);
        Assert.IsFalse(encap.IsBitAddress);
        Assert.AreEqual((byte)1, encap.Destination);
        Assert.AreEqual(CommandClassId.BinarySwitch, encap.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x03, encap.EncapsulatedFrame.CommandId);
    }

    [TestMethod]
    public void CommandEncapsulation_Parse_BitAddressDestination()
    {
        // Source EP=0, Bit Address=1, Destination=0x03 (EP1+EP2), encapsulated: Basic Set (0x20, 0x01, 0xFF)
        byte[] data = [0x60, 0x0D, 0x00, 0x83, 0x20, 0x01, 0xFF];
        CommandClassFrame frame = new(data);

        MultiChannelCommandEncapsulation encap = MultiChannelCommandClass.ParseEncapsulation(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, encap.SourceEndpoint);
        Assert.IsTrue(encap.IsBitAddress);
        Assert.AreEqual((byte)3, encap.Destination);
    }

    [TestMethod]
    public void CommandEncapsulation_Parse_TooShort_Throws()
    {
        byte[] data = [0x60, 0x0D, 0x01, 0x02, 0x25];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultiChannelCommandClass.ParseEncapsulation(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CommandEncapsulation_Create_RejectsDestinationAbove127()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x02);
        Assert.Throws<ArgumentOutOfRangeException>(() => MultiChannelCommandClass.CreateEncapsulation(0, 128, innerFrame));
    }

    [TestMethod]
    public void CommandEncapsulation_CreateBitAddress_SingleEndpoint()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01);
        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(0, (ReadOnlySpan<byte>)[5], innerFrame);

        ReadOnlySpan<byte> parameters = encapFrame.CommandParameters.Span;
        Assert.AreEqual((byte)0x00, parameters[0]); // source EP 0
        Assert.AreEqual((byte)0x90, parameters[1]); // bit address flag | 0x10 (EP5 = bit 4)
    }

    [TestMethod]
    public void CommandEncapsulation_CreateBitAddress_AllEndpoints()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01);
        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(0, (ReadOnlySpan<byte>)[1, 2, 3, 4, 5, 6, 7], innerFrame);

        ReadOnlySpan<byte> parameters = encapFrame.CommandParameters.Span;
        Assert.AreEqual((byte)0x00, parameters[0]); // source EP 0
        Assert.AreEqual((byte)0xFF, parameters[1]); // bit address flag | 0x7F (all 7 EPs)
    }

    [TestMethod]
    public void CommandEncapsulation_CreateBitAddress_EmptyEndpoints_RejectsBothZero()
    {
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x01);
        Assert.Throws<ArgumentException>(() => MultiChannelCommandClass.CreateEncapsulation(0, (ReadOnlySpan<byte>)[], innerFrame));
    }

    [TestMethod]
    public void CommandEncapsulation_Parse_SourceEndpointReservedBitMasked()
    {
        // Source EP byte = 0x82 (reserved bit set + EP 2)
        byte[] data = [0x60, 0x0D, 0x82, 0x01, 0x25, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        MultiChannelCommandEncapsulation encap = MultiChannelCommandClass.ParseEncapsulation(frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, encap.SourceEndpoint);
    }

    [TestMethod]
    public void CommandEncapsulation_RoundTrip_PreservesInnerFrame()
    {
        // Create a command with parameters
        byte[] innerParams = [0xFF, 0x01, 0x00];
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x03, innerParams);

        // Encapsulate from Root Device (EP0) to EP3
        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(0, 3, innerFrame);

        // Parse back and verify the inner frame is preserved
        MultiChannelCommandEncapsulation encap = MultiChannelCommandClass.ParseEncapsulation(encapFrame, NullLogger.Instance);

        Assert.AreEqual((byte)0, encap.SourceEndpoint);
        Assert.AreEqual((byte)3, encap.Destination);
        Assert.IsFalse(encap.IsBitAddress);
        Assert.AreEqual(CommandClassId.BinarySwitch, encap.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x03, encap.EncapsulatedFrame.CommandId);
        Assert.AreEqual(innerParams.Length, encap.EncapsulatedFrame.CommandParameters.Length);
        Assert.IsTrue(innerParams.AsSpan().SequenceEqual(encap.EncapsulatedFrame.CommandParameters.Span));
    }

    [TestMethod]
    public void CommandEncapsulation_RoundTrip_ResponseSwapsEndpoints()
    {
        // Simulate the spec-defined request/response flow (CC:0060.03.0D.11.009):
        // Request: Source=0 (controller), Destination=2 (remote endpoint)
        CommandClassFrame getCommand = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x02);
        CommandClassFrame requestFrame = MultiChannelCommandClass.CreateEncapsulation(0, 2, getCommand);

        // Parse the request to verify endpoint assignment
        MultiChannelCommandEncapsulation requestEncap = MultiChannelCommandClass.ParseEncapsulation(requestFrame, NullLogger.Instance);
        Assert.AreEqual((byte)0, requestEncap.SourceEndpoint);
        Assert.AreEqual((byte)2, requestEncap.Destination);

        // Simulate the response: Source=2 (remote endpoint), Destination=0 (controller)
        // Per CC:0060.03.0D.11.009, response swaps source and destination.
        CommandClassFrame reportCommand = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x03, [0xFF]);
        CommandClassFrame responseFrame = MultiChannelCommandClass.CreateEncapsulation(2, 0, reportCommand);

        // Parse the response — SourceEndpoint tells us which endpoint sent it
        MultiChannelCommandEncapsulation responseEncap = MultiChannelCommandClass.ParseEncapsulation(responseFrame, NullLogger.Instance);
        Assert.AreEqual((byte)2, responseEncap.SourceEndpoint);
        Assert.AreEqual((byte)0, responseEncap.Destination);
        Assert.AreEqual(CommandClassId.BinarySwitch, responseEncap.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x03, responseEncap.EncapsulatedFrame.CommandId);
    }

    [TestMethod]
    public void CommandEncapsulation_Create_MaxEndpoint127()
    {
        // Per spec CC:0060.03.0D.11.006: Source End Point MUST be in range 0..127
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x02);
        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(0, 127, innerFrame);

        MultiChannelCommandEncapsulation encap = MultiChannelCommandClass.ParseEncapsulation(encapFrame, NullLogger.Instance);
        Assert.AreEqual((byte)127, encap.Destination);
    }

    [TestMethod]
    public void CommandEncapsulation_Create_EndpointToRootDevice()
    {
        // Endpoint-to-Root: Source=1, Destination=0 (per spec CC:0060.03.0D.11.008,
        // Source EP MUST be different from 0 if Destination EP is 0)
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.BinarySwitch, 0x03, [0xFF]);
        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(1, 0, innerFrame);

        MultiChannelCommandEncapsulation encap = MultiChannelCommandClass.ParseEncapsulation(encapFrame, NullLogger.Instance);
        Assert.AreEqual((byte)1, encap.SourceEndpoint);
        Assert.AreEqual((byte)0, encap.Destination);
    }

    [TestMethod]
    public void CommandEncapsulation_RoundTrip_WithMultipleParameters()
    {
        // Use a larger payload to verify all bytes are preserved through encapsulation
        byte[] parameters = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        CommandClassFrame innerFrame = CommandClassFrame.Create(CommandClassId.MultilevelSensor, 0x05, parameters);

        CommandClassFrame encapFrame = MultiChannelCommandClass.CreateEncapsulation(0, 5, innerFrame);
        MultiChannelCommandEncapsulation encap = MultiChannelCommandClass.ParseEncapsulation(encapFrame, NullLogger.Instance);

        Assert.AreEqual(CommandClassId.MultilevelSensor, encap.EncapsulatedFrame.CommandClassId);
        Assert.AreEqual((byte)0x05, encap.EncapsulatedFrame.CommandId);
        Assert.IsTrue(parameters.AsSpan().SequenceEqual(encap.EncapsulatedFrame.CommandParameters.Span));
    }
}
