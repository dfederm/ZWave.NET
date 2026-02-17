using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SoftResetTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SoftReset,
            new[]
            {
                (Request: SoftResetRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });
}
