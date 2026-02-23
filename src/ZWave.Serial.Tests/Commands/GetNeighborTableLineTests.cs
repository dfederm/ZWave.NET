using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetNeighborTableLineTests : CommandTestBase
{
    private record GetNeighborTableLineResponseData(IReadOnlySet<ushort> NeighborNodeIds);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetNeighborTableLine,
            new[]
            {
                (
                    Request: GetNeighborTableLineRequest.Create(
                        nodeId: 2,
                        removeBadLink: true,
                        removeNonRepeaters: false),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x01, 0x00 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetNeighborTableLineResponse, GetNeighborTableLineResponseData>(
            DataFrameType.RES,
            CommandId.GetNeighborTableLine,
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
                    ExpectedData: new GetNeighborTableLineResponseData(
                        NeighborNodeIds: new HashSet<ushort> { 2, 3 })
                )
            });
}
