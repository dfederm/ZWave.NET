using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetRandomWordTests : CommandTestBase
{
    private record GetRandomWordResponseData(byte RandomByte1, byte RandomByte2);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetRandomWord,
            new[]
            {
                (Request: GetRandomWordRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetRandomWordResponse, GetRandomWordResponseData>(
            DataFrameType.RES,
            CommandId.GetRandomWord,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0xAB, 0xCD },
                    ExpectedData: new GetRandomWordResponseData(
                        RandomByte1: 0xAB,
                        RandomByte2: 0xCD)
                )
            });
}
