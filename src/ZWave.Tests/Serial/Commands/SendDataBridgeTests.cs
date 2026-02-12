using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class SendDataBridgeTests : CommandTestBase
{
    private record SendDataBridgeCallbackData(
        byte SessionId,
        TransmissionStatus TransmissionStatus,
        TransmissionStatusReport? TransmissionStatusReport);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SendDataBridge,
            new[]
            {
                (
                    Request: SendDataBridgeRequest.Create(
                        sourceNodeId: 1,
                        destinationNodeId: 2,
                        data: new byte[] { 0x25, 0x01 },
                        txOptions: TransmissionOptions.ACK,
                        sessionId: 3),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x02, 0x02, 0x25, 0x01, 0x01, 0x03 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<SendDataBridgeCallback, SendDataBridgeCallbackData>(
            DataFrameType.REQ,
            CommandId.SendDataBridge,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x03, 0x00 },
                    ExpectedData: new SendDataBridgeCallbackData(
                        SessionId: 3,
                        TransmissionStatus: TransmissionStatus.Ok,
                        TransmissionStatusReport: null)
                )
            });
}
