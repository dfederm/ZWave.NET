using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ColorSwitchCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        ColorSwitchCommandClass.ColorSwitchSupportedGetCommand command =
            ColorSwitchCommandClass.ColorSwitchSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.ColorSwitch, ColorSwitchCommandClass.ColorSwitchSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)ColorSwitchCommand.SupportedGet, ColorSwitchCommandClass.ColorSwitchSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_TwoBytes_RgbwComponents()
    {
        // CC=0x33, Cmd=0x02, Mask1=0x1F (WW,CW,R,G,B), Mask2=0x00
        byte[] data = [0x33, 0x02, 0x1F, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<ColorSwitchColorComponent> supported = ColorSwitchCommandClass.ColorSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(5, supported);
        Assert.Contains(ColorSwitchColorComponent.WarmWhite, supported);
        Assert.Contains(ColorSwitchColorComponent.ColdWhite, supported);
        Assert.Contains(ColorSwitchColorComponent.Red, supported);
        Assert.Contains(ColorSwitchColorComponent.Green, supported);
        Assert.Contains(ColorSwitchColorComponent.Blue, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TwoBytes_AllComponents()
    {
        // Mask1=0xFF (bits 0-7), Mask2=0x01 (bit 8 = Index)
        byte[] data = [0x33, 0x02, 0xFF, 0x01];
        CommandClassFrame frame = new(data);

        IReadOnlySet<ColorSwitchColorComponent> supported = ColorSwitchCommandClass.ColorSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(9, supported);
        Assert.Contains(ColorSwitchColorComponent.WarmWhite, supported);
        Assert.Contains(ColorSwitchColorComponent.ColdWhite, supported);
        Assert.Contains(ColorSwitchColorComponent.Red, supported);
        Assert.Contains(ColorSwitchColorComponent.Green, supported);
        Assert.Contains(ColorSwitchColorComponent.Blue, supported);
        Assert.Contains(ColorSwitchColorComponent.Amber, supported);
        Assert.Contains(ColorSwitchColorComponent.Cyan, supported);
        Assert.Contains(ColorSwitchColorComponent.Purple, supported);
        Assert.Contains(ColorSwitchColorComponent.Index, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_SingleByte_RgbOnly()
    {
        // Only 1 mask byte: 0x1C = bits 2,3,4 = R,G,B
        byte[] data = [0x33, 0x02, 0x1C];
        CommandClassFrame frame = new(data);

        IReadOnlySet<ColorSwitchColorComponent> supported = ColorSwitchCommandClass.ColorSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, supported);
        Assert.Contains(ColorSwitchColorComponent.Red, supported);
        Assert.Contains(ColorSwitchColorComponent.Green, supported);
        Assert.Contains(ColorSwitchColorComponent.Blue, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_VariableLengthMask_ThreeBytes()
    {
        // 3 mask bytes - forward compatible with future component IDs
        byte[] data = [0x33, 0x02, 0x04, 0x00, 0x01];
        CommandClassFrame frame = new(data);

        IReadOnlySet<ColorSwitchColorComponent> supported = ColorSwitchCommandClass.ColorSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        // Bit 2 of byte 0 = component 2 (Red), Bit 0 of byte 2 = component 16 (future)
        Assert.HasCount(2, supported);
        Assert.Contains(ColorSwitchColorComponent.Red, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_EmptyMask()
    {
        // 1 mask byte = 0x00 (no components)
        byte[] data = [0x33, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<ColorSwitchColorComponent> supported = ColorSwitchCommandClass.ColorSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        // CC=0x33, Cmd=0x02, no mask bytes
        byte[] data = [0x33, 0x02];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ColorSwitchCommandClass.ColorSwitchSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
