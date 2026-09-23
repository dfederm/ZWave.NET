using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class MeterCommandClassTests
{
    [TestMethod]
    public void ResetCommand_CreateResetAll_NoParams()
    {
        var command = MeterCommandClass.MeterResetCommand.CreateResetAll();

        Assert.AreEqual(CommandClassId.Meter, MeterCommandClass.MeterResetCommand.CommandClassId);
        Assert.AreEqual((byte)MeterCommand.Reset, MeterCommandClass.MeterResetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void ResetCommand_Create_Version5_Targeted_Throws()
    {
        // V2-5 have no parameterized reset; a targeted reset is only available at V6.
        Assert.ThrowsExactly<ZWaveException>(() => MeterCommandClass.MeterResetCommand.Create(version: 5, meterType: MeterType.Water, value: 42));
    }

    [TestMethod]
    public void ResetCommand_Create_Version6_ZeroValue_1Byte()
    {
        // value 0 -> size 1; byte0 = (1 << 5) | Electric(1) = 0x21
        var command = MeterCommandClass.MeterResetCommand.Create(version: 6, meterType: MeterType.Electric, value: 0);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(0b0010_0001, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ResetCommand_Create_Version6_PositiveValue_2Byte()
    {
        // value 1000 -> size 2; byte0 = (2 << 5) | Electric(1) = 0x41
        var command = MeterCommandClass.MeterResetCommand.Create(version: 6, meterType: MeterType.Electric, value: 1000);

        Assert.AreEqual(5, command.Frame.Data.Length);
        Assert.AreEqual(0b0100_0001, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x03, command.Frame.CommandParameters.Span[1]);
        Assert.AreEqual(0xE8, command.Frame.CommandParameters.Span[2]);
    }

    [TestMethod]
    public void ResetCommand_Create_Version6_NegativeValue_1Byte_Water()
    {
        // value -5 -> size 1; byte0 = (1 << 5) | Water(3) = 0x23
        var command = MeterCommandClass.MeterResetCommand.Create(version: 6, meterType: MeterType.Water, value: -5);

        Assert.AreEqual(4, command.Frame.Data.Length);
        Assert.AreEqual(0b0010_0011, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0xFB, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ResetCommand_Create_Version6_LargeValue_4Byte()
    {
        // value = int.MinValue -> size 4; byte0 = (4 << 5) | Gas(2) = 0x82
        var command = MeterCommandClass.MeterResetCommand.Create(version: 6, meterType: MeterType.Gas, value: int.MinValue);

        Assert.AreEqual(7, command.Frame.Data.Length);
        Assert.AreEqual(0b1000_0010, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x80, command.Frame.CommandParameters.Span[1]);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[2]);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[3]);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[4]);
    }

    [TestMethod]
    public async Task ResetAsync_VersionBelow6_ThrowsAndSendsNothing()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 3);

        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.ResetAsync(MeterType.Electric, MeterScale.kWh, MeterRateType.Unspecified, 0, CancellationToken.None));

        Assert.HasCount(0, driver.SentFrames);
    }

    [TestMethod]
    public async Task ResetAsync_Version6_SendsTargetedReset()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);
        await AdvertiseResetSupportAsync(meter, resetSupported: true);

        await meter.ResetAsync(MeterType.Electric, MeterScale.kWh, MeterRateType.Unspecified, 1000, CancellationToken.None);

        // The first sent frame is the SupportedGet from AdvertiseResetSupportAsync.
        CommandClassFrame resetFrame = driver.SentFrames[^1];
        Assert.AreEqual(5, resetFrame.Data.Length);
        Assert.AreEqual(0b0100_0001, resetFrame.CommandParameters.Span[0]); // size 2, Electric
        Assert.AreEqual(0x03, resetFrame.CommandParameters.Span[1]);
        Assert.AreEqual(0xE8, resetFrame.CommandParameters.Span[2]);
    }

    [TestMethod]
    public async Task ResetAllAsync_Version3_SendsParameterlessReset()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 3);
        await AdvertiseResetSupportAsync(meter, resetSupported: true);

        await meter.ResetAllAsync(CancellationToken.None);

        // The first sent frame is the SupportedGet from AdvertiseResetSupportAsync.
        CommandClassFrame resetFrame = driver.SentFrames[^1];
        Assert.AreEqual(2, resetFrame.Data.Length);
    }

    [TestMethod]
    public async Task ResetAllAsync_Version6_ThrowsAndSendsNothing()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        await Assert.ThrowsExactlyAsync<ZWaveException>(() => meter.ResetAllAsync(CancellationToken.None));

        Assert.HasCount(0, driver.SentFrames);
    }

    [TestMethod]
    public async Task ResetAsync_Version6_SupportNotDiscovered_ThrowsAndSendsNothing()
    {
        // The version is known but the Supported Report has not been read, so Reset support is unknown.
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);

        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.ResetAsync(MeterType.Electric, MeterScale.kWh, MeterRateType.Unspecified, 0, CancellationToken.None));

        Assert.HasCount(0, driver.SentFrames);
    }

    [TestMethod]
    public async Task ResetAsync_Version6_ResetNotAdvertised_ThrowsAndSendsNothing()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 6);
        await AdvertiseResetSupportAsync(meter, resetSupported: false);

        await Assert.ThrowsExactlyAsync<ZWaveException>(
            () => meter.ResetAsync(MeterType.Electric, MeterScale.kWh, MeterRateType.Unspecified, 0, CancellationToken.None));

        // Only the SupportedGet from AdvertiseResetSupportAsync was sent; no reset frame.
        Assert.HasCount(1, driver.SentFrames);
        Assert.AreEqual((byte)MeterCommand.SupportedGet, driver.SentFrames[0].CommandId);
    }

    [TestMethod]
    public async Task ResetAllAsync_Version3_SupportNotDiscovered_ThrowsAndSendsNothing()
    {
        (MeterCommandClass meter, RecordingDriver driver) = CreateMeter(version: 3);

        await Assert.ThrowsExactlyAsync<ZWaveException>(() => meter.ResetAllAsync(CancellationToken.None));

        Assert.HasCount(0, driver.SentFrames);
    }

    [TestMethod]
    public void IsCommandSupported_Reset_SupportNotDiscovered_ReturnsNull()
    {
        // Reset support is only known after the Supported Report has been read.
        (MeterCommandClass meter, _) = CreateMeter(version: 6);

        Assert.IsNull(meter.IsCommandSupported(MeterCommand.Reset));
    }

    [TestMethod]
    public async Task IsCommandSupported_Reset_NotAdvertised_ReturnsFalse()
    {
        (MeterCommandClass meter, _) = CreateMeter(version: 6);
        await AdvertiseResetSupportAsync(meter, resetSupported: false);

        Assert.IsFalse(meter.IsCommandSupported(MeterCommand.Reset));
    }

    [TestMethod]
    public async Task IsCommandSupported_Reset_Advertised_ReturnsTrue()
    {
        (MeterCommandClass meter, _) = CreateMeter(version: 6);
        await AdvertiseResetSupportAsync(meter, resetSupported: true);

        Assert.IsTrue(meter.IsCommandSupported(MeterCommand.Reset));
    }
}
