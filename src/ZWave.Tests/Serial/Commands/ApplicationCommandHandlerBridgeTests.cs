using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class ApplicationCommandHandlerBridgeTests : CommandTestBase
{
    private record ApplicationCommandHandlerBridgeData(
        ReceivedStatus ReceivedStatus,
        byte DestinationNodeId,
        byte SourceNodeId,
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
}
