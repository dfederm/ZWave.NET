using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SetLongRangeShadowNodeIdsTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SetLongRangeShadowNodeIds,
            new[]
            {
                (
                    Request: SetLongRangeShadowNodeIdsRequest.Create(nodeIdBitmask: 0x0F),
                    ExpectedCommandParameters: new byte[] { 0x0F }
                ),
            });
}
