using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class NvmExtReadLongBufferTests : CommandTestBase
{
    private record NvmExtReadLongBufferResponseData(ReadOnlyMemory<byte> Data, NvmStatus Status);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmExtReadLongBuffer,
            new[]
            {
                (
                    Request: NvmExtReadLongBufferRequest.Create(offset: 0x001234, length: 5),
                    ExpectedCommandParameters: new byte[] { 0x00, 0x12, 0x34, 0x00, 0x05 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<NvmExtReadLongBufferResponse, NvmExtReadLongBufferResponseData>(
            DataFrameType.RES,
            CommandId.NvmExtReadLongBuffer,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0xAA, 0xBB, 0xCC, 0x00 },
                    ExpectedData: new NvmExtReadLongBufferResponseData(
                        Data: new byte[] { 0xAA, 0xBB, 0xCC },
                        Status: NvmStatus.Success)
                )
            });
}
