using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class WindowCoveringCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        WindowCoveringCommandClass.WindowCoveringSupportedGetCommand command =
            WindowCoveringCommandClass.WindowCoveringSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.WindowCovering, WindowCoveringCommandClass.WindowCoveringSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)WindowCoveringCommand.SupportedGet, WindowCoveringCommandClass.WindowCoveringSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_SingleMaskByte_PositionParameters()
    {
        // CC=0x6A, Cmd=0x02, Header=0x01 (reserved=0, maskCount=1), Mask1=0x0A (bits 1,3 = params 1,3)
        byte[] data = [0x6A, 0x02, 0x01, 0x0A];
        CommandClassFrame frame = new(data);

        IReadOnlySet<WindowCoveringParameterId> supported = WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(WindowCoveringParameterId.OutboundLeftPosition, supported);
        Assert.Contains(WindowCoveringParameterId.OutboundRightPosition, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_ThreeMaskBytes_MultipleParameters()
    {
        // Header=0x03 (maskCount=3)
        // Mask1=0x02 (bit 1 = param 1: OutboundLeftPosition)
        // Mask2=0x08 (bit 3 = param 11: VerticalSlatsAnglePosition)
        // Mask3=0x20 (bit 5 = param 21: InboundTopBottomPosition)
        byte[] data = [0x6A, 0x02, 0x03, 0x02, 0x08, 0x20];
        CommandClassFrame frame = new(data);

        IReadOnlySet<WindowCoveringParameterId> supported = WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, supported);
        Assert.Contains(WindowCoveringParameterId.OutboundLeftPosition, supported);
        Assert.Contains(WindowCoveringParameterId.VerticalSlatsAnglePosition, supported);
        Assert.Contains(WindowCoveringParameterId.InboundTopBottomPosition, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_AllParameters()
    {
        // 24 parameter IDs need 3 mask bytes. All bits 0-23 set.
        // Mask1=0xFF (params 0-7), Mask2=0xFF (params 8-15), Mask3=0xFF (params 16-23)
        byte[] data = [0x6A, 0x02, 0x03, 0xFF, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        IReadOnlySet<WindowCoveringParameterId> supported = WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(24, supported);
        Assert.Contains(WindowCoveringParameterId.OutboundLeftMovement, supported);
        Assert.Contains(WindowCoveringParameterId.HorizontalSlatsAnglePosition, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_MovementOnlyParameters()
    {
        // Mask1=0x05 (bits 0,2 = params 0,2: movement-only even IDs)
        byte[] data = [0x6A, 0x02, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        IReadOnlySet<WindowCoveringParameterId> supported = WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(WindowCoveringParameterId.OutboundLeftMovement, supported);
        Assert.Contains(WindowCoveringParameterId.OutboundRightMovement, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_EmptyMask()
    {
        // Mask byte count=1, mask=0x00 (no parameters)
        byte[] data = [0x6A, 0x02, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<WindowCoveringParameterId> supported = WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_ReservedBitsIgnored()
    {
        // Header=0xF1 (reserved=0xF, maskCount=1) - reserved nibble should be ignored
        byte[] data = [0x6A, 0x02, 0xF1, 0x02];
        CommandClassFrame frame = new(data);

        IReadOnlySet<WindowCoveringParameterId> supported = WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, supported);
        Assert.Contains(WindowCoveringParameterId.OutboundLeftPosition, supported);
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        // CC=0x6A, Cmd=0x02, no parameters
        byte[] data = [0x6A, 0x02];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_InvalidMaskByteCount_Zero_Throws()
    {
        // Header=0x00 (maskCount=0, which is below minimum of 1)
        byte[] data = [0x6A, 0x02, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_InsufficientMaskBytes_Throws()
    {
        // Header=0x03 (maskCount=3) but only 1 mask byte follows
        byte[] data = [0x6A, 0x02, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => WindowCoveringCommandClass.WindowCoveringSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
