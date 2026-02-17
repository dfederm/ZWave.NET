using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class ApplicationNodeInformationTests : CommandTestBase
{
    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.ApplicationNodeInformation,
            new[]
            {
                (
                    Request: ApplicationNodeInformationRequest.Create(
                        deviceOptionMask: 0x01,
                        genericType: 0x02,
                        specificType: 0x01,
                        commandClasses: new byte[] { 0x25, 0x26 }),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x02, 0x01, 0x02, 0x25, 0x26 }
                ),
            });
}
