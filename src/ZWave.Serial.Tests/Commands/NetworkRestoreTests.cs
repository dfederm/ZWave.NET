using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class NetworkRestoreTests : CommandTestBase
{
    private record NetworkRestoreResponseData(
        NetworkRestoreSubCommand SubCommand,
        NetworkRestoreStatus Status);

    [TestMethod]
    public void PrepareRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NetworkRestore,
            new[]
            {
                (
                    Request: NetworkRestoreRequest.Prepare(),
                    ExpectedCommandParameters: new byte[] { 0x00 }
                ),
            });

    [TestMethod]
    public void RestoreHomeIdAndNodeIdRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NetworkRestore,
            new[]
            {
                (
                    Request: NetworkRestoreRequest.RestoreHomeIdAndNodeId(homeId: 0x12345678, controllerNodeId: 0x01),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x12, 0x34, 0x56, 0x78, 0x01 }
                ),
            });

    [TestMethod]
    public void FinalizeRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NetworkRestore,
            new[]
            {
                (
                    Request: NetworkRestoreRequest.Finalize(),
                    ExpectedCommandParameters: new byte[] { 0xFF }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<NetworkRestoreResponse, NetworkRestoreResponseData>(
            DataFrameType.RES,
            CommandId.NetworkRestore,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x00 },
                    ExpectedData: new NetworkRestoreResponseData(
                        SubCommand: NetworkRestoreSubCommand.Prepare,
                        Status: NetworkRestoreStatus.OK)
                ),
            });
}
