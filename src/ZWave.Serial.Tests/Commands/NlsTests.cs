using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class NlsTests : CommandTestBase
{
    private record EnableNodeNlsResponseData(byte CommandStatus);

    private record GetNodeNlsStateResponseData(bool IsNlsSupported, bool IsNlsEnabled);

    private record GetNlsNodesResponseData(
        bool MoreNodes,
        byte StartOffset,
        ReadOnlyMemory<byte> NodeList);

    [TestMethod]
    public void EnableRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.EnableNodeNls,
            new[]
            {
                (
                    Request: ZWave.Serial.Commands.EnableNodeNlsRequest.Create(nodeId: 5, NodeIdType.Short),
                    ExpectedCommandParameters: new byte[] { 0x05 }
                ),
            });

    [TestMethod]
    public void EnableResponse()
        => TestReceivableCommand<EnableNodeNlsResponse, EnableNodeNlsResponseData>(
            DataFrameType.RES,
            CommandId.EnableNodeNls,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new EnableNodeNlsResponseData(CommandStatus: 0x01)
                ),
            });

    [TestMethod]
    public void GetStateRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetNodeNlsState,
            new[]
            {
                (
                    Request: ZWave.Serial.Commands.GetNodeNlsStateRequest.Create(nodeId: 5, NodeIdType.Short),
                    ExpectedCommandParameters: new byte[] { 0x05 }
                ),
            });

    [TestMethod]
    public void GetStateResponse()
        => TestReceivableCommand<GetNodeNlsStateResponse, GetNodeNlsStateResponseData>(
            DataFrameType.RES,
            CommandId.GetNodeNlsState,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x00 },
                    ExpectedData: new GetNodeNlsStateResponseData(IsNlsSupported: true, IsNlsEnabled: false)
                ),
            });

    [TestMethod]
    public void GetNodesRequest()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetNlsNodes,
            new[]
            {
                (
                    Request: ZWave.Serial.Commands.GetNlsNodesRequest.Create(startOffset: 0),
                    ExpectedCommandParameters: new byte[] { 0x00 }
                ),
            });

    [TestMethod]
    public void GetNodesResponse()
        => TestReceivableCommand<GetNlsNodesResponse, GetNlsNodesResponseData>(
            DataFrameType.RES,
            CommandId.GetNlsNodes,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x00, 0x02, 0xFF, 0x00 },
                    ExpectedData: new GetNlsNodesResponseData(
                        MoreNodes: true,
                        StartOffset: 0,
                        NodeList: new byte[] { 0xFF, 0x00 })
                ),
            });
}
