using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MultilevelSwitchCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        MultilevelSwitchCommandClass.MultilevelSwitchSupportedGetCommand command =
            MultilevelSwitchCommandClass.MultilevelSwitchSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.MultilevelSwitch, MultilevelSwitchCommandClass.MultilevelSwitchSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)MultilevelSwitchCommand.SupportedGet, MultilevelSwitchCommandClass.MultilevelSwitchSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_DownUp()
    {
        // CC=0x26, Cmd=0x07, PrimarySwitchType=0x02 (Down/Up)
        byte[] data = [0x26, 0x07, 0x02];
        CommandClassFrame frame = new(data);

        MultilevelSwitchType switchType = MultilevelSwitchCommandClass.MultilevelSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSwitchType.DownUp, switchType);
    }

    [TestMethod]
    public void SupportedReport_Parse_OffOn()
    {
        // CC=0x26, Cmd=0x07, PrimarySwitchType=0x01 (Off/On)
        byte[] data = [0x26, 0x07, 0x01];
        CommandClassFrame frame = new(data);

        MultilevelSwitchType switchType = MultilevelSwitchCommandClass.MultilevelSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSwitchType.OffOn, switchType);
    }

    [TestMethod]
    public void SupportedReport_Parse_CloseOpen()
    {
        // CC=0x26, Cmd=0x07, PrimarySwitchType=0x03 (Close/Open)
        byte[] data = [0x26, 0x07, 0x03];
        CommandClassFrame frame = new(data);

        MultilevelSwitchType switchType = MultilevelSwitchCommandClass.MultilevelSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MultilevelSwitchType.CloseOpen, switchType);
    }

    [TestMethod]
    public void SupportedReport_Parse_ExtractsLower5Bits()
    {
        // CC=0x26, Cmd=0x07, Byte=0xE2 (reserved bits set + PrimarySwitchType=0x02)
        byte[] data = [0x26, 0x07, 0xE2];
        CommandClassFrame frame = new(data);

        MultilevelSwitchType switchType = MultilevelSwitchCommandClass.MultilevelSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        // Should extract only the lower 5 bits (0x02 = DownUp)
        Assert.AreEqual(MultilevelSwitchType.DownUp, switchType);
    }

    [TestMethod]
    public void SupportedReport_Parse_WithSecondaryType_IgnoresSecondary()
    {
        // CC=0x26, Cmd=0x07, Primary=0x02 (DownUp), Secondary=0x03 (CloseOpen)
        byte[] data = [0x26, 0x07, 0x02, 0x03];
        CommandClassFrame frame = new(data);

        MultilevelSwitchType switchType = MultilevelSwitchCommandClass.MultilevelSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        // Only the primary switch type is returned; secondary is ignored
        Assert.AreEqual(MultilevelSwitchType.DownUp, switchType);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        // CC=0x26, Cmd=0x07, no parameters
        byte[] data = [0x26, 0x07];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => MultilevelSwitchCommandClass.MultilevelSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
