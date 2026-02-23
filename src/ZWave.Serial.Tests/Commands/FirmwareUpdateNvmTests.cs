using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class FirmwareUpdateNvmTests : CommandTestBase
{
    private record FirmwareUpdateNvmResponseData(
        FirmwareUpdateNvmSubCommand SubCommand,
        FirmwareUpdateNvmStatus Status);

    [TestMethod]
    public void PrepareRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.FirmwareUpdateNvm,
            new[]
            {
                (
                    Request: FirmwareUpdateNvmRequest.Prepare(),
                    ExpectedCommandParameters: new byte[] { 0x00 }
                ),
            });

    [TestMethod]
    public void WriteChunkRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.FirmwareUpdateNvm,
            new[]
            {
                (
                    Request: FirmwareUpdateNvmRequest.WriteChunk(offset: 0x00001000, data: new byte[] { 0xAA, 0xBB }),
                    ExpectedCommandParameters: new byte[]
                    {
                        0x01,                   // WriteChunk sub-command
                        0x00, 0x00, 0x10, 0x00, // offset big-endian
                        0x00, 0x02,             // data length big-endian
                        0xAA, 0xBB,             // data
                    }
                ),
            });

    [TestMethod]
    public void PerformUpdateRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.FirmwareUpdateNvm,
            new[]
            {
                (
                    Request: FirmwareUpdateNvmRequest.PerformUpdate(FirmwareUpdateTarget.Firmware),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x01 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<FirmwareUpdateNvmResponse, FirmwareUpdateNvmResponseData>(
            DataFrameType.RES,
            CommandId.FirmwareUpdateNvm,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x00 },
                    ExpectedData: new FirmwareUpdateNvmResponseData(
                        SubCommand: FirmwareUpdateNvmSubCommand.Prepare,
                        Status: FirmwareUpdateNvmStatus.OK)
                ),
            });
}
