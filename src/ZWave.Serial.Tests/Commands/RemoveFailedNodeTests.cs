using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class RemoveFailedNodeTests : CommandTestBase
{
    private record RemoveFailedNodeCallbackData(byte SessionId, RemoveFailedNodeStatus Status);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.RemoveFailedNode,
            new[]
            {
                (
                    Request: RemoveFailedNodeRequest.Create(nodeId: 5, sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0x05, 0x01 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<RemoveFailedNodeCallback, RemoveFailedNodeCallbackData>(
            DataFrameType.REQ,
            CommandId.RemoveFailedNode,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new RemoveFailedNodeCallbackData(
                        SessionId: 1,
                        Status: RemoveFailedNodeStatus.NodeRemoved)
                )
            });
}
