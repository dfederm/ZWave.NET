using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class MemoryPutByteTests : CommandTestBase
{
    private record MemoryPutByteResponseData(bool Success);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.MemoryPutByte,
            new[]
            {
                (
                    Request: MemoryPutByteRequest.Create(offset: 0x1234, value: 0xAB),
                    ExpectedCommandParameters: new byte[] { 0x12, 0x34, 0xAB }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<MemoryPutByteResponse, MemoryPutByteResponseData>(
            DataFrameType.RES,
            CommandId.MemoryPutByte,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new MemoryPutByteResponseData(Success: true)
                )
            });
}
