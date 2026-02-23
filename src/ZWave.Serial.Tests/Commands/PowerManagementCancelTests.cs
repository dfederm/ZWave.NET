using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class PowerManagementCancelTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.PowerManagementCancel,
            new[]
            {
                (
                    Request: PowerManagementCancelRequest.Create(PowerLockType.Radio),
                    ExpectedCommandParameters: new byte[] { 0x00 }
                ),
            });
}
