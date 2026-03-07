using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class BarrierOperatorCommandClassTests
{
    [TestMethod]
    public void SignalSupportedGetCommand_Create()
    {
        var command = BarrierOperatorCommandClass.SignalSupportedGetCommand.Create();

        Assert.AreEqual(2, command.Frame.Data.Length);
        Assert.AreEqual(CommandClassId.BarrierOperator, command.Frame.CommandClassId);
        Assert.AreEqual((byte)BarrierOperatorCommand.SignalSupportedGet, command.Frame.CommandId);
    }

    [TestMethod]
    public void SignalSupportedReportCommand_Parse_BothSubsystems()
    {
        // Bit 0 = type 0x01 (Audible), bit 1 = type 0x02 (Visual) → 0x03
        byte[] data = [0x66, 0x05, 0x03];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BarrierOperatorSignalingSubsystemType> supported =
            BarrierOperatorCommandClass.SignalSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, supported);
        Assert.Contains(BarrierOperatorSignalingSubsystemType.AudibleNotification, supported);
        Assert.Contains(BarrierOperatorSignalingSubsystemType.VisualNotification, supported);
    }

    [TestMethod]
    public void SignalSupportedReportCommand_Parse_AudibleOnly()
    {
        // Bit 0 = type 0x01 (Audible) → 0x01
        byte[] data = [0x66, 0x05, 0x01];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BarrierOperatorSignalingSubsystemType> supported =
            BarrierOperatorCommandClass.SignalSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, supported);
        Assert.Contains(BarrierOperatorSignalingSubsystemType.AudibleNotification, supported);
    }

    [TestMethod]
    public void SignalSupportedReportCommand_Parse_VisualOnly()
    {
        // Bit 1 = type 0x02 (Visual) → 0x02
        byte[] data = [0x66, 0x05, 0x02];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BarrierOperatorSignalingSubsystemType> supported =
            BarrierOperatorCommandClass.SignalSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, supported);
        Assert.Contains(BarrierOperatorSignalingSubsystemType.VisualNotification, supported);
    }

    [TestMethod]
    public void SignalSupportedReportCommand_Parse_NoneSupported()
    {
        // All bits zero — no subsystems supported
        byte[] data = [0x66, 0x05, 0x00];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BarrierOperatorSignalingSubsystemType> supported =
            BarrierOperatorCommandClass.SignalSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(supported);
    }

    [TestMethod]
    public void SignalSupportedReportCommand_Parse_MultiByteMask()
    {
        // 2-byte bitmask: byte 0 = 0x03 (types 1,2), byte 1 = 0x01 (type 9)
        byte[] data = [0x66, 0x05, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        IReadOnlySet<BarrierOperatorSignalingSubsystemType> supported =
            BarrierOperatorCommandClass.SignalSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(3, supported);
        Assert.Contains(BarrierOperatorSignalingSubsystemType.AudibleNotification, supported);
        Assert.Contains(BarrierOperatorSignalingSubsystemType.VisualNotification, supported);
        Assert.Contains((BarrierOperatorSignalingSubsystemType)9, supported);
    }

    [TestMethod]
    public void SignalSupportedReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x66, 0x05];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => BarrierOperatorCommandClass.SignalSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
