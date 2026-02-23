using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class ApplicationCommandHandlerBridgeTests : CommandTestBase
{
    private record ApplicationCommandHandlerBridgeData(
        ReceivedStatus ReceivedStatus,
        ushort DestinationNodeId,
        ushort SourceNodeId,
        ReadOnlyMemory<byte> Payload,
        RssiMeasurement ReceivedRssi);

    [TestMethod]
    public void Command()
        => TestReceivableCommand<ApplicationCommandHandlerBridge, ApplicationCommandHandlerBridgeData>(
            DataFrameType.REQ,
            CommandId.ApplicationCommandHandlerBridge,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x01, 0x05, 0x03, 0x25, 0x03, 0xFF, 0xD5 },
                    ExpectedData: new ApplicationCommandHandlerBridgeData(
                        ReceivedStatus: 0,
                        DestinationNodeId: 1,
                        SourceNodeId: 5,
                        Payload: new byte[] { 0x25, 0x03, 0xFF },
                        ReceivedRssi: new RssiMeasurement(-43))
                )
            });

    [TestMethod]
    public void Command16Bit()
    {
        // In 16-bit mode, both DestinationNodeId and SourceNodeId are 2 bytes each
        // ReceivedStatus(1) + DestNodeId(2) + SrcNodeId(2) + PayloadLen(1) + Payload(3) + RSSI(1)
        byte[] commandParameters = new byte[] { 0x00, 0x01, 0x00, 0x01, 0x05, 0x03, 0x25, 0x03, 0xFF, 0xD5 };
        DataFrame dataFrame = DataFrame.Create(DataFrameType.REQ, CommandId.ApplicationCommandHandlerBridge, commandParameters);
        ApplicationCommandHandlerBridge response = ApplicationCommandHandlerBridge.Create(dataFrame, new CommandParsingContext(NodeIdType.Long));

        Assert.AreEqual((ushort)256, response.DestinationNodeId);
        Assert.AreEqual((ushort)261, response.SourceNodeId);
        Assert.AreEqual(3, response.Payload.Length);
        Assert.AreEqual(0x25, response.Payload.Span[0]);
        Assert.AreEqual(new RssiMeasurement(-43), response.ReceivedRssi);
    }
}
