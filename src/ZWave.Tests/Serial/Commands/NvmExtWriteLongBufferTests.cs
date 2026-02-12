using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class NvmExtWriteLongBufferTests : CommandTestBase
{
    private record NvmExtWriteLongBufferResponseData(NvmStatus Status);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmExtWriteLongBuffer,
            new[]
            {
                (
                    Request: NvmExtWriteLongBufferRequest.Create(
                        offset: 0x001234,
                        data: new byte[] { 0xAA, 0xBB }),
                    ExpectedCommandParameters: new byte[] { 0x00, 0x12, 0x34, 0x00, 0x02, 0xAA, 0xBB }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<NvmExtWriteLongBufferResponse, NvmExtWriteLongBufferResponseData>(
            DataFrameType.RES,
            CommandId.NvmExtWriteLongBuffer,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00 },
                    ExpectedData: new NvmExtWriteLongBufferResponseData(Status: NvmStatus.Success)
                )
            });
}
