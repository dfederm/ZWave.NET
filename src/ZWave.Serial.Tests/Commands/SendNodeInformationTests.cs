using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SendNodeInformationTests : CommandTestBase
{
    private record SendNodeInformationCallbackData(byte SessionId, TransmissionStatus TransmissionStatus);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SendNodeInformation,
            new[]
            {
                (
                    Request: SendNodeInformationRequest.Create(
                        destinationNodeId: 1,
                        txOptions: TransmissionOptions.ACK,
                        sessionId: 2),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x01, 0x02 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<SendNodeInformationCallback, SendNodeInformationCallbackData>(
            DataFrameType.REQ,
            CommandId.SendNodeInformation,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x02, 0x00 },
                    ExpectedData: new SendNodeInformationCallbackData(
                        SessionId: 2,
                        TransmissionStatus: TransmissionStatus.Ok)
                )
            });
}
