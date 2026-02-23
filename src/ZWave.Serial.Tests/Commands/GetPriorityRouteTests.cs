using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetPriorityRouteTests : CommandTestBase
{
    private record GetPriorityRouteResponseData(PriorityRouteKind RouteKind, ReadOnlyMemory<byte> Repeaters, PriorityRouteSpeed Speed);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetPriorityRoute,
            new[]
            {
                (
                    Request: GetPriorityRouteRequest.Create(nodeId: 3),
                    ExpectedCommandParameters: new byte[] { 0x03 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetPriorityRouteResponse, GetPriorityRouteResponseData>(
            DataFrameType.RES,
            CommandId.GetPriorityRoute,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x02, 0x03, 0x00, 0x00, 0x03 },
                    ExpectedData: new GetPriorityRouteResponseData(
                        RouteKind: PriorityRouteKind.LastWorkingRoute,
                        Repeaters: new byte[] { 0x02, 0x03, 0x00, 0x00 },
                        Speed: PriorityRouteSpeed.ZWave100k)
                )
            });
}
