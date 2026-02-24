using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class IsNodeFailedTests : CommandTestBase
{
    private record IsNodeFailedResponseData(bool IsNodeFailed);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.IsNodeFailed,
            new[]
            {
                (
                    Request: IsNodeFailedRequest.Create(nodeId: 5, NodeIdType.Short),
                    ExpectedCommandParameters: new byte[] { 0x05 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<IsNodeFailedResponse, IsNodeFailedResponseData>(
            DataFrameType.RES,
            CommandId.IsNodeFailed,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new IsNodeFailedResponseData(IsNodeFailed: true)
                )
            });
}
