using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class RandomTests : CommandTestBase
{
    private record RandomResponseData(bool Success, byte Count, ReadOnlyMemory<byte> RandomBytes);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.Random,
            new[]
            {
                (
                    Request: RandomRequest.Create(count: 5),
                    ExpectedCommandParameters: new byte[] { 0x05 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<RandomResponse, RandomResponseData>(
            DataFrameType.RES,
            CommandId.Random,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 },
                    ExpectedData: new RandomResponseData(
                        Success: true,
                        Count: 5,
                        RandomBytes: new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 })
                )
            });
}
