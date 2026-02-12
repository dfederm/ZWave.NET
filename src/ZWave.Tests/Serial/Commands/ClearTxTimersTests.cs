using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class ClearTxTimersTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.ClearTxTimers,
            new[]
            {
                (Request: ClearTxTimersRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });
}
