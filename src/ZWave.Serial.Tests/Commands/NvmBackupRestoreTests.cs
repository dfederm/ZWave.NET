using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class NvmBackupRestoreTests : CommandTestBase
{
    private record NvmBackupRestoreResponseData(
        NvmOperationStatus Status,
        ushort AddressOffsetOrNvmSize,
        ReadOnlyMemory<byte> FirmwareData);

    [TestMethod]
    public void OpenRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmBackupRestore,
            new[]
            {
                (
                    Request: NvmBackupRestoreRequest.Open(),
                    ExpectedCommandParameters: new byte[] { 0x00 }
                ),
            });

    [TestMethod]
    public void ReadRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmBackupRestore,
            new[]
            {
                (
                    Request: NvmBackupRestoreRequest.Read(length: 64, offset: 0x0100),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x40, 0x01, 0x00 }
                ),
            });

    [TestMethod]
    public void WriteRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmBackupRestore,
            new[]
            {
                (
                    Request: NvmBackupRestoreRequest.Write(offset: 0x0200, data: new byte[] { 0xAA, 0xBB }),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x02, 0x02, 0x00, 0xAA, 0xBB }
                ),
            });

    [TestMethod]
    public void CloseRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.NvmBackupRestore,
            new[]
            {
                (
                    Request: NvmBackupRestoreRequest.Close(),
                    ExpectedCommandParameters: new byte[] { 0x03 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<NvmBackupRestoreResponse, NvmBackupRestoreResponseData>(
            DataFrameType.RES,
            CommandId.NvmBackupRestore,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x02, 0x01, 0x00, 0xAB, 0xCD },
                    ExpectedData: new NvmBackupRestoreResponseData(
                        Status: NvmOperationStatus.OK,
                        AddressOffsetOrNvmSize: 0x0100,
                        FirmwareData: new byte[] { 0xAB, 0xCD })
                ),
            });
}
