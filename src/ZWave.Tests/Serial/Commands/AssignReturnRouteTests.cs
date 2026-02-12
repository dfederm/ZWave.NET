using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class AssignReturnRouteTests : CommandTestBase
{
    private record AssignReturnRouteCallbackData(byte SessionId, TransmissionStatus Status);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.AssignReturnRoute,
            new[]
            {
                (
                    Request: AssignReturnRouteRequest.Create(
                        sourceNodeId: 2,
                        destinationNodeId: 1,
                        sessionId: 3),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x01, 0x03 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<AssignReturnRouteCallback, AssignReturnRouteCallbackData>(
            DataFrameType.REQ,
            CommandId.AssignReturnRoute,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x03, 0x00 },
                    ExpectedData: new AssignReturnRouteCallbackData(
                        SessionId: 3,
                        Status: TransmissionStatus.Ok)
                )
            });
}
