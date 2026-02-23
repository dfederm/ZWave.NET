using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SendNopTests : CommandTestBase
{
    private record SendNopCallbackData(
        byte SessionId,
        TransmissionStatus TransmissionStatus,
        TransmissionStatusReport? TransmissionStatusReport);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SendNop,
            new[]
            {
                (
                    Request: SendNopRequest.Create(
                        nodeId: 5,
                        txOptions: TransmissionOptions.ACK | TransmissionOptions.AutoRoute | TransmissionOptions.Explore,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0x05, 0x25, 0x01 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<SendNopCallback, SendNopCallbackData>(
            DataFrameType.REQ,
            CommandId.SendNop,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x00 },
                    ExpectedData: new SendNopCallbackData(
                        SessionId: 1,
                        TransmissionStatus: TransmissionStatus.Ok,
                        TransmissionStatusReport: null)
                ),
            },
            additionalExcludedProperties: new[] { "AckRepeaterRssi" });
}
