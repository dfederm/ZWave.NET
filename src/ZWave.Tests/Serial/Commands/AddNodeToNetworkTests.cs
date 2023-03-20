using ZWave.Serial.Commands;
using ZWave.Serial;
using ZWave.CommandClasses;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class AddNodeToNetworkTests : CommandTestBase
{
    private record AddNodeToNetworkCallbackData(
        byte SessionId,
        AddNodeStatus Status,
        byte AssignedNodeId,
        byte BasicDeviceClass,
        byte GenericDeviceClass,
        byte SpecificDeviceClass,
        IReadOnlyList<CommandClassInfo> CommandClasses);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.AddNodeToNetwork,
            new[]
            {
                (
                    Request: AddNodeToNetworkRequest.Create(
                        isHighPower: true,
                        isNetworkWide: true,
                        isLongRange: true,
                        addMode: AddNodeMode.Any,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0xe1, 0x01 }
                ),
            });

    [TestMethod]
    public void RequestSmartStartInclude()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.AddNodeToNetwork,
            new[]
            {
                (
                    Request: AddNodeToNetworkRequest.CreateSmartStartInclude(
                        isHighPower: true,
                        isNetworkWide: true,
                        isLongRange: true,
                        sessionId: 1,
                        nwiHomeId: 123,
                        authHomeId: 456),
                    ExpectedCommandParameters: new byte[] { 0xe8, 0x01, 0x00, 0x00, 0x00, 0x7B, 0x00, 0x00, 0x01, 0xc8 }
                ),
            });

    [TestMethod]
    public void RequestStartSmartStart()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.AddNodeToNetwork,
            new[]
            {
                (
                    Request: AddNodeToNetworkRequest.CreateStartSmartStart(),
                    ExpectedCommandParameters: new byte[] { 0x48, 0x00 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<AddNodeToNetworkCallback, AddNodeToNetworkCallbackData>(
            DataFrameType.REQ,
            CommandId.AddNodeToNetwork,
            new[]
            {
                (
                    CommandParameters: new byte[]
                    {
                        0x7b, 0x01, 0x02, 0x0e, 0x04, 0x11, 0x01, 0x5e,
                        0x56, 0x86, 0x72, 0x5a, 0x85, 0x59, 0x73, 0x26,
                        0x27, 0x70, 0x2c, 0x2b, 0x7a
                    },
                    ExpectedData: new AddNodeToNetworkCallbackData(
                        SessionId: 123,
                        Status: AddNodeStatus.NetworkInclusionStarted,
                        AssignedNodeId: 2,
                        BasicDeviceClass: 4,
                        GenericDeviceClass: 17,
                        SpecificDeviceClass: 1,
                        CommandClasses: new[]
                        {
                            new CommandClassInfo(CommandClassId.ZWavePlusInfo, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.Crc16Encapsulation, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.Version, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.ManufacturerSpecific, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.DeviceResetLocally, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.Association, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.AssociationGroupInformation, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.Powerlevel, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.MultilevelSwitch, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.AllSwitch, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.Configuration, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.SceneActuatorConfiguration, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.SceneActivation, IsSupported: true, IsControlled: false),
                            new CommandClassInfo(CommandClassId.FirmwareUpdateMetaData, IsSupported: true, IsControlled: false),
                        })
                )
            });
}
