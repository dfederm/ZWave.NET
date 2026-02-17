using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetBackgroundRSSITests : CommandTestBase
{
    private record GetBackgroundRSSIResponseData(
        RssiMeasurement RssiChannel0,
        RssiMeasurement RssiChannel1,
        RssiMeasurement? RssiChannel2);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetBackgroundRSSI,
            new[]
            {
                (Request: GetBackgroundRSSIRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetBackgroundRSSIResponse, GetBackgroundRSSIResponseData>(
            DataFrameType.RES,
            CommandId.GetBackgroundRSSI,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0xD5, 0xD8, 0xDA },
                    ExpectedData: new GetBackgroundRSSIResponseData(
                        RssiChannel0: new RssiMeasurement(-43),
                        RssiChannel1: new RssiMeasurement(-40),
                        RssiChannel2: new RssiMeasurement(-38))
                )
            });
}
