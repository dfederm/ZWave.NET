using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

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
                        NodeIdType.Short,
                        sessionId: 3),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x01, 0x03 }
                ),
            });

    [TestMethod]
    public void Request16Bit()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.AssignReturnRoute,
            new[]
            {
                (
                    Request: AssignReturnRouteRequest.Create(
                        sourceNodeId: 0x0102,
                        destinationNodeId: 0x0103,
                        NodeIdType.Long,
                        sessionId: 3),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x02, 0x01, 0x03, 0x03 }
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
