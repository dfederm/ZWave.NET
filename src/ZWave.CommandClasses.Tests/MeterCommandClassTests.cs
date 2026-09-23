using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public partial class MeterCommandClassTests
{
    [TestMethod]
    public void MeterScaleHelper_ResolveScale_Electric()
    {
        Assert.AreEqual(MeterScale.kWh, MeterScaleHelper.ResolveScale(MeterType.Electric, 0));
        Assert.AreEqual(MeterScale.kVAh, MeterScaleHelper.ResolveScale(MeterType.Electric, 1));
        Assert.AreEqual(MeterScale.W, MeterScaleHelper.ResolveScale(MeterType.Electric, 2));
        Assert.AreEqual(MeterScale.PulseCount, MeterScaleHelper.ResolveScale(MeterType.Electric, 3));
        Assert.AreEqual(MeterScale.V, MeterScaleHelper.ResolveScale(MeterType.Electric, 4));
        Assert.AreEqual(MeterScale.A, MeterScaleHelper.ResolveScale(MeterType.Electric, 5));
        Assert.AreEqual(MeterScale.PowerFactor, MeterScaleHelper.ResolveScale(MeterType.Electric, 6));
        Assert.AreEqual(MeterScale.kVar, MeterScaleHelper.ResolveScale(MeterType.Electric, 7));
        Assert.AreEqual(MeterScale.kVarh, MeterScaleHelper.ResolveScale(MeterType.Electric, 8));
    }

    [TestMethod]
    public void MeterScaleHelper_ResolveScale_Reserved_ReturnsNull()
    {
        // Gas index 2 is reserved.
        Assert.IsNull(MeterScaleHelper.ResolveScale(MeterType.Gas, 2));
        // Water index 4 is reserved.
        Assert.IsNull(MeterScaleHelper.ResolveScale(MeterType.Water, 4));
        // Heating/Cooling only support kWh (index 0).
        Assert.IsNull(MeterScaleHelper.ResolveScale(MeterType.Heating, 1));
        Assert.IsNull(MeterScaleHelper.ResolveScale(MeterType.Cooling, 3));
        // Out-of-range index.
        Assert.IsNull(MeterScaleHelper.ResolveScale(MeterType.Electric, 15));
    }

    [TestMethod]
    public void MeterScaleHelper_TryGetEncoding_Electric()
    {
        Assert.IsTrue(MeterScaleHelper.TryGetEncoding(MeterType.Electric, MeterScale.kWh, out byte scale, out byte? scale2));
        Assert.AreEqual(0, scale);
        Assert.IsNull(scale2);

        Assert.IsTrue(MeterScaleHelper.TryGetEncoding(MeterType.Electric, MeterScale.kVar, out scale, out scale2));
        Assert.AreEqual(7, scale);
        Assert.AreEqual((byte)0, scale2);

        Assert.IsTrue(MeterScaleHelper.TryGetEncoding(MeterType.Electric, MeterScale.kVarh, out scale, out scale2));
        Assert.AreEqual(7, scale);
        Assert.AreEqual((byte)1, scale2);
    }

    [TestMethod]
    public void MeterScaleHelper_TryGetEncoding_InvalidForType_ReturnsFalse()
    {
        // kWh is not a gas scale.
        Assert.IsFalse(MeterScaleHelper.TryGetEncoding(MeterType.Gas, MeterScale.kWh, out byte scale, out byte? scale2));
        Assert.AreEqual(0, scale);
        Assert.IsNull(scale2);
    }

    [TestMethod]
    public void MeterScaleHelper_IsKnownMeterType()
    {
        Assert.IsTrue(MeterScaleHelper.IsKnownMeterType(MeterType.Electric));
        Assert.IsTrue(MeterScaleHelper.IsKnownMeterType(MeterType.Cooling));
        Assert.IsFalse(MeterScaleHelper.IsKnownMeterType((MeterType)0));
        Assert.IsFalse(MeterScaleHelper.IsKnownMeterType((MeterType)6));
    }

    private sealed class FakeEndpoint : IEndpoint
    {
        public ushort NodeId => 1;

        public byte EndpointIndex => 0;

        public IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses => new Dictionary<CommandClassId, CommandClassInfo>();

        public CommandClass GetCommandClass(CommandClassId commandClassId) => throw new NotImplementedException();
    }

    // Records the frames a command class sends; never injects a report, which keeps the
    // report-delivery tests deterministic (reports are delivered directly via ProcessCommand).
    private sealed class RecordingDriver : IDriver
    {
        private readonly List<CommandClassFrame> _sentFrames = [];

        public IReadOnlyList<CommandClassFrame> SentFrames => _sentFrames;

        public Task SendCommandAsync<TCommand>(TCommand command, ushort nodeId, byte endpointIndex, CancellationToken cancellationToken)
            where TCommand : struct, ICommand
        {
            _sentFrames.Add(command.Frame);
            return Task.CompletedTask;
        }

        public INode? GetNode(ushort nodeId) => null;
    }

    private static (MeterCommandClass, RecordingDriver) CreateMeter(byte version)
    {
        var driver = new RecordingDriver();
        var endpoint = new FakeEndpoint();
        var meter = new MeterCommandClass(
            new CommandClassInfo(CommandClassId.Meter, true, false),
            driver,
            endpoint,
            NullLogger.Instance);
        meter.SetVersion(version);
        return (meter, driver);
    }

    // Deterministic Supported Report round-trip: GetSupportedAsync registers its awaiter
    // synchronously, so the report can be delivered directly via ProcessCommand.
    private static async Task AdvertiseResetSupportAsync(MeterCommandClass meter, bool resetSupported)
    {
        Task<MeterSupportedReport> supportedTask = meter.GetSupportedAsync(CancellationToken.None);
        var report = MeterCommandClass.MeterSupportedReportCommand.Create(MeterType.Electric, MeterRateType.Import, resetSupported, new HashSet<MeterScale> { MeterScale.kWh });
        meter.ProcessCommand(report.Frame);
        await supportedTask;
    }
}
