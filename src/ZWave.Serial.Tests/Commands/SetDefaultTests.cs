using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SetDefaultTests : CommandTestBase
{
    private record SetDefaultCallbackData(byte SessionId);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SetDefault,
            new[]
            {
                (
                    Request: SetDefaultRequest.Create(sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0x01 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<SetDefaultCallback, SetDefaultCallbackData>(
            DataFrameType.REQ,
            CommandId.SetDefault,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new SetDefaultCallbackData(SessionId: 1)
                )
            });
}
