using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class WakeUpCommandClassTests
{
    [TestMethod]
    public void IntervalCapabilitiesGetCommand_Create_HasCorrectFormat()
    {
        WakeUpCommandClass.WakeUpIntervalCapabilitiesGetCommand command =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesGetCommand.Create();

        Assert.AreEqual(CommandClassId.WakeUp, WakeUpCommandClass.WakeUpIntervalCapabilitiesGetCommand.CommandClassId);
        Assert.AreEqual((byte)WakeUpCommand.IntervalCapabilitiesGet, WakeUpCommandClass.WakeUpIntervalCapabilitiesGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_Version2_NoOnDemand()
    {
        // CC=0x84, Cmd=0x0A
        // Min=0x00012C (300), Max=0x015180 (86400), Default=0x000E10 (3600), Step=0x00012C (300)
        byte[] data =
        [
            0x84, 0x0A,
            0x00, 0x01, 0x2C, // Min: 300
            0x01, 0x51, 0x80, // Max: 86400
            0x00, 0x0E, 0x10, // Default: 3600
            0x00, 0x01, 0x2C, // Step: 300
        ];
        CommandClassFrame frame = new(data);

        WakeUpIntervalCapabilities caps =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(300u, caps.MinimumWakeUpIntervalInSeconds);
        Assert.AreEqual(86400u, caps.MaximumWakeUpIntervalInSeconds);
        Assert.AreEqual(3600u, caps.DefaultWakeUpIntervalInSeconds);
        Assert.AreEqual(300u, caps.WakeUpIntervalStepInSeconds);
        Assert.IsNull(caps.SupportsWakeUpOnDemand);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_Version3_WakeUpOnDemandSupported()
    {
        // Same as above + byte 12: 0x01 (Wake Up On Demand = 1)
        byte[] data =
        [
            0x84, 0x0A,
            0x00, 0x01, 0x2C, // Min: 300
            0x01, 0x51, 0x80, // Max: 86400
            0x00, 0x0E, 0x10, // Default: 3600
            0x00, 0x01, 0x2C, // Step: 300
            0x01,             // Wake Up On Demand = true
        ];
        CommandClassFrame frame = new(data);

        WakeUpIntervalCapabilities caps =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(300u, caps.MinimumWakeUpIntervalInSeconds);
        Assert.AreEqual(86400u, caps.MaximumWakeUpIntervalInSeconds);
        Assert.AreEqual(3600u, caps.DefaultWakeUpIntervalInSeconds);
        Assert.AreEqual(300u, caps.WakeUpIntervalStepInSeconds);
        Assert.IsTrue(caps.SupportsWakeUpOnDemand);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_Version3_WakeUpOnDemandNotSupported()
    {
        byte[] data =
        [
            0x84, 0x0A,
            0x00, 0x01, 0x2C, // Min: 300
            0x01, 0x51, 0x80, // Max: 86400
            0x00, 0x0E, 0x10, // Default: 3600
            0x00, 0x01, 0x2C, // Step: 300
            0x00,             // Wake Up On Demand = false
        ];
        CommandClassFrame frame = new(data);

        WakeUpIntervalCapabilities caps =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(caps.SupportsWakeUpOnDemand);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_Version3_ReservedBitsSet()
    {
        // Byte 12: 0xFF — reserved bits set, but bit 0 = 1 → WakeUpOnDemand = true
        byte[] data =
        [
            0x84, 0x0A,
            0x00, 0x01, 0x2C,
            0x01, 0x51, 0x80,
            0x00, 0x0E, 0x10,
            0x00, 0x01, 0x2C,
            0xFF,
        ];
        CommandClassFrame frame = new(data);

        WakeUpIntervalCapabilities caps =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(caps.SupportsWakeUpOnDemand);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_Version3_ReservedBitsSetOnDemandFalse()
    {
        // Byte 12: 0xFE — reserved bits set, but bit 0 = 0 → WakeUpOnDemand = false
        byte[] data =
        [
            0x84, 0x0A,
            0x00, 0x01, 0x2C,
            0x01, 0x51, 0x80,
            0x00, 0x0E, 0x10,
            0x00, 0x01, 0x2C,
            0xFE,
        ];
        CommandClassFrame frame = new(data);

        WakeUpIntervalCapabilities caps =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(caps.SupportsWakeUpOnDemand);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_AllZeros()
    {
        // All zeros = event-based wake up only (no timer)
        byte[] data =
        [
            0x84, 0x0A,
            0x00, 0x00, 0x00, // Min: 0
            0x00, 0x00, 0x00, // Max: 0
            0x00, 0x00, 0x00, // Default: 0
            0x00, 0x00, 0x00, // Step: 0
        ];
        CommandClassFrame frame = new(data);

        WakeUpIntervalCapabilities caps =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(0u, caps.MinimumWakeUpIntervalInSeconds);
        Assert.AreEqual(0u, caps.MaximumWakeUpIntervalInSeconds);
        Assert.AreEqual(0u, caps.DefaultWakeUpIntervalInSeconds);
        Assert.AreEqual(0u, caps.WakeUpIntervalStepInSeconds);
        Assert.IsNull(caps.SupportsWakeUpOnDemand);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_MaxValues()
    {
        // All 24-bit fields at maximum
        byte[] data =
        [
            0x84, 0x0A,
            0xFF, 0xFF, 0xFF, // Min: 16777215
            0xFF, 0xFF, 0xFF, // Max: 16777215
            0xFF, 0xFF, 0xFF, // Default: 16777215
            0xFF, 0xFF, 0xFF, // Step: 16777215
        ];
        CommandClassFrame frame = new(data);

        WakeUpIntervalCapabilities caps =
            WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(16777215u, caps.MinimumWakeUpIntervalInSeconds);
        Assert.AreEqual(16777215u, caps.MaximumWakeUpIntervalInSeconds);
        Assert.AreEqual(16777215u, caps.DefaultWakeUpIntervalInSeconds);
        Assert.AreEqual(16777215u, caps.WakeUpIntervalStepInSeconds);
    }

    [TestMethod]
    public void IntervalCapabilitiesReport_Parse_TooShort_Throws()
    {
        // Only 11 parameter bytes, need 12
        byte[] data =
        [
            0x84, 0x0A,
            0x00, 0x01, 0x2C,
            0x01, 0x51, 0x80,
            0x00, 0x0E, 0x10,
            0x00, 0x01,
        ];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => WakeUpCommandClass.WakeUpIntervalCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }
}
