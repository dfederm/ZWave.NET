using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class SetLearnModeTests : CommandTestBase
{
    private record SetLearnModeCallbackData(byte SessionId, LearnModeStatus Status, byte AssignedNodeId);

    [TestMethod]
    public void Request()
        => TestSendableCommand(
            DataFrameType.REQ,
            CommandId.SetLearnMode,
            new[]
            {
                (
                    Request: SetLearnModeRequest.Create(
                        mode: LearnMode.NetworkWideInclusion,
                        sessionId: 1),
                    ExpectedCommandParameters: new byte[] { 0x02, 0x01 }
                ),
            });

    [TestMethod]
    public void Callback()
        => TestReceivableCommand<SetLearnModeCallback, SetLearnModeCallbackData>(
            DataFrameType.REQ,
            CommandId.SetLearnMode,
            new[]
            {
                (
                    CommandParameters: new byte[] { 0x01, 0x06, 0x05 },
                    ExpectedData: new SetLearnModeCallbackData(
                        SessionId: 1,
                        Status: LearnModeStatus.Done,
                        AssignedNodeId: 5)
                )
            });
}
