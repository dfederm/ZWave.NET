using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetManufacturerInfoTests : CommandTestBase
{
    private record GetManufacturerInfoResponseData(
        ushort HardwareManufacturerId,
        ushort ProtocolManufacturerId,
        ushort HostApiManufacturerId,
        ReadOnlyMemory<byte> ChipInfo);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetManufacturerInfo,
            new[]
            {
                (Request: GetManufacturerInfoRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetManufacturerInfoResponse, GetManufacturerInfoResponseData>(
            DataFrameType.RES,
            CommandId.GetManufacturerInfo,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x02, 0xAB, 0xCD },
                    ExpectedData: new GetManufacturerInfoResponseData(
                        HardwareManufacturerId: 0x0001,
                        ProtocolManufacturerId: 0x0002,
                        HostApiManufacturerId: 0x0003,
                        ChipInfo: new byte[] { 0xAB, 0xCD })
                ),
            });
}
