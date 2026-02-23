using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class StopWatchdogTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.StopWatchdog,
            new[]
            {
                (Request: StopWatchdogRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });
}
