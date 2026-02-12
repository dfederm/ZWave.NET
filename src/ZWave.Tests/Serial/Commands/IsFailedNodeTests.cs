using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class IsFailedNodeTests : CommandTestBase
{
    private record IsFailedNodeResponseData(bool IsFailedNode);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.IsFailedNode,
            new[]
            {
                (
                    Request: IsFailedNodeRequest.Create(nodeId: 5),
                    ExpectedCommandParameters: new byte[] { 0x05 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<IsFailedNodeResponse, IsFailedNodeResponseData>(
            DataFrameType.RES,
            CommandId.IsFailedNode,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new IsFailedNodeResponseData(IsFailedNode: true)
                )
            });
}
