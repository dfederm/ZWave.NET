using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class RFPowerLevelSetTests : CommandTestBase
{
    private record RFPowerLevelSetResponseData(byte PowerLevel);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.RFPowerLevelSet,
            new[]
            {
                (
                    Request: RFPowerLevelSetRequest.Create(powerLevel: 3),
                    ExpectedCommandParameters: new byte[] { 0x03 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<RFPowerLevelSetResponse, RFPowerLevelSetResponseData>(
            DataFrameType.RES,
            CommandId.RFPowerLevelSet,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x03 },
                    ExpectedData: new RFPowerLevelSetResponseData(PowerLevel: 3)
                )
            });
}
