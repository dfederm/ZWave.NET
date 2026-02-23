using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetRandomWordTests : CommandTestBase
{
    private record GetRandomWordResponseData(bool Success, byte Count, ReadOnlyMemory<byte> RandomBytes);

    [TestMethod]
    public void RequestDefault()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetRandomWord,
            new[]
            {
                (Request: GetRandomWordRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void RequestWithCount()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetRandomWord,
            new[]
            {
                (Request: GetRandomWordRequest.Create(count: 5), ExpectedCommandParameters: new byte[] { 0x05 }),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetRandomWordResponse, GetRandomWordResponseData>(
            DataFrameType.RES,
            CommandId.GetRandomWord,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 },
                    ExpectedData: new GetRandomWordResponseData(
                        Success: true,
                        Count: 5,
                        RandomBytes: new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 })
                )
            });
}
