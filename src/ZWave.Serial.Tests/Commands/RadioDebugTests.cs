using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class RadioDebugTests : CommandTestBase
{
    private record RadioDebugEnableResponseData(byte CommandStatus);

    private record RadioDebugStatusResponseData(bool IsEnabled, DebugInterfaceProtocol? DebugInterfaceProtocol);

    [TestMethod]
    public void EnableRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.RadioDebugEnable,
            new[]
            {
                (
                    Request: RadioDebugEnableRequest.Create(enable: true, debugInterfaceProtocol: DebugInterfaceProtocol.Pti),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x00, 0x00 }
                ),
            });

    [TestMethod]
    public void EnableResponse()
        => TestReceivableCommand<RadioDebugEnableResponse, RadioDebugEnableResponseData>(
            DataFrameType.RES,
            CommandId.RadioDebugEnable,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new RadioDebugEnableResponseData(CommandStatus: 0x01)
                ),
            });

    [TestMethod]
    public void StatusRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.RadioDebugStatus,
            new[]
            {
                (Request: RadioDebugStatusRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void StatusResponse()
        => TestReceivableCommand<RadioDebugStatusResponse, RadioDebugStatusResponseData>(
            DataFrameType.RES,
            CommandId.RadioDebugStatus,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x00, 0x00 },
                    ExpectedData: new RadioDebugStatusResponseData(IsEnabled: true, DebugInterfaceProtocol: DebugInterfaceProtocol.Pti)
                ),
                (
                    CommandParameters: new byte[] { 0x00 },
                    ExpectedData: new RadioDebugStatusResponseData(IsEnabled: false, DebugInterfaceProtocol: null)
                ),
            });

    [TestMethod]
    public void GetProtocolListRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.RadioDebugGetProtocolList,
            new[]
            {
                (Request: RadioDebugGetProtocolListRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void GetProtocolListResponse()
    {
        DataFrame dataFrame = DataFrame.Create(
            DataFrameType.RES,
            CommandId.RadioDebugGetProtocolList,
            new byte[] { 0x02, 0x02, 0x00, 0x00, 0x00, 0x01 });
        RadioDebugGetProtocolListResponse response = RadioDebugGetProtocolListResponse.Create(dataFrame);

        Assert.AreEqual((byte)0x02, response.RadioDebugCommandsVersion);
        Assert.AreEqual(2, response.ProtocolCount);
        Assert.AreEqual(DebugInterfaceProtocol.Pti, response.GetProtocol(0));
        Assert.AreEqual((DebugInterfaceProtocol)0x0001, response.GetProtocol(1));
    }
}
