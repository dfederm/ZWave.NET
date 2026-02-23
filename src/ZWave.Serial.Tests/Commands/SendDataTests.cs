using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SendDataTests : CommandTestBase
{
    private record SendDataCallbackData(
        byte SessionId,
        TransmissionStatus TransmissionStatus,
        TransmissionStatusReportData? TransmissionStatusReport);

    private record TransmissionStatusReportData(
        TimeSpan? TransitTime,
        byte? NumRepeaters,
        RssiMeasurement? AckRssi,
        byte? AckChannelNumber,
        byte? TransmitChannelNumber,
        byte? RouteSchemeState,
        ReadOnlyMemory<byte> LastRouteRepeaters,
        bool? Beam1000ms,
        bool? Beam250ms,
        TransmissionStatusReportLastRouteSpeed? LastRouteSpeed,
        byte? RoutingAttempts,
        ushort? RouteFailedLastFunctionalNodeId,
        ushort? RouteFailedFirstNonFunctionalNodeId,
        sbyte? TransmitPower,
        RssiMeasurement? MeasuredNoiseFloor,
        sbyte? DestinationAckTransmitPower,
        RssiMeasurement? DestinationAckMeasuredRssi,
        RssiMeasurement? DestinationAckMeasuredNoiseFloor);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SendData,
            new[]
            {
                (
                    Request: SendDataRequest.Create(
                        nodeId: 2,
                        NodeIdType.Short,
                        data: new byte[] { 0x03, 0x86, 0x13, 0x5e },
                        TransmissionOptions.ACK | TransmissionOptions.AutoRoute | TransmissionOptions.Explore,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x04, 0x03, 0x86, 0x13, 0x5e, 0x25, 0x01 }
                ),
            });

    [TestMethod]
    public void Request16Bit()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SendData,
            new[]
            {
                (
                    Request: SendDataRequest.Create(
                        nodeId: 0x0102,
                        NodeIdType.Long,
                        data: new byte[] { 0x03, 0x86, 0x13, 0x5e },
                        TransmissionOptions.ACK | TransmissionOptions.AutoRoute | TransmissionOptions.Explore,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0x01, 0x02, 0x04, 0x03, 0x86, 0x13, 0x5e, 0x25, 0x01 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<SendDataCallback, SendDataCallbackData>(
            DataFrameType.REQ,
            CommandId.SendData,
            new[]
            {
                (
                    CommandParameters: new byte[]
                    {
                        0x01, 0x00, 0x00, 0x03, 0x00, 0xd5, 0x7f, 0x7f,
                        0x7f, 0x7f, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
                        0x00, 0x03, 0x01, 0x00, 0x00
                    },
                    ExpectedData: new SendDataCallbackData(
                        SessionId: 1,
                        TransmissionStatus: TransmissionStatus.Ok,
                        TransmissionStatusReport: new TransmissionStatusReportData(
                            TransitTime: TimeSpan.FromMilliseconds(30),
                            NumRepeaters: 0,
                            AckRssi: new RssiMeasurement(-43),
                            AckChannelNumber: 0,
                            TransmitChannelNumber: 0,
                            RouteSchemeState: 3,
                            LastRouteRepeaters: new byte[] { 0x00, 0x00, 0x00, 0x00 },
                            Beam1000ms: false,
                            Beam250ms: false,
                            LastRouteSpeed: TransmissionStatusReportLastRouteSpeed.ZWave100k,
                            RoutingAttempts: 1,
                            RouteFailedLastFunctionalNodeId: 0,
                            RouteFailedFirstNonFunctionalNodeId: 0,
                            TransmitPower: null,
                            MeasuredNoiseFloor: null,
                            DestinationAckTransmitPower: null,
                            DestinationAckMeasuredRssi: null,
                            DestinationAckMeasuredNoiseFloor: null))
                )
            },
            additionalExcludedProperties: new[] { "AckRepeaterRssi" });

    [TestMethod]
    public void CallbackAckRepeaterRssi()
    {
        byte[] commandParameters = new byte[]
        {
            0x01, 0x00, 0x00, 0x03, 0x00, 0xd5, 0x7f, 0x7f,
            0x7f, 0x7f, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x00, 0x03, 0x01, 0x00, 0x00
        };

        DataFrame dataFrame = DataFrame.Create(DataFrameType.REQ, CommandId.SendData, commandParameters);
        SendDataCallback callback = SendDataCallback.Create(dataFrame, new CommandParsingContext(NodeIdType.Short));
        ReadOnlySpan<RssiMeasurement> ackRepeaterRssi = callback.TransmissionStatusReport!.Value.AckRepeaterRssi;

        Assert.AreEqual(4, ackRepeaterRssi.Length);
        Assert.AreEqual(new RssiMeasurement(127), ackRepeaterRssi[0]);
        Assert.AreEqual(new RssiMeasurement(127), ackRepeaterRssi[1]);
        Assert.AreEqual(new RssiMeasurement(127), ackRepeaterRssi[2]);
        Assert.AreEqual(new RssiMeasurement(127), ackRepeaterRssi[3]);
    }
}
