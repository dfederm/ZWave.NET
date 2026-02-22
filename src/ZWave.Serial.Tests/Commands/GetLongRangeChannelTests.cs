using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetLongRangeChannelTests : CommandTestBase
{
    private record GetLongRangeChannelResponseData(
        LongRangeChannel Channel,
        bool SupportsAutoChannelSelection,
        bool AutoChannelSelectionActive);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetLongRangeChannel,
            new[]
            {
                (
                    Request: GetLongRangeChannelRequest.Create(),
                    ExpectedCommandParameters: Array.Empty<byte>()
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetLongRangeChannelResponse, GetLongRangeChannelResponseData>(
            DataFrameType.RES,
            CommandId.GetLongRangeChannel,
            new[]
            {
                // Channel A, no auto-select support
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new GetLongRangeChannelResponseData(
                        Channel: LongRangeChannel.A,
                        SupportsAutoChannelSelection: false,
                        AutoChannelSelectionActive: false)
                ),

                // Channel B, supports auto-select, auto-select active
                (
                    CommandParameters: [0x02, 0x30],
                    ExpectedData: new GetLongRangeChannelResponseData(
                        Channel: LongRangeChannel.B,
                        SupportsAutoChannelSelection: true,
                        AutoChannelSelectionActive: true)
                ),

                // Unsupported channel
                (
                    CommandParameters: [0x00, 0x00],
                    ExpectedData: new GetLongRangeChannelResponseData(
                        Channel: LongRangeChannel.Unsupported,
                        SupportsAutoChannelSelection: false,
                        AutoChannelSelectionActive: false)
                ),

                // Channel A, supports auto-select but not active
                (
                    CommandParameters: [0x01, 0x10],
                    ExpectedData: new GetLongRangeChannelResponseData(
                        Channel: LongRangeChannel.A,
                        SupportsAutoChannelSelection: true,
                        AutoChannelSelectionActive: false)
                ),
            });
}
