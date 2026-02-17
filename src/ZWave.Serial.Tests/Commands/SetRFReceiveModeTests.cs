using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SetRFReceiveModeTests : CommandTestBase
{
    private record SetRFReceiveModeResponseData(bool Success);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SetRFReceiveMode,
            new[]
            {
                (
                    Request: SetRFReceiveModeRequest.Create(enabled: true),
                    ExpectedCommandParameters: new byte[] { 0x01 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<SetRFReceiveModeResponse, SetRFReceiveModeResponseData>(
            DataFrameType.RES,
            CommandId.SetRFReceiveMode,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new SetRFReceiveModeResponseData(Success: true)
                )
            });
}
