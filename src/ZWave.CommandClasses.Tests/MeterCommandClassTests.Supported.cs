using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MeterCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        var command = MeterCommandClass.MeterSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.Meter, MeterCommandClass.MeterSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)MeterCommand.SupportedGet, MeterCommandClass.MeterSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReport_Parse_Electric_NoMst()
    {
        // byte0: reset=0, rate=0, type=Electric(1) -> 0x01
        // byte1: mst=0, scale bytes: bits 0 and 2 -> 0x05 (kWh + W)
        byte[] data = [0x32, 0x04, 0x01, 0x05];
        CommandClassFrame frame = new(data);

        MeterSupportedReport report = MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Electric, report.Type);
        Assert.AreEqual(MeterRateType.Unspecified, report.RateType);
        Assert.IsFalse(report.ResetSupported);
        Assert.HasCount(2, report.SupportedScales);
        Assert.Contains(MeterScale.kWh, report.SupportedScales);
        Assert.Contains(MeterScale.W, report.SupportedScales);
    }

    [TestMethod]
    public void SupportedReport_Parse_Electric_WithMst_KVar()
    {
        // byte0: reset=1, rate=3 (both), type=Electric -> 0xE1
        // byte1: mst=1, first scale byte bit 0 -> 0x81 (kWh)
        // byte2: count = 1
        // byte3: additional byte bit 0 -> index 7 = kVar -> 0x01
        byte[] data = [0x32, 0x04, 0xE1, 0x81, 0x01, 0x01];
        CommandClassFrame frame = new(data);

        MeterSupportedReport report = MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterRateType.Both, report.RateType);
        Assert.IsTrue(report.ResetSupported);
        Assert.HasCount(2, report.SupportedScales);
        Assert.Contains(MeterScale.kWh, report.SupportedScales);
        Assert.Contains(MeterScale.kVar, report.SupportedScales);
    }

    [TestMethod]
    public void SupportedReport_Parse_Gas_ReservedBitsIgnored()
    {
        // byte0: reset=0, rate=1 (import), type=Gas(2) -> 0x22
        // byte1: mst=0, scale bits 0,1,2,3 -> 0x0F; index 2 is reserved for gas and must be ignored
        byte[] data = [0x32, 0x04, 0x22, 0x0F];
        CommandClassFrame frame = new(data);

        MeterSupportedReport report = MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Gas, report.Type);
        Assert.AreEqual(MeterRateType.Import, report.RateType);
        Assert.HasCount(3, report.SupportedScales);
        Assert.Contains(MeterScale.CubicMeters, report.SupportedScales);
        Assert.Contains(MeterScale.CubicFeet, report.SupportedScales);
        Assert.Contains(MeterScale.PulseCount, report.SupportedScales);
    }

    [TestMethod]
    public void SupportedReport_Parse_Empty_Throws()
    {
        byte[] data = [0x32, 0x04];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x32, 0x04, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_UnknownMeterType_Throws()
    {
        byte[] data = [0x32, 0x04, 0x06, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_MstButNoCountByte_Throws()
    {
        // mst=1 but there is no count byte
        byte[] data = [0x32, 0x04, 0x81, 0x81];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Parse_MstCountExceedsPayload_Throws()
    {
        // mst=1, count=3 but only 1 additional byte provided
        byte[] data = [0x32, 0x04, 0x81, 0x81, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReport_Create_NoMst_RoundTrips()
    {
        var command = MeterCommandClass.MeterSupportedReportCommand.Create(
            meterType: MeterType.Gas,
            rateType: MeterRateType.Import,
            resetSupported: false,
            supportedScales: new HashSet<MeterScale> { MeterScale.CubicMeters, MeterScale.CubicFeet });

        MeterSupportedReport report = MeterCommandClass.MeterSupportedReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Gas, report.Type);
        Assert.AreEqual(MeterRateType.Import, report.RateType);
        Assert.IsFalse(report.ResetSupported);
        Assert.HasCount(2, report.SupportedScales);
        Assert.Contains(MeterScale.CubicMeters, report.SupportedScales);
        Assert.Contains(MeterScale.CubicFeet, report.SupportedScales);
    }

    [TestMethod]
    public void SupportedReport_Create_WithMst_RoundTrips()
    {
        var command = MeterCommandClass.MeterSupportedReportCommand.Create(
            meterType: MeterType.Electric,
            rateType: MeterRateType.Both,
            resetSupported: true,
            supportedScales: new HashSet<MeterScale> { MeterScale.kWh, MeterScale.kVar });

        MeterSupportedReport report = MeterCommandClass.MeterSupportedReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(MeterRateType.Both, report.RateType);
        Assert.IsTrue(report.ResetSupported);
        Assert.HasCount(2, report.SupportedScales);
        Assert.Contains(MeterScale.kWh, report.SupportedScales);
        Assert.Contains(MeterScale.kVar, report.SupportedScales);
    }
}
