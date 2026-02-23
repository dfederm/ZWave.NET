using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class PowerManagementStayAwakeTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.PowerManagementStayAwake,
            new[]
            {
                (
                    Request: PowerManagementStayAwakeRequest.Create(
                        powerLockType: PowerLockType.Radio,
                        powerLockTimeoutMs: 5000,
                        wakeUpTimerTimeoutMs: 1000),
                    ExpectedCommandParameters: new byte[]
                    {
                        0x00,                   // PowerLockType.Radio
                        0x00, 0x00, 0x13, 0x88, // 5000 big-endian
                        0x00, 0x00, 0x03, 0xE8, // 1000 big-endian
                    }
                ),
            });
}
