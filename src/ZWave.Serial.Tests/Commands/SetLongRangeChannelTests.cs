using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SetLongRangeChannelTests : CommandTestBase
{
    private record SetLongRangeChannelResponseData(
        bool Success);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SetLongRangeChannel,
            new[]
            {
                (
                    Request: SetLongRangeChannelRequest.Create(LongRangeChannel.A),
                    ExpectedCommandParameters: new byte[] { 0x01 }
                ),

                (
                    Request: SetLongRangeChannelRequest.Create(LongRangeChannel.B),
                    ExpectedCommandParameters: [0x02]
                ),

                (
                    Request: SetLongRangeChannelRequest.Create(LongRangeChannel.Auto),
                    ExpectedCommandParameters: [0xFF]
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<SetLongRangeChannelResponse, SetLongRangeChannelResponseData>(
            DataFrameType.RES,
            CommandId.SetLongRangeChannel,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new SetLongRangeChannelResponseData(
                        Success: true)
                ),
                (
                    CommandParameters: [0x00],
                    ExpectedData: new SetLongRangeChannelResponseData(
                        Success: false)
                ),
            });
}
