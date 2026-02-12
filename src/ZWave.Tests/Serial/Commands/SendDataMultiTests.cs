using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class SendDataMultiTests : CommandTestBase
{
    private record SendDataMultiCallbackData(byte SessionId, TransmissionStatus TransmissionStatus);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SendDataMulti,
            new[]
            {
                (
                    Request: SendDataMultiRequest.Create(
                        nodeList: new byte[] { 0x02, 0x03 },
                        data: new byte[] { 0x25, 0x01 },
                        txOptions: TransmissionOptions.ACK | TransmissionOptions.AutoRoute,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x02, 0x03, 0x02, 0x25, 0x01, 0x05, 0x01 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<SendDataMultiCallback, SendDataMultiCallbackData>(
            DataFrameType.REQ,
            CommandId.SendDataMulti,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x00 },
                    ExpectedData: new SendDataMultiCallbackData(
                        SessionId: 1,
                        TransmissionStatus: TransmissionStatus.Ok)
                )
            });
}
