using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class RandomTests : CommandTestBase
{
    private record RandomResponseData(byte RandomNumber);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.Random,
            new[]
            {
                (
                    Request: RandomRequest.Create(),
                    ExpectedCommandParameters: Array.Empty<byte>()
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
                    CommandParameters: new byte[] { 0x42 },
                    ExpectedData: new RandomResponseData(RandomNumber: 0x42)
                )
            });
}
