using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class StartWatchdogTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.StartWatchdog,
            new[]
            {
                (Request: StartWatchdogRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });
}
