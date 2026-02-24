using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests.Commands;

[TestClass]
public class GetCapabilitiesTests : CommandTestBase
{
    private record GetCapabilitiesResponseData(
        byte SerialApiVersion,
        byte SerialApiRevision,
        ushort ManufacturerId,
        ushort ManufacturerProductType,
        ushort ManufacturerProductId,
        IReadOnlySet<CommandId> SupportedCommandIds);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.GetCapabilities,
            new[]
            {
                (Request: GetCapabilitiesRequest.Create(), ExpectedCommandParameters: Array.Empty<byte>()),
            });

    [TestMethod]
    public void Response()
        => TestReceivableCommand<GetCapabilitiesResponse, GetCapabilitiesResponseData>(
            DataFrameType.RES,
            CommandId.GetCapabilities,
            new[]
            {
                (
                    CommandParameters: new byte[]
                    {
                        0x01, 0x02, 0x00, 0x86, 0x00, 0x01, 0x00, 0x5a,
                        0xfe, 0x87, 0x7f, 0x88, 0xcf, 0x7f, 0xc0, 0x4f,
                        0xfb, 0xdf, 0xfd, 0xe0, 0x67, 0x00, 0x80, 0x80,
                        0x00, 0x80, 0x86, 0x00, 0x01, 0x00, 0xe8, 0x73,
                        0x00, 0x80, 0x0f, 0x00, 0x00, 0x60, 0x00, 0x00
                    },
                    ExpectedData: new GetCapabilitiesResponseData(
                        SerialApiVersion: 1,
                        SerialApiRevision: 2,
                        ManufacturerId: 134,
                        ManufacturerProductType: 1,
                        ManufacturerProductId: 90,
                        SupportedCommandIds: new HashSet<CommandId>
                        {
                            CommandId.GetInitData,
                            CommandId.ApplicationNodeInformation,
                            CommandId.ApplicationCommandHandler,
                            CommandId.GetControllerCapabilities,
                            CommandId.SerialApiSetTimeouts,
                            CommandId.GetCapabilities,
                            CommandId.SoftReset,
                            CommandId.GetProtocolVersion,
                            CommandId.SerialApiStarted,
                            CommandId.SerialApiSetup,
                            CommandId.SetRFReceiveMode,
                            CommandId.SetSleepMode,
                            CommandId.SendNodeInformation,
                            CommandId.SendData,
                            CommandId.SendDataMulti,
                            CommandId.GetLibraryVersion,
                            CommandId.SendDataAbort,
                            CommandId.RFPowerLevelSet,
                            CommandId.GetRandomWord,
                            CommandId.GetNetworkIds,
                            CommandId.MemoryGetByte,
                            CommandId.MemoryPutByte,
                            CommandId.MemoryGetBuffer,
                            CommandId.MemoryPutBuffer,
                            CommandId.FlashAutoProgSet,
                            CommandId.NvrGetValue,
                            CommandId.NvmGetId,
                            CommandId.NvmExtReadLongBuffer,
                            CommandId.NvmExtWriteLongBuffer,
                            CommandId.NvmExtReadLongByte,
                            CommandId.NvmExtWriteLongByte,
                            (CommandId)46,
                            (CommandId)47,
                            CommandId.ClearTxTimers,
                            CommandId.GetTxTimer,
                            CommandId.ClearNetworkStats,
                            CommandId.GetNetworkStats,
                            CommandId.GetBackgroundRSSI,
                            CommandId.SetListenBeforeTalkThreshold,
                            CommandId.RemoveSpecificNodeFromNetwork,
                            CommandId.GetNodeInformationProtocolData,
                            CommandId.SetDefault,
                            CommandId.ReplicationReceiveComplete,
                            CommandId.ReplicationSend,
                            CommandId.AssignReturnRoute,
                            CommandId.DeleteReturnRoute,
                            CommandId.RequestNodeNeighborDiscovery,
                            CommandId.ApplicationUpdate,
                            CommandId.AddNodeToNetwork,
                            CommandId.RemoveNodeFromNetwork,
                            CommandId.AddControllerAndAssignPrimaryControllerRole,
                            CommandId.AddPrimaryController,
                            CommandId.AssignPriorityReturnRoute,
                            CommandId.SetLearnMode,
                            CommandId.AssignSucReturnRoute,
                            CommandId.RequestNetworkUpdate,
                            CommandId.SetSucNodeId,
                            CommandId.DeleteSucReturnRoute,
                            CommandId.GetSucNodeId,
                            CommandId.SendSucId,
                            CommandId.AssignPrioritySucReturnRoute,
                            CommandId.ExploreRequestInclusion,
                            CommandId.ExploreRequestExclusion,
                            CommandId.RequestNodeInfo,
                            CommandId.RemoveFailedNode,
                            CommandId.IsNodeFailed,
                            CommandId.ReplaceFailedNode,
                            (CommandId)102,
                            (CommandId)103,
                            CommandId.LegacyFirmwareUpdate,
                            CommandId.GetNeighborTableLine,
                            CommandId.LockUnlockLastRoute,
                            CommandId.GetPriorityRoute,
                            CommandId.SetPriorityRoute,
                            (CommandId)152,
                            (CommandId)161,
                            CommandId.SetWutTimeout,
                            CommandId.WatchdogEnable,
                            CommandId.WatchdogDisable,
                            CommandId.WatchdogKick,
                            CommandId.SetExtIntLevel,
                            CommandId.RFPowerLevelGet,
                            CommandId.GetLibraryType,
                            CommandId.SendTestFrame,
                            CommandId.GetProtocolStatus,
                            CommandId.SetPromiscuousMode,
                            (CommandId)209,
                            (CommandId)210,
                            (CommandId)211,
                            CommandId.SetMaximumRoutingAttempts,
                            (CommandId)238,
                            (CommandId)239,
                        })
                )
            });
}
