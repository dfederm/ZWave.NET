using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class ApplicationCommandHandlerTests : CommandTestBase
{
    private record ApplicationCommandHandlerData(
        ReceivedStatus ReceivedStatus,
        ushort NodeId,
        ReadOnlyMemory<byte> Payload,
        RssiMeasurement ReceivedRssi);

    [TestMethod]
    public void Command()
        => TestReceivableCommand<ApplicationCommandHandler, ApplicationCommandHandlerData>(
            DataFrameType.REQ,
            CommandId.ApplicationCommandHandler,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x00, 0x02, 0x04, 0x86, 0x14, 0x5e, 0x02, 0xd5, 0x00 },
                    ExpectedData: new ApplicationCommandHandlerData(
                        ReceivedStatus: 0,
                        NodeId: 2,
                        Payload: new byte[] { 0x86, 0x14, 0x5e, 0x02 },
                        ReceivedRssi: new RssiMeasurement(-43))
                ),
                (
                    CommandParameters: new byte[] { 0x00, 0x2c, 0x0e, 0x32, 0x02, 0x21, 0x64, 0x00, 0x08, 0x83, 0xd6, 0x00, 0x1b, 0x00, 0x08, 0x83, 0xcb, 0xbf, 0x00 },
                    ExpectedData: new ApplicationCommandHandlerData(
                        ReceivedStatus: 0,
                        NodeId: 44,
                        Payload: new byte[] { 0x32, 0x02, 0x21, 0x64, 0x00, 0x08, 0x83, 0xd6, 0x00, 0x1b, 0x00, 0x08, 0x83, 0xcb },
                        ReceivedRssi: new RssiMeasurement(-65))
                ),
            });

    [TestMethod]
    public void Command16Bit()
    {
        // In 16-bit mode, Source NodeID is 2 bytes: 0x01, 0x00 = node 256
        byte[] commandParameters = new byte[] { 0x00, 0x01, 0x00, 0x04, 0x86, 0x14, 0x5e, 0x02, 0xd5 };
        DataFrame dataFrame = DataFrame.Create(DataFrameType.REQ, CommandId.ApplicationCommandHandler, commandParameters);
        ApplicationCommandHandler cmd = ApplicationCommandHandler.Create(dataFrame, new CommandParsingContext(NodeIdType.Long));

        Assert.AreEqual((ushort)256, cmd.NodeId);
        Assert.AreEqual(4, cmd.Payload.Length);
        Assert.AreEqual(new RssiMeasurement(-43), cmd.ReceivedRssi);
    }
}
