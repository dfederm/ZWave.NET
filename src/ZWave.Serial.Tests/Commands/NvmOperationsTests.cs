using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class NvmOperationsTests : CommandTestBase
{
    private record NvmOperationsResponseData(
        NvmOperationStatus Status,
        ushort AddressOffsetOrNvmSize,
        ReadOnlyMemory<byte> FirmwareData);

    [TestMethod]
    public void OpenRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmOperations,
            new[]
            {
                (
                    Request: NvmOperationsRequest.Open(),
                    ExpectedCommandParameters: new byte[] { 0x00 }
                ),
            });

    [TestMethod]
    public void ReadRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmOperations,
            new[]
            {
                (
                    Request: NvmOperationsRequest.Read(length: 64, offset: 0x0100),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x40, 0x01, 0x00 }
                ),
            });

    [TestMethod]
    public void WriteRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmOperations,
            new[]
            {
                (
                    Request: NvmOperationsRequest.Write(offset: 0x0200, data: new byte[] { 0xAA, 0xBB }),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x02, 0x02, 0x00, 0xAA, 0xBB }
                ),
            });

    [TestMethod]
    public void CloseRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmOperations,
            new[]
            {
                (
                    Request: NvmOperationsRequest.Close(),
                    ExpectedCommandParameters: new byte[] { 0x03 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<NvmOperationsResponse, NvmOperationsResponseData>(
            DataFrameType.RES,
            CommandId.NvmOperations,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x02, 0x01, 0x00, 0xAB, 0xCD },
                    ExpectedData: new NvmOperationsResponseData(
                        Status: NvmOperationStatus.OK,
                        AddressOffsetOrNvmSize: 0x0100,
                        FirmwareData: new byte[] { 0xAB, 0xCD })
                ),
            });
}
