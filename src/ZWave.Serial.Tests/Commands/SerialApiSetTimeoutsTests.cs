using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SerialApiSetTimeoutsTests : CommandTestBase
{
    private record SerialApiSetTimeoutsResponseData(byte PreviousRxAckTimeout, byte PreviousRxByteTimeout);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SerialApiSetTimeouts,
            new[]
            {
                (
                    Request: SerialApiSetTimeoutsRequest.Create(rxAckTimeout: 10, rxByteTimeout: 20),
                    ExpectedCommandParameters: new byte[] { 0x0a, 0x14 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<SerialApiSetTimeoutsResponse, SerialApiSetTimeoutsResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetTimeouts,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x05, 0x0a },
                    ExpectedData: new SerialApiSetTimeoutsResponseData(
                        PreviousRxAckTimeout: 5,
                        PreviousRxByteTimeout: 10)
                )
            });
}
