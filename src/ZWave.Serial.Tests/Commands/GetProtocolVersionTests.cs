using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetProtocolVersionTests : CommandTestBase
{
    private record GetProtocolVersionResponseData(
        ProtocolType ProtocolType,
        byte MajorVersion,
        byte MinorVersion,
        byte RevisionVersion,
        ushort ApplicationFrameworkBuildNumber);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetProtocolVersion,
            new[]
            {
                (Request: GetProtocolVersionRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetProtocolVersionResponse, GetProtocolVersionResponseData>(
            DataFrameType.RES,
            CommandId.GetProtocolVersion,
            new[]
            {
                (
                    CommandParameters: new byte[]
                    {
                        0x00, // Protocol Type: Z-Wave
                        0x07, // Major Version
                        0x1E, // Minor Version
                        0x03, // Revision Version
                        0x00, 0x2A, // Application Framework Build Number (42)
                        // Git Commit hash (16 bytes)
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                    },
                    ExpectedData: new GetProtocolVersionResponseData(
                        ProtocolType: ProtocolType.ZWave,
                        MajorVersion: 7,
                        MinorVersion: 30,
                        RevisionVersion: 3,
                        ApplicationFrameworkBuildNumber: 42)
                ),
            },
            additionalExcludedProperties: ["GitCommitHash"]);

    [TestMethod]
    public void Response_GitCommitHash()
    {
        byte[] commitHash = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00];
        byte[] commandParameters =
        [
            0x00, // Protocol Type
            0x07, // Major
            0x1E, // Minor
            0x03, // Revision
            0x00, 0x01, // Build Number
            .. commitHash,
        ];

        DataFrame dataFrame = DataFrame.Create(DataFrameType.RES, CommandId.GetProtocolVersion, commandParameters);
        GetProtocolVersionResponse response = GetProtocolVersionResponse.Create(dataFrame, new CommandParsingContext(NodeIdType.Short));

        CollectionAssert.AreEqual(commitHash, response.GitCommitHash.ToArray());
    }

    [TestMethod]
    public void Response_GitCommitHashOmitted()
    {
        byte[] commandParameters =
        [
            0x00, // Protocol Type
            0x07, // Major
            0x1E, // Minor
            0x03, // Revision
            0x00, 0x01, // Build Number
        ];

        DataFrame dataFrame = DataFrame.Create(DataFrameType.RES, CommandId.GetProtocolVersion, commandParameters);
        GetProtocolVersionResponse response = GetProtocolVersionResponse.Create(dataFrame, new CommandParsingContext(NodeIdType.Short));

        Assert.AreEqual(0, response.GitCommitHash.Length);
    }
}
