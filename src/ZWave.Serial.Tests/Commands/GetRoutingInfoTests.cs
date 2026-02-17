using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetRoutingInfoTests : CommandTestBase
{
    private record GetRoutingInfoResponseData(HashSet<byte> NeighborNodeIds);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetRoutingInfo,
            new[]
            {
                (
                    Request: GetRoutingInfoRequest.Create(
                        nodeId: 2,
                        removeBadNodes: true,
                        removeNonRepeaters: false),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x01, 0x00 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetRoutingInfoResponse, GetRoutingInfoResponseData>(
            DataFrameType.RES,
            CommandId.GetRoutingInfo,
            new[]
            {
                (
                    CommandParameters: new byte[]
                    {
                        0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00
                    },
                    ExpectedData: new GetRoutingInfoResponseData(
                        NeighborNodeIds: new HashSet<byte> { 2, 3 })
                )
            });
}
