using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class EndNodeSendDataTests : CommandTestBase
{
    private record EndNodeSendDataCallbackData(
        byte SessionId,
        TransmissionStatus TransmissionStatus,
        TransmissionStatusReport? TransmissionStatusReport);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.EndNodeSendData,
            new[]
            {
                (
                    Request: EndNodeSendDataRequest.Create(
                        destinationNodeId: 5,
                        NodeIdType.Short,
                        data: new byte[] { 0x62, 0x01 },
                        txOptions: TransmissionOptions.ACK | TransmissionOptions.AutoRoute | TransmissionOptions.Explore,
                        txSecurityOptions: TxSecurityOptions.None,
                        securityKey: SecurityKey.None,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[]
                    {
                        0x05,       // destinationNodeId
                        0x02,       // data length
                        0x62, 0x01, // data
                        0x25,       // txOptions (ACK | AutoRoute | Explore)
                        0x00,       // txSecurityOptions (None)
                        0x00,       // securityKey (None)
                        0x00,       // TxOptions2 (reserved)
                        0x01,       // sessionId
                    }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<EndNodeSendDataCallback, EndNodeSendDataCallbackData>(
            DataFrameType.REQ,
            CommandId.EndNodeSendData,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x00 },
                    ExpectedData: new EndNodeSendDataCallbackData(
                        SessionId: 1,
                        TransmissionStatus: TransmissionStatus.Ok,
                        TransmissionStatusReport: null)
                ),
            },
            additionalExcludedProperties: new[] { "AckRepeaterRssi" });
}
