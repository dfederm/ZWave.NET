using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class FirmwareUpdateTests : CommandTestBase
{
    private record FirmwareUpdateResponseData(
        FirmwareUpdateSubCommand SubCommand,
        FirmwareUpdateStatus Status);

    [TestMethod]
    public void PrepareRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.FirmwareUpdate,
            new[]
            {
                (
                    Request: FirmwareUpdateRequest.Prepare(),
                    ExpectedCommandParameters: new byte[] { 0x00 }
                ),
            });

    [TestMethod]
    public void WriteChunkRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.FirmwareUpdate,
            new[]
            {
                (
                    Request: FirmwareUpdateRequest.WriteChunk(offset: 0x00001000, data: new byte[] { 0xAA, 0xBB }),
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
            CommandId.FirmwareUpdate,
            new[]
            {
                (
                    Request: FirmwareUpdateRequest.PerformUpdate(FirmwareUpdateTarget.Firmware),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x01 }
                ),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<FirmwareUpdateResponse, FirmwareUpdateResponseData>(
            DataFrameType.RES,
            CommandId.FirmwareUpdate,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x00 },
                    ExpectedData: new FirmwareUpdateResponseData(
                        SubCommand: FirmwareUpdateSubCommand.Prepare,
                        Status: FirmwareUpdateStatus.OK)
                ),
            });
}
