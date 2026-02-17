using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetTxTimerTests : CommandTestBase
{
    private record GetTxTimerResponseData(uint TimerTicks);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetTxTimer,
            new[]
            {
                (
                    Request: GetTxTimerRequest.Create(channel: 1),
                    ExpectedCommandParameters: new byte[] { 0x01 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetTxTimerResponse, GetTxTimerResponseData>(
            DataFrameType.RES,
            CommandId.GetTxTimer,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x00, 0x01, 0x00 },
                    ExpectedData: new GetTxTimerResponseData(TimerTicks: 256)
                )
            });
}
