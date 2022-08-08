using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class SoftResetTests
{
    [TestMethod]
    public void Request()
    {
        Assert.AreEqual(DataFrameType.REQ, SoftResetRequest.Type);
        Assert.AreEqual(CommandId.SoftReset, SoftResetRequest.CommandId);

        var request = SoftResetRequest.Create();

        Assert.AreEqual(DataFrameType.REQ, request.Frame.Type);
        Assert.AreEqual(CommandId.SoftReset, request.Frame.CommandId);
        Assert.IsTrue(request.Frame.CommandParameters.IsEmpty);
    }
}
