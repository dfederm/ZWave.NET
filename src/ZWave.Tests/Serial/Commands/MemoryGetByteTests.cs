using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class MemoryGetByteTests : CommandTestBase
{
    private record MemoryGetByteResponseData(byte Value);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.MemoryGetByte,
            new[]
            {
                (
                    Request: MemoryGetByteRequest.Create(offset: 0x1234),
                    ExpectedCommandParameters: new byte[] { 0x12, 0x34 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<MemoryGetByteResponse, MemoryGetByteResponseData>(
            DataFrameType.RES,
            CommandId.MemoryGetByte,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0xAB },
                    ExpectedData: new MemoryGetByteResponseData(Value: 0xAB)
                )
            });
}
