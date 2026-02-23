using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class NonceManagementTests : CommandTestBase
{
    private record NonceManagementResponseData(NonceSubCommand SubCommand, byte CommandStatus);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NonceManagement,
            new[]
            {
                (
                    Request: NonceManagementRequest.Create(enable: true),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x01 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<NonceManagementResponse, NonceManagementResponseData>(
            DataFrameType.RES,
            CommandId.NonceManagement,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new NonceManagementResponseData(
                        SubCommand: NonceSubCommand.SetMode,
                        CommandStatus: 0x01)
                ),
            });
}
