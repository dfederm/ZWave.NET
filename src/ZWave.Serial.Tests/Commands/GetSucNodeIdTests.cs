using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetSucNodeIdTests : CommandTestBase
{
    private record GetSucNodeIdResponseData(ushort SucNodeId);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetSucNodeId,
            new[]
            {
                (Request: GetSucNodeIdRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetSucNodeIdResponse, GetSucNodeIdResponseData>(
            DataFrameType.RES,
            CommandId.GetSucNodeId,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01 },
                    ExpectedData: new GetSucNodeIdResponseData(SucNodeId: 1)
                )
            });

    [TestMethod]
    public void Response16Bit()
    {
        byte[] commandParameters = new byte[] { 0x01, 0x00 };
        DataFrame dataFrame = DataFrame.Create(DataFrameType.RES, CommandId.GetSucNodeId, commandParameters);
        GetSucNodeIdResponse response = GetSucNodeIdResponse.Create(dataFrame, new CommandParsingContext(NodeIdType.Long));

        Assert.AreEqual((ushort)256, response.SucNodeId);
    }
}
