using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class InitiateShutdownTests : CommandTestBase
{
    private record InitiateShutdownResponseData(bool WasAccepted);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.InitiateShutdown,
            new[]
            {
                (Request: InitiateShutdownRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<InitiateShutdownResponse, InitiateShutdownResponseData>(
            DataFrameType.RES,
            CommandId.InitiateShutdown,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new InitiateShutdownResponseData(WasAccepted: true)
                ),
                (
                    CommandParameters: new byte[] { 0x00 },
                    ExpectedData: new InitiateShutdownResponseData(WasAccepted: false)
                ),
            });
}
