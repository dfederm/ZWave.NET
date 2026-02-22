using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class SerialApiSetupTests : CommandTestBase
{
    private record SerialApiSetupGetSupportedCommandsResponseData(
        bool WasSubcommandSupported,
        HashSet<SerialApiSetupSubcommand> SupportedSubcommands);

    private record SerialApiSetupSetTxStatusReportResponseData(
        bool WasSubcommandSupported,
        bool Success);

    private record SerialApiSetupSetNodeIdBaseTypeResponseData(
        bool WasSubcommandSupported,
        bool Success);

    private record SerialApiSetupSetMaxLongRangePowerlevelResponseData(
        bool WasSubcommandSupported,
        bool Success);

    private record SerialApiSetupGetMaxLongRangePowerlevelResponseData(
        bool WasSubcommandSupported,
        short MaxPowerlevelDeciDbm);

    private record SerialApiSetupSetLongRangeMaxNodeIdResponseData(
        bool WasSubcommandSupported,
        bool Success);

    private record SerialApiSetupGetLongRangeMaxNodeIdResponseData(
        bool WasSubcommandSupported,
        ushort CurrentMaxNodeId,
        ushort MaxSupportedNodeId);

    private record SerialApiSetupGetLongRangeMaxPayloadSizeResponseData(
        bool WasSubcommandSupported,
        byte MaxPayloadSize);

    private record SerialApiSetupSetPowerlevelResponseData(
        bool WasSubcommandSupported,
        bool Success);

    private record SerialApiSetupGetPowerlevelResponseData(
        bool WasSubcommandSupported,
        sbyte NormalPowerDeciDbm,
        sbyte Measured0dBmDeciDbm);

    private record SerialApiSetupGetMaxPayloadSizeResponseData(
        bool WasSubcommandSupported,
        byte MaxPayloadSize);

    private record SerialApiSetupGetRfRegionResponseData(
        bool WasSubcommandSupported,
        RfRegion Region);

    private record SerialApiSetupSetRfRegionResponseData(
        bool WasSubcommandSupported,
        bool Success);

    private record SerialApiSetupSet16BitPowerlevelResponseData(
        bool WasSubcommandSupported,
        bool Success);

    private record SerialApiSetupGet16BitPowerlevelResponseData(
        bool WasSubcommandSupported,
        short NormalPowerlevelDeciDbm,
        short Measured0dBmPowerlevelDeciDbm);

    private record SerialApiSetupGetSupportedRegionsResponseData(
        bool WasSubcommandSupported,
        byte Count);

    private record SerialApiSetupGetRegionInfoResponseData(
        bool WasSubcommandSupported,
        RfRegion Region,
        bool SupportsZWave,
        bool SupportsZWaveLongRange,
        RfRegion IncludesRegion);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    Request: SerialApiSetupRequest.GetSupportedCommands(),
                    ExpectedCommandParameters: new byte[] { 0x01 }
                ),

                // Synthetic
                (
                    Request: SerialApiSetupRequest.SetTxStatusReport(enable: true),
                    ExpectedCommandParameters: [0x02, 0x01]
                ),

                (
                    Request: SerialApiSetupRequest.SetNodeIdBaseType(NodeIdType.Long),
                    ExpectedCommandParameters: [0x80, 0x02]
                ),

                (
                    Request: SerialApiSetupRequest.SetNodeIdBaseType(NodeIdType.Short),
                    ExpectedCommandParameters: [0x80, 0x01]
                ),

                (
                    Request: SerialApiSetupRequest.SetMaxLongRangePowerlevel(200),
                    ExpectedCommandParameters: [0x03, 0x00, 0xC8]
                ),

                (
                    Request: SerialApiSetupRequest.SetMaxLongRangePowerlevel(-60),
                    ExpectedCommandParameters: [0x03, 0xFF, 0xC4]
                ),

                (
                    Request: SerialApiSetupRequest.GetMaxLongRangePowerlevel(),
                    ExpectedCommandParameters: [0x05]
                ),

                (
                    Request: SerialApiSetupRequest.SetLongRangeMaxNodeId(0x03FF),
                    ExpectedCommandParameters: [0x06, 0x03, 0xFF]
                ),

                (
                    Request: SerialApiSetupRequest.GetLongRangeMaxNodeId(),
                    ExpectedCommandParameters: [0x07]
                ),

                (
                    Request: SerialApiSetupRequest.GetLongRangeMaxPayloadSize(),
                    ExpectedCommandParameters: [0x11]
                ),

                // SetPowerlevel: normalPower=10 (0x0A), measured0dBm=-20 (0xEC)
                (
                    Request: SerialApiSetupRequest.SetPowerlevel(10, -20),
                    ExpectedCommandParameters: [0x04, 0x0A, 0xEC]
                ),

                (
                    Request: SerialApiSetupRequest.GetPowerlevel(),
                    ExpectedCommandParameters: [0x08]
                ),

                (
                    Request: SerialApiSetupRequest.GetMaxPayloadSize(),
                    ExpectedCommandParameters: [0x10]
                ),

                (
                    Request: SerialApiSetupRequest.GetRfRegion(),
                    ExpectedCommandParameters: [0x20]
                ),

                (
                    Request: SerialApiSetupRequest.SetRfRegion(RfRegion.US),
                    ExpectedCommandParameters: [0x40, 0x01]
                ),

                (
                    Request: SerialApiSetupRequest.SetRfRegion(RfRegion.USLongRange),
                    ExpectedCommandParameters: [0x40, 0x09]
                ),

                // Set16BitPowerlevel: normalPower=130 (0x0082), measured0dBm=-130 (0xFF7E)
                (
                    Request: SerialApiSetupRequest.Set16BitPowerlevel(130, -130),
                    ExpectedCommandParameters: [0x12, 0x00, 0x82, 0xFF, 0x7E]
                ),

                (
                    Request: SerialApiSetupRequest.Get16BitPowerlevel(),
                    ExpectedCommandParameters: [0x13]
                ),

                (
                    Request: SerialApiSetupRequest.GetSupportedRegions(),
                    ExpectedCommandParameters: [0x15]
                ),

                (
                    Request: SerialApiSetupRequest.GetRegionInfo(RfRegion.USLongRange),
                    ExpectedCommandParameters: [0x16, 0x09]
                ),
            });

    [TestMethod]
    public void GetSupportedCommandsResponse()
        => TestReceivableCommand<SerialApiSetupGetSupportedCommandsResponse, SerialApiSetupGetSupportedCommandsResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x1e },
                    ExpectedData: new SerialApiSetupGetSupportedCommandsResponseData(
                        WasSubcommandSupported: true,
                        SupportedSubcommands: new HashSet<SerialApiSetupSubcommand>
                        {
                            SerialApiSetupSubcommand.GetSupportedCommands,
                            SerialApiSetupSubcommand.SetPowerlevel,
                            SerialApiSetupSubcommand.GetPowerlevel,
                            SerialApiSetupSubcommand.GetMaxPayloadSize,
                            SerialApiSetupSubcommand.GetRFRegion
                        })
                ),
            });

    [TestMethod]
    public void SetTxStatusReportResponse()
        => TestReceivableCommand<SerialApiSetupSetTxStatusReportResponse, SerialApiSetupSetTxStatusReportResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                // Synthetic
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupSetTxStatusReportResponseData(
                        WasSubcommandSupported: true,
                        Success: true)
                ),
            });

    [TestMethod]
    public void SetNodeIdBaseTypeResponse()
        => TestReceivableCommand<SerialApiSetupSetNodeIdBaseTypeResponse, SerialApiSetupSetNodeIdBaseTypeResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupSetNodeIdBaseTypeResponseData(
                        WasSubcommandSupported: true,
                        Success: true)
                ),
                (
                    CommandParameters: [0x00, 0x00],
                    ExpectedData: new SerialApiSetupSetNodeIdBaseTypeResponseData(
                        WasSubcommandSupported: false,
                        Success: false)
                ),
            });

    [TestMethod]
    public void SetMaxLongRangePowerlevel_TooLow_Throws()
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => SerialApiSetupRequest.SetMaxLongRangePowerlevel(SerialApiSetupRequest.MinLongRangePowerlevelDeciDbm - 1));

    [TestMethod]
    public void SetMaxLongRangePowerlevel_TooHigh_Throws()
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => SerialApiSetupRequest.SetMaxLongRangePowerlevel(SerialApiSetupRequest.MaxLongRangePowerlevelDeciDbm + 1));

    [TestMethod]
    public void SetMaxLongRangePowerlevelResponse()
        => TestReceivableCommand<SerialApiSetupSetMaxLongRangePowerlevelResponse, SerialApiSetupSetMaxLongRangePowerlevelResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupSetMaxLongRangePowerlevelResponseData(
                        WasSubcommandSupported: true,
                        Success: true)
                ),
                (
                    CommandParameters: [0x01, 0x00],
                    ExpectedData: new SerialApiSetupSetMaxLongRangePowerlevelResponseData(
                        WasSubcommandSupported: true,
                        Success: false)
                ),
            });

    [TestMethod]
    public void GetMaxLongRangePowerlevelResponse()
        => TestReceivableCommand<SerialApiSetupGetMaxLongRangePowerlevelResponse, SerialApiSetupGetMaxLongRangePowerlevelResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                // 200 deci-dBm = 0x00C8
                (
                    CommandParameters: new byte[] { 0x01, 0x00, 0xC8 },
                    ExpectedData: new SerialApiSetupGetMaxLongRangePowerlevelResponseData(
                        WasSubcommandSupported: true,
                        MaxPowerlevelDeciDbm: 200)
                ),
                // -100 deci-dBm = 0xFF9C
                (
                    CommandParameters: [0x01, 0xFF, 0x9C],
                    ExpectedData: new SerialApiSetupGetMaxLongRangePowerlevelResponseData(
                        WasSubcommandSupported: true,
                        MaxPowerlevelDeciDbm: -100)
                ),
            });

    [TestMethod]
    public void SetLongRangeMaxNodeIdResponse()
        => TestReceivableCommand<SerialApiSetupSetLongRangeMaxNodeIdResponse, SerialApiSetupSetLongRangeMaxNodeIdResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupSetLongRangeMaxNodeIdResponseData(
                        WasSubcommandSupported: true,
                        Success: true)
                ),
            });

    [TestMethod]
    public void GetLongRangeMaxNodeIdResponse()
        => TestReceivableCommand<SerialApiSetupGetLongRangeMaxNodeIdResponse, SerialApiSetupGetLongRangeMaxNodeIdResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                // Current max node ID = 0x03FF = 1023, max supported = 0x07FF = 2047
                (
                    CommandParameters: new byte[] { 0x07, 0x03, 0xFF, 0x07, 0xFF },
                    ExpectedData: new SerialApiSetupGetLongRangeMaxNodeIdResponseData(
                        WasSubcommandSupported: true,
                        CurrentMaxNodeId: 0x03FF,
                        MaxSupportedNodeId: 0x07FF)
                ),
            });

    [TestMethod]
    public void GetLongRangeMaxPayloadSizeResponse()
        => TestReceivableCommand<SerialApiSetupGetLongRangeMaxPayloadSizeResponse, SerialApiSetupGetLongRangeMaxPayloadSizeResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0xA0 },
                    ExpectedData: new SerialApiSetupGetLongRangeMaxPayloadSizeResponseData(
                        WasSubcommandSupported: true,
                        MaxPayloadSize: 0xA0)
                ),
            });

    [TestMethod]
    public void SetPowerlevelResponse()
        => TestReceivableCommand<SerialApiSetupSetPowerlevelResponse, SerialApiSetupSetPowerlevelResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupSetPowerlevelResponseData(
                        WasSubcommandSupported: true,
                        Success: true)
                ),
                (
                    CommandParameters: [0x01, 0x00],
                    ExpectedData: new SerialApiSetupSetPowerlevelResponseData(
                        WasSubcommandSupported: true,
                        Success: false)
                ),
            });

    [TestMethod]
    public void GetPowerlevelResponse()
        => TestReceivableCommand<SerialApiSetupGetPowerlevelResponse, SerialApiSetupGetPowerlevelResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                // normalPower=10 (1.0 dBm), measured0dBm=-20 (-2.0 dBm = 0xEC)
                (
                    CommandParameters: new byte[] { 0x01, 0x0A, 0xEC },
                    ExpectedData: new SerialApiSetupGetPowerlevelResponseData(
                        WasSubcommandSupported: true,
                        NormalPowerDeciDbm: 10,
                        Measured0dBmDeciDbm: -20)
                ),
            });

    [TestMethod]
    public void GetMaxPayloadSizeResponse()
        => TestReceivableCommand<SerialApiSetupGetMaxPayloadSizeResponse, SerialApiSetupGetMaxPayloadSizeResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x2E },
                    ExpectedData: new SerialApiSetupGetMaxPayloadSizeResponseData(
                        WasSubcommandSupported: true,
                        MaxPayloadSize: 0x2E)
                ),
            });

    [TestMethod]
    public void GetRfRegionResponse()
        => TestReceivableCommand<SerialApiSetupGetRfRegionResponse, SerialApiSetupGetRfRegionResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupGetRfRegionResponseData(
                        WasSubcommandSupported: true,
                        Region: RfRegion.US)
                ),
                (
                    CommandParameters: [0x01, 0x09],
                    ExpectedData: new SerialApiSetupGetRfRegionResponseData(
                        WasSubcommandSupported: true,
                        Region: RfRegion.USLongRange)
                ),
            });

    [TestMethod]
    public void SetRfRegionResponse()
        => TestReceivableCommand<SerialApiSetupSetRfRegionResponse, SerialApiSetupSetRfRegionResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupSetRfRegionResponseData(
                        WasSubcommandSupported: true,
                        Success: true)
                ),
                (
                    CommandParameters: [0x01, 0x00],
                    ExpectedData: new SerialApiSetupSetRfRegionResponseData(
                        WasSubcommandSupported: true,
                        Success: false)
                ),
            });

    [TestMethod]
    public void Set16BitPowerlevelResponse()
        => TestReceivableCommand<SerialApiSetupSet16BitPowerlevelResponse, SerialApiSetupSet16BitPowerlevelResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x01 },
                    ExpectedData: new SerialApiSetupSet16BitPowerlevelResponseData(
                        WasSubcommandSupported: true,
                        Success: true)
                ),
                (
                    CommandParameters: [0x01, 0x00],
                    ExpectedData: new SerialApiSetupSet16BitPowerlevelResponseData(
                        WasSubcommandSupported: true,
                        Success: false)
                ),
            });

    [TestMethod]
    public void Get16BitPowerlevelResponse()
        => TestReceivableCommand<SerialApiSetupGet16BitPowerlevelResponse, SerialApiSetupGet16BitPowerlevelResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                // normalPower=130 (0x0082), measured0dBm=-130 (0xFF7E)
                (
                    CommandParameters: new byte[] { 0x01, 0x00, 0x82, 0xFF, 0x7E },
                    ExpectedData: new SerialApiSetupGet16BitPowerlevelResponseData(
                        WasSubcommandSupported: true,
                        NormalPowerlevelDeciDbm: 130,
                        Measured0dBmPowerlevelDeciDbm: -130)
                ),
            });

    [TestMethod]
    public void GetSupportedRegionsResponse()
        => TestReceivableCommand<SerialApiSetupGetSupportedRegionsResponse, SerialApiSetupGetSupportedRegionsResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                // 3 regions: EU (0x00), US (0x01), US_LR (0x09)
                (
                    CommandParameters: new byte[] { 0x01, 0x03, 0x00, 0x01, 0x09 },
                    ExpectedData: new SerialApiSetupGetSupportedRegionsResponseData(
                        WasSubcommandSupported: true,
                        Count: 3)
                ),
            });

    [TestMethod]
    public void GetRegionInfoResponse()
        => TestReceivableCommand<SerialApiSetupGetRegionInfoResponse, SerialApiSetupGetRegionInfoResponseData>(
            DataFrameType.RES,
            CommandId.SerialApiSetup,
            new[]
            {
                // US_LR region: supports Z-Wave + LR (0x03), includes US (0x01)
                (
                    CommandParameters: new byte[] { 0x01, 0x09, 0x03, 0x01 },
                    ExpectedData: new SerialApiSetupGetRegionInfoResponseData(
                        WasSubcommandSupported: true,
                        Region: RfRegion.USLongRange,
                        SupportsZWave: true,
                        SupportsZWaveLongRange: true,
                        IncludesRegion: RfRegion.US)
                ),
                // EU region: supports Z-Wave only (0x01), includes self (0x00)
                (
                    CommandParameters: [0x01, 0x00, 0x01, 0x00],
                    ExpectedData: new SerialApiSetupGetRegionInfoResponseData(
                        WasSubcommandSupported: true,
                        Region: RfRegion.Europe,
                        SupportsZWave: true,
                        SupportsZWaveLongRange: false,
                        IncludesRegion: RfRegion.Europe)
                ),
            });
}
