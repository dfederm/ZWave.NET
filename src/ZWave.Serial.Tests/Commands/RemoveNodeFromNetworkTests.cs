using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class RemoveNodeFromNetworkTests : CommandTestBase
{
    private record RemoveNodeFromNetworkCallbackData(
        byte SessionId,
        RemoveNodeStatus Status,
        ushort NodeId);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.RemoveNodeFromNetwork,
            new[]
            {
                (
                    Request: RemoveNodeFromNetworkRequest.Create(
                        isHighPower: true,
                        isNetworkWide: true,
                        removeMode: RemoveNodeMode.Any,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0xc1, 0x01 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<RemoveNodeFromNetworkCallback, RemoveNodeFromNetworkCallbackData>(
            DataFrameType.REQ,
            CommandId.RemoveNodeFromNetwork,
            new[]
            {
                (
                    CommandParameters: new byte[]
                    {
                        0x7b, 0x01, 0x02
                    },
                    ExpectedData: new RemoveNodeFromNetworkCallbackData(
                        SessionId: 123,
                        Status: RemoveNodeStatus.NetworkExclusionStarted,
                        NodeId: 2)
                )
            });
}
