using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MeterCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_Version1_NoParams()
    {
        var command = MeterCommandClass.MeterGetCommand.Create(version: 1, rateType: null, scale: null, scale2: null);

        Assert.AreEqual(CommandClassId.Meter, MeterCommandClass.MeterGetCommand.CommandClassId);
        Assert.AreEqual((byte)MeterCommand.Get, MeterCommandClass.MeterGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void GetCommand_Create_Version2_DefaultScale_SingleByte()
    {
        var command = MeterCommandClass.MeterGetCommand.Create(version: 2, rateType: null, scale: null, scale2: null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version2_KVAh_ScaleInBits()
    {
        // scale value 1 (kVAh): bits 5-3 = 001 -> 0x08
        var command = MeterCommandClass.MeterGetCommand.Create(version: 2, rateType: null, scale: 1, scale2: null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0b0000_1000, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version3_W_ScaleInThreeBits()
    {
        // scale value 2 (W): bits 5-3 = 010 -> 0x10
        var command = MeterCommandClass.MeterGetCommand.Create(version: 3, rateType: null, scale: 2, scale2: null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0b0001_0000, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version3_RateTypeRequested_Throws()
    {
        // The Rate Type field only exists at V4+, so it cannot be requested at V3.
        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterGetCommand.Create(version: 3, rateType: (byte)MeterRateType.Import, scale: 0, scale2: null));
    }

    [TestMethod]
    public void GetCommand_Create_Version1_ScaleRequested_Throws()
    {
        // The V1 Meter Get has no parameters, so a specific scale cannot be requested.
        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterGetCommand.Create(version: 1, rateType: null, scale: 0, scale2: null));
    }

    [TestMethod]
    public void GetCommand_Create_Version1_RateTypeRequested_Throws()
    {
        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterGetCommand.Create(version: 1, rateType: (byte)MeterRateType.Import, scale: null, scale2: null));
    }

    [TestMethod]
    public void GetCommand_Create_Version2_ScaleOutOfRange_Throws()
    {
        // V2 has a 2-bit Scale field (values 0-3); scale 4 (V) is not representable.
        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterGetCommand.Create(version: 2, rateType: null, scale: 4, scale2: null));
    }

    [TestMethod]
    public void GetCommand_Create_Version2_RateTypeRequested_Throws()
    {
        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterGetCommand.Create(version: 2, rateType: (byte)MeterRateType.Import, scale: null, scale2: null));
    }

    [TestMethod]
    public void GetCommand_Create_Version2_MaxScale_EncodesBits()
    {
        // Boundary: scale 3 (Pulse count) fits the 2-bit field -> bits 5-3 = 011 -> 0x18.
        var command = MeterCommandClass.MeterGetCommand.Create(version: 2, rateType: null, scale: 3, scale2: null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0b0001_1000, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version3_MaxScale_EncodesBits()
    {
        // Boundary: scale 6 (Power factor) fits the 3-bit field -> bits 5-3 = 110 -> 0x30.
        var command = MeterCommandClass.MeterGetCommand.Create(version: 3, rateType: null, scale: 6, scale2: null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0b0011_0000, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version4_MS_T_WithScale2_Succeeds()
    {
        // V4 introduces the M.S.T scale (7) together with the Scale 2 field.
        var command = MeterCommandClass.MeterGetCommand.Create(version: 4, rateType: null, scale: 7, scale2: 1);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(0b0011_1000, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_Version4_Import_KWh()
    {
        // rate type 1 (import): bits 7-6 = 01 -> 0x40; scale 0
        var command = MeterCommandClass.MeterGetCommand.Create(version: 4, rateType: (byte)MeterRateType.Import, scale: 0, scale2: null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0b0100_0000, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version4_Export_V()
    {
        // rate type 2 (export): bits 7-6 = 10 -> 0x80; scale 4 (V): bits 5-3 = 100 -> 0x20
        var command = MeterCommandClass.MeterGetCommand.Create(version: 4, rateType: (byte)MeterRateType.Export, scale: 4, scale2: null);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0b1010_0000, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_Version6_KVar_IncludesScale2()
    {
        // scale value 7 (M.S.T) with scale2 = 0 (kVar): byte0 = 0x38, scale2 byte = 0x00
        var command = MeterCommandClass.MeterGetCommand.Create(version: 6, rateType: null, scale: 7, scale2: 0);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(0b0011_1000, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_Version6_KVarh_Scale2IsOne()
    {
        // scale value 7 with scale2 = 1 (kVarh): byte0 = 0x38, scale2 byte = 0x01
        var command = MeterCommandClass.MeterGetCommand.Create(version: 6, rateType: null, scale: 7, scale2: 1);

        Assert.AreEqual(0b0011_1000, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_Version3_MS_T_Throws()
    {
        // The M.S.T scale (7) does not exist before V4, so a V3 Get cannot request it.
        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterGetCommand.Create(version: 3, rateType: null, scale: 7, scale2: 0));
    }

    [TestMethod]
    public void Report_Parse_KWh_Precision2_DeltaTimeZero_NoPrevious()
    {
        // V2+ frame: the Delta Time field is present but set to 0x0000 (no Previous Meter Value).
        // byte0: scale(2)=0, rate=0, type=Electric(1) -> 0x01
        // byte1: precision=2, scale(1:0)=0, size=2 -> 0x42
        // value: 1025 = 0x04 0x01 -> 10.25 ; delta: 0x0000
        byte[] data = [0x32, 0x02, 0x01, 0x42, 0x04, 0x01, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Electric, report.Type);
        Assert.AreEqual(MeterRateType.Unspecified, report.RateType);
        Assert.AreEqual(MeterScale.kWh, report.Scale);
        Assert.AreEqual(10.25, report.Value, 0.001);
        Assert.IsNull(report.DeltaTime);
        Assert.IsNull(report.PreviousValue);
    }

    [TestMethod]
    public void Report_Parse_V1_NoDeltaTime_Succeeds()
    {
        // V1 reports end after the Meter Value; the Delta Time and Previous Meter Value fields do not exist.
        // byte0: scale(2)=0, rate=0, type=Electric(1) -> 0x01
        // byte1: precision=2, scale(1:0)=0, size=2 -> 0x42
        // value: 1025 = 0x04 0x01 -> 10.25
        byte[] data = [0x32, 0x02, 0x01, 0x42, 0x04, 0x01];
        CommandClassFrame frame = new(data);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Electric, report.Type);
        Assert.AreEqual(MeterRateType.Unspecified, report.RateType);
        Assert.AreEqual(MeterScale.kWh, report.Scale);
        Assert.AreEqual(10.25, report.Value, 0.001);
        Assert.IsNull(report.DeltaTime);
        Assert.IsNull(report.PreviousValue);
    }

    [TestMethod]
    public void Report_Parse_SingleTrailingByte_Throws()
    {
        // One byte after the Meter Value is neither a valid V1 ending nor a complete Delta Time field.
        // byte0: type=Electric(1) -> 0x01
        // byte1: precision=0, scale(1:0)=0, size=1 -> 0x01
        // value: 1 (0x01) ; trailing byte: 0x00
        byte[] data = [0x32, 0x02, 0x01, 0x01, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_MST_MissingScale2_Throws()
    {
        // The 3-bit scale field is 7 (M.S.T) but the frame ends after the value, so the
        // mandatory Scale 2 byte is missing.
        // byte0: scale(2)=1, rate=0, type=Electric(1) -> 0x81
        // byte1: precision=0, scale(1:0)=3, size=1 -> 0x19
        // value: 5 (0x05)
        byte[] data = [0x32, 0x02, 0x81, 0x19, 0x05];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_W_Import_WithDeltaAndPrevious()
    {
        // byte0: rate=1 (import), type=Electric -> 0x21
        // byte1: precision=0, scale(1:0)=2 (W), size=1 -> 0x11
        // value: 100 (0x64) ; delta: 3600 (0x0E 0x10) ; previous: 80 (0x50)
        byte[] data = [0x32, 0x02, 0x21, 0x11, 0x64, 0x0E, 0x10, 0x50];
        CommandClassFrame frame = new(data);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterRateType.Import, report.RateType);
        Assert.AreEqual(MeterScale.W, report.Scale);
        Assert.AreEqual(100.0, report.Value, 0.001);
        Assert.AreEqual(TimeSpan.FromSeconds(3600), report.DeltaTime);
        Assert.AreEqual(80.0, report.PreviousValue.GetValueOrDefault(), 0.001);
    }

    [TestMethod]
    public void Report_Parse_MS_T_Scale2_KVar()
    {
        // byte0: scale(2)=1, rate=0, type=Electric -> 0x81
        // byte1: precision=1, scale(1:0)=3, size=2 -> 0x3A
        // value: 150 (0x00 0x96) -> 15.0 ; delta: 0x0000 ; scale2: 0x00
        byte[] data = [0x32, 0x02, 0x81, 0x3A, 0x00, 0x96, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterScale.kVar, report.Scale);
        Assert.AreEqual(15.0, report.Value, 0.001);
    }

    [TestMethod]
    public void Report_Parse_Gas_Negative_4Byte_UnknownDelta()
    {
        // byte0: type=Gas(2) -> 0x02
        // byte1: precision=0, scale(1:0)=0, size=4 -> 0x04
        // value: -5 (0xFF 0xFF 0xFF 0xFB) ; delta: 0xFFFF (unknown) ; previous: -10 (0xFF 0xFF 0xFF 0xF6)
        byte[] data =
        [
            0x32, 0x02, 0x02, 0x04,
            0xFF, 0xFF, 0xFF, 0xFB,
            0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xF6,
        ];
        CommandClassFrame frame = new(data);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Gas, report.Type);
        Assert.AreEqual(MeterScale.CubicMeters, report.Scale);
        Assert.AreEqual(-5.0, report.Value, 0.001);
        Assert.IsNull(report.DeltaTime);
        Assert.AreEqual(-10.0, report.PreviousValue.GetValueOrDefault(), 0.001);
    }

    [TestMethod]
    public void Report_Parse_Water_USGallons_Precision3_Export()
    {
        // byte0: rate=2 (export), type=Water(3) -> 0x43
        // byte1: precision=3, scale(1:0)=2, size=2 -> 0x72
        // value: 456 (0x01 0xC8) -> 0.456 ; delta: 0x0000
        byte[] data = [0x32, 0x02, 0x43, 0x72, 0x01, 0xC8, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Water, report.Type);
        Assert.AreEqual(MeterRateType.Export, report.RateType);
        Assert.AreEqual(MeterScale.USGallons, report.Scale);
        Assert.AreEqual(0.456, report.Value, 0.0001);
    }

    [TestMethod]
    public void Report_Parse_EmptyCommandParameters_Throws()
    {
        byte[] data = [0x32, 0x02];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        byte[] data = [0x32, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_InvalidSize_Throws()
    {
        // size = 3 (invalid); value byte follows but 3 is not 1/2/4
        byte[] data = [0x32, 0x02, 0x01, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_ValueSizeExceedsPayload_Throws()
    {
        // size = 4 but only 1 value byte provided
        byte[] data = [0x32, 0x02, 0x01, 0x04, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_UnknownMeterType_Throws()
    {
        // type = 6 (reserved)
        byte[] data = [0x32, 0x02, 0x06, 0x01, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_UnsupportedScaleForType_Throws()
    {
        // Gas with scale index 2 (reserved for gas)
        byte[] data = [0x32, 0x02, 0x02, 0x11, 0x01, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_PreviousValueMissing_Throws()
    {
        // delta = 3600 (non-zero) but no previous value bytes
        byte[] data = [0x32, 0x02, 0x01, 0x01, 0x01, 0x0E, 0x10];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Create_ParsesBackToSameValues()
    {
        var command = MeterCommandClass.MeterReportCommand.Create(
            meterType: MeterType.Electric,
            rateType: MeterRateType.Import,
            scale: MeterScale.W,
            precision: 1,
            value: 12.3,
            size: 2,
            deltaTime: TimeSpan.FromSeconds(120),
            previousValue: 10.1);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(MeterType.Electric, report.Type);
        Assert.AreEqual(MeterRateType.Import, report.RateType);
        Assert.AreEqual(MeterScale.W, report.Scale);
        Assert.AreEqual(12.3, report.Value, 0.0001);
        Assert.AreEqual(TimeSpan.FromSeconds(120), report.DeltaTime);
        Assert.AreEqual(10.1, report.PreviousValue.GetValueOrDefault(), 0.0001);
    }

    [TestMethod]
    public void Report_Create_MS_T_Scale2_RoundTrips()
    {
        var command = MeterCommandClass.MeterReportCommand.Create(
            meterType: MeterType.Electric,
            rateType: MeterRateType.Unspecified,
            scale: MeterScale.kVarh,
            precision: 0,
            value: 5,
            size: 1,
            deltaTime: null,
            previousValue: null);

        MeterReport report = MeterCommandClass.MeterReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(MeterScale.kVarh, report.Scale);
        Assert.AreEqual(5.0, report.Value, 0.001);
        Assert.IsNull(report.DeltaTime);
    }

    [TestMethod]
    public void Report_MatchesRequest_ExplicitScale_MatchesOnlyOwnReport()
    {
        var reportW = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.W, 0, 100, 1, null, null);
        var reportKVAh = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Import, MeterScale.kVAh, 1, 12.3, 2, null, null);

        // Each report matches only the request that asked for it.
        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(reportW.Frame, scale: 2, scale2: null, rateType: null));
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(reportKVAh.Frame, scale: 2, scale2: null, rateType: null));
        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(reportKVAh.Frame, scale: 1, scale2: null, rateType: (byte)MeterRateType.Import));
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(reportW.Frame, scale: 1, scale2: null, rateType: (byte)MeterRateType.Import));
    }

    [TestMethod]
    public void Report_MatchesRequest_DefaultRequest_MatchesAnyReport()
    {
        var report = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Import, MeterScale.W, 0, 100, 1, null, null);

        // A scale of null or 0 requests the device default; no scale matching is attempted.
        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(report.Frame, scale: null, scale2: null, rateType: null));
        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(report.Frame, scale: 0, scale2: null, rateType: null));
        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(report.Frame, scale: null, scale2: null, rateType: (byte)MeterRateType.Import));
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(report.Frame, scale: null, scale2: null, rateType: (byte)MeterRateType.Export));
    }

    [TestMethod]
    public void Report_MatchesRequest_MST_MatchesScale2AtComputedPosition()
    {
        // kVar (scale 7, Scale 2 = 0) with a previous value present; kVarh (scale 7, Scale 2 = 1) without.
        var reportKVar = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kVar, 1, 3.5, 2, TimeSpan.FromSeconds(60), 2.0);
        var reportKVarh = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kVarh, 0, 7, 1, null, null);

        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(reportKVar.Frame, scale: 7, scale2: 0, rateType: null));
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(reportKVar.Frame, scale: 7, scale2: 1, rateType: null));
        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(reportKVarh.Frame, scale: 7, scale2: 1, rateType: null));
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(reportKVarh.Frame, scale: 7, scale2: 0, rateType: null));
    }

    [TestMethod]
    public void Report_MatchesRequest_MST_TruncatedOrMalformed_ReturnsFalse()
    {
        // M.S.T report missing the Delta Time / Scale 2 bytes.
        byte[] truncated = [0x32, 0x02, 0x81, 0x19, 0x07, 0x00, 0x00];
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(new CommandClassFrame(truncated), scale: 7, scale2: 0, rateType: null));

        // M.S.T with an invalid value size (6).
        byte[] badSize = [0x32, 0x02, 0x81, 0x16, 0x07, 0x00, 0x00];
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(new CommandClassFrame(badSize), scale: 7, scale2: 0, rateType: null));

        // Header too short.
        byte[] shortFrame = [0x32, 0x02, 0x81];
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(new CommandClassFrame(shortFrame), scale: 7, scale2: 0, rateType: null));
    }

    [TestMethod]
    public void Report_MatchesRequest_ExplicitRateType_MatchesOnlyOwnReport()
    {
        var import = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Import, MeterScale.W, 0, 1, 1, null, null);
        var export = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Export, MeterScale.W, 0, 2, 1, null, null);

        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(import.Frame, scale: null, scale2: null, rateType: (byte)MeterRateType.Import));
        Assert.IsFalse(MeterCommandClass.MeterReportCommand.MatchesRequest(export.Frame, scale: null, scale2: null, rateType: (byte)MeterRateType.Import));
        Assert.IsTrue(MeterCommandClass.MeterReportCommand.MatchesRequest(export.Frame, scale: null, scale2: null, rateType: (byte)MeterRateType.Export));
    }

    [TestMethod]
    public async Task GetAsync_ConcurrentGets_ReversedReports_EachCompletesWithOwnReport()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        // Both GetAsync calls register their report awaiter synchronously before returning (the
        // recording driver completes inline), so the reports below can be delivered directly.
        Task<MeterReport> taskW = meter.GetAsync(MeterType.Electric, MeterScale.W, rateType: null, CancellationToken.None);
        Task<MeterReport> taskKVAh = meter.GetAsync(MeterType.Electric, MeterScale.kVAh, MeterRateType.Import, CancellationToken.None);

        var reportW = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.W, 0, 100, 1, null, null);
        var reportKVAh = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Import, MeterScale.kVAh, 1, 12.3, 2, null, null);

        // Deliver in reversed order: the kVAh report first.
        meter.ProcessCommand(reportKVAh.Frame);
        meter.ProcessCommand(reportW.Frame);

        MeterReport w = await taskW;
        MeterReport kVAh = await taskKVAh;

        Assert.AreEqual(MeterScale.W, w.Scale);
        Assert.AreEqual(100.0, w.Value, 0.001);
        Assert.AreEqual(MeterScale.kVAh, kVAh.Scale);
        Assert.AreEqual(MeterRateType.Import, kVAh.RateType);
        Assert.AreEqual(12.3, kVAh.Value, 0.001);

        Assert.HasCount(2, driver.SentFrames);
        Assert.AreEqual(0b0001_0000, driver.SentFrames[0].CommandParameters.Span[0]); // W (scale 2)
        Assert.AreEqual(0b0100_1000, driver.SentFrames[1].CommandParameters.Span[0]); // kVAh + Import
    }

    [TestMethod]
    public async Task GetAsync_DefaultGetInFlight_ExplicitGet_Throws()
    {
        // A default Get (unconstrained scale) matches any report, so an explicit-scale Get issued
        // while it is in flight could be answered with the wrong report; it must be rejected instead.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport> taskDefault = meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None);
        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.GetAsync(MeterType.Electric, MeterScale.W, rateType: null, CancellationToken.None));

        Assert.HasCount(1, driver.SentFrames);

        // Complete the in-flight default Get so its awaiter is cleaned up.
        var report = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kWh, 0, 10, 1, null, null);
        meter.ProcessCommand(report.Frame);
        await taskDefault;
    }

    [TestMethod]
    public async Task GetAsync_ExplicitGetInFlight_DefaultGet_Throws()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport> taskW = meter.GetAsync(MeterType.Electric, MeterScale.W, rateType: null, CancellationToken.None);
        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None));

        Assert.HasCount(1, driver.SentFrames);

        var report = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.W, 0, 100, 1, null, null);
        meter.ProcessCommand(report.Frame);
        await taskW;
    }

    [TestMethod]
    public async Task GetAsync_DuplicateGetInFlight_Throws()
    {
        // An identical duplicate is rejected rather than coalesced: the reports could not be
        // attributed, and a shared wait would not offer independent cancellation.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport> taskA = meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None);
        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None));

        Assert.HasCount(1, driver.SentFrames);

        var report = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kWh, 0, 10, 1, null, null);
        meter.ProcessCommand(report.Frame);
        await taskA;
    }

    [TestMethod]
    public async Task GetAsync_ExplicitKWh_ConflictsWithDefault_Throws()
    {
        // Scale 0 (kWh) is the device default on the wire, so it conflicts with a default Get.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport> taskKWh = meter.GetAsync(MeterType.Electric, MeterScale.kWh, rateType: null, CancellationToken.None);
        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None));

        Assert.HasCount(1, driver.SentFrames);

        var report = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kWh, 0, 10, 1, null, null);
        meter.ProcessCommand(report.Frame);
        await taskKWh;
    }

    [TestMethod]
    public async Task GetAsync_SameScale_RateDefaultVsImport_Throws()
    {
        // Same concrete scale, but one side requests the default rate: the default rate is a
        // specific (unknown) value, so the two requests' reports cannot be told apart.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport> taskW = meter.GetAsync(MeterType.Electric, MeterScale.W, rateType: null, CancellationToken.None);
        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.GetAsync(MeterType.Electric, MeterScale.W, MeterRateType.Import, CancellationToken.None));

        Assert.HasCount(1, driver.SentFrames);

        var report = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.W, 0, 100, 1, null, null);
        meter.ProcessCommand(report.Frame);
        await taskW;
    }

    [TestMethod]
    public async Task GetAsync_SameScale_DifferentRates_CompleteSeparately()
    {
        // Two different concrete rates on the same scale are distinguishable.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport> taskImport = meter.GetAsync(MeterType.Electric, MeterScale.W, MeterRateType.Import, CancellationToken.None);
        Task<MeterReport> taskExport = meter.GetAsync(MeterType.Electric, MeterScale.W, MeterRateType.Export, CancellationToken.None);

        Assert.HasCount(2, driver.SentFrames);

        var reportImport = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Import, MeterScale.W, 0, 100, 1, null, null);
        // 200 does not fit a signed 1-byte value; use size 2.
        var reportExport = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Export, MeterScale.W, 0, 200, 2, null, null);

        meter.ProcessCommand(reportImport.Frame);
        meter.ProcessCommand(reportExport.Frame);

        MeterReport import = await taskImport;
        MeterReport export = await taskExport;

        Assert.AreEqual(MeterRateType.Import, import.RateType);
        Assert.AreEqual(100.0, import.Value, 0.001);
        Assert.AreEqual(MeterRateType.Export, export.RateType);
        Assert.AreEqual(200.0, export.Value, 0.001);
    }

    [TestMethod]
    public async Task GetAsync_MS_T_DifferentScale2_CompleteSeparately()
    {
        // M.S.T requests differ in their Scale 2 value, which is transmitted, so they are distinguishable.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport> taskKVar = meter.GetAsync(MeterType.Electric, MeterScale.kVar, rateType: null, CancellationToken.None);
        Task<MeterReport> taskKVarh = meter.GetAsync(MeterType.Electric, MeterScale.kVarh, rateType: null, CancellationToken.None);

        Assert.HasCount(2, driver.SentFrames);

        var reportKVar = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kVar, 0, 5, 1, null, null);
        var reportKVarh = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kVarh, 0, 6, 1, null, null);

        meter.ProcessCommand(reportKVarh.Frame);
        meter.ProcessCommand(reportKVar.Frame);

        MeterReport kVar = await taskKVar;
        MeterReport kVarh = await taskKVarh;

        Assert.AreEqual(MeterScale.kVar, kVar.Scale);
        Assert.AreEqual(MeterScale.kVarh, kVarh.Scale);
    }

    [TestMethod]
    public async Task GetAsync_FollowUpGetFromReportEvent_Succeeds()
    {
        // The in-flight slot is released before the report event fires, so a handler reacting to
        // the report can immediately issue a follow-up Get.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport>? followUp = null;
        // One-shot: the event also fires when the follow-up's own report arrives.
        meter.OnMeterReportReceived += _ =>
        {
            if (followUp is null)
            {
                followUp = meter.GetAsync(MeterType.Electric, MeterScale.W, rateType: null, CancellationToken.None);
            }
        };

        Task<MeterReport> taskDefault = meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None);

        var reportKWh = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kWh, 0, 10, 1, null, null);
        meter.ProcessCommand(reportKWh.Frame);
        MeterReport defaultReport = await taskDefault;

        Assert.AreEqual(MeterScale.kWh, defaultReport.Scale);
        Assert.IsNotNull(followUp);

        var reportW = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.W, 0, 100, 1, null, null);
        meter.ProcessCommand(reportW.Frame);
        MeterReport w = await followUp;

        Assert.AreEqual(MeterScale.W, w.Scale);
        Assert.HasCount(2, driver.SentFrames);
    }

    [TestMethod]
    public async Task GetAsync_SameSignatureFollowUpFromReportEvent_WaitsForNewReport()
    {
        // A same-signature follow-up started from the report event must wait for a new report
        // (the dispatched report already belongs to the completing Get), and it must keep the
        // in-flight slot so that conflicting Gets are still rejected.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        Task<MeterReport>? followUp = null;
        // One-shot: the event also fires when the follow-up's own report arrives.
        meter.OnMeterReportReceived += _ =>
        {
            if (followUp is null)
            {
                followUp = meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None);
            }
        };

        Task<MeterReport> taskFirst = meter.GetAsync(type: null, scale: null, rateType: null, CancellationToken.None);

        var reportFirst = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kWh, 0, 10, 1, null, null);
        meter.ProcessCommand(reportFirst.Frame);
        MeterReport first = await taskFirst;

        Assert.AreEqual(10.0, first.Value, 0.001);
        Assert.IsNotNull(followUp);

        // The dispatched report was already consumed by the first Get; the follow-up waits for a new one.
        Assert.IsFalse(followUp.IsCompleted);

        // The follow-up owns the in-flight slot; the first Get's cleanup must not have removed it.
        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.GetAsync(MeterType.Electric, MeterScale.W, rateType: null, CancellationToken.None));

        var reportSecond = MeterCommandClass.MeterReportCommand.Create(MeterType.Electric, MeterRateType.Unspecified, MeterScale.kWh, 0, 20, 1, null, null);
        meter.ProcessCommand(reportSecond.Frame);
        MeterReport second = await followUp;

        Assert.AreEqual(20.0, second.Value, 0.001);
        Assert.HasCount(2, driver.SentFrames);
    }
}
