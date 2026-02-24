using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class NonceGenerationSetModeTests : CommandTestBase
{
    private record NonceGenerationSetModeResponseData(NonceSubCommand SubCommand, byte CommandStatus);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NonceGenerationSetMode,
            new[]
            {
                (
                    Request: NonceGenerationSetModeRequest.Create(enable: true),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x01 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<NonceGenerationSetModeResponse, NonceGenerationSetModeResponseData>(
            DataFrameType.RES,
            CommandId.NonceGenerationSetMode,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new NonceGenerationSetModeResponseData(
                        SubCommand: NonceSubCommand.SetMode,
                        CommandStatus: 0x01)
                ),
            });
}
