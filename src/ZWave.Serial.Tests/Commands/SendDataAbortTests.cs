using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SendDataAbortTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SendDataAbort,
            new[]
            {
                (Request: SendDataAbortRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });
}
