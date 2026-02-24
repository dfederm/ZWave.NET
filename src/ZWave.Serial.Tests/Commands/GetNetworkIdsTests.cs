using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetNetworkIdsTests : CommandTestBase
{
    private record GetNetworkIdsResponseData(uint HomeId, ushort NodeId);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetNetworkIds,
            new[]
            {
                (Request: GetNetworkIdsRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetNetworkIdsResponse, GetNetworkIdsResponseData>(
            DataFrameType.RES,
            CommandId.GetNetworkIds,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x2f, 0x3b, 0xd8, 0x2a, 0x01 },
                    ExpectedData: new GetNetworkIdsResponseData(HomeId: 792451114u, NodeId: 1)
                )
            });

    [TestMethod]
    public void Response16Bit()
    {
        // In 16-bit mode, NodeID is 2 bytes: 0x01, 0x00 = node 256
        byte[] commandParameters = new byte[] { 0x2f, 0x3b, 0xd8, 0x2a, 0x01, 0x00 };
        DataFrame dataFrame = DataFrame.Create(DataFrameType.RES, CommandId.GetNetworkIds, commandParameters);
        GetNetworkIdsResponse response = GetNetworkIdsResponse.Create(dataFrame, new CommandParsingContext(NodeIdType.Long));

        Assert.AreEqual(792451114u, response.HomeId);
        Assert.AreEqual((ushort)256, response.NodeId);
    }
}
