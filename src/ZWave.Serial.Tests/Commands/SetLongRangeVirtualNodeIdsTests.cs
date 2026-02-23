using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SetLongRangeVirtualNodeIdsTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SetLongRangeVirtualNodeIds,
            new[]
            {
                (
                    Request: SetLongRangeVirtualNodeIdsRequest.Create(nodeIdBitmask: 0x0F),
                    ExpectedCommandParameters: new byte[] { 0x0F }
                ),
            });
}
