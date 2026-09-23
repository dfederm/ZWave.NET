using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The type of metering physical unit being reported.
/// </summary>
public enum MeterType : byte
{
    /// <summary>
    /// Electric meter (e.g. kWh, kVAh, W, V, A).
    /// </summary>
    Electric = 0x01,

    /// <summary>
    /// Gas meter (e.g. cubic meters, cubic feet).
    /// </summary>
    Gas = 0x02,

    /// <summary>
    /// Water meter (e.g. cubic meters, cubic feet, US gallons).
    /// </summary>
    Water = 0x03,

    /// <summary>
    /// Heating meter (e.g. kWh).
    /// </summary>
    Heating = 0x04,

    /// <summary>
    /// Cooling meter (e.g. kWh).
    /// </summary>
    Cooling = 0x05,
}

/// <summary>
/// The unit of measure (scale) of a meter reading.
/// </summary>
/// <remarks>
/// The wire encoding of a scale is specific to a <see cref="MeterType"/>. Use
/// <see cref="MeterScaleHelper"/> to convert between a scale and its wire encoding.
/// The underlying value of this enum is an identifier, not a wire value.
/// </remarks>
public enum MeterScale
{
    /// <summary>
    /// Kilowatt-hour (kWh).
    /// </summary>
    kWh,

    /// <summary>
    /// Kilovolt-ampere-hour (kVAh).
    /// </summary>
    kVAh,

    /// <summary>
    /// Watt (W).
    /// </summary>
    W,

    /// <summary>
    /// Pulse count.
    /// </summary>
    PulseCount,

    /// <summary>
    /// Volt (V).
    /// </summary>
    V,

    /// <summary>
    /// Ampere (A).
    /// </summary>
    A,

    /// <summary>
    /// Power factor.
    /// </summary>
    PowerFactor,

    /// <summary>
    /// Kilovolt-ampere reactive (kVar).
    /// </summary>
    kVar,

    /// <summary>
    /// Kilovolt-ampere reactive hour (kVarh).
    /// </summary>
    kVarh,

    /// <summary>
    /// Cubic meters (m³).
    /// </summary>
    CubicMeters,

    /// <summary>
    /// Cubic feet (ft³).
    /// </summary>
    CubicFeet,

    /// <summary>
    /// US gallons.
    /// </summary>
    USGallons,
}

/// <summary>
/// The rate type of a meter reading, indicating whether it advertises an import (consumed) or
/// export (produced) value.
/// </summary>
public enum MeterRateType : byte
{
    /// <summary>
    /// The rate type is unspecified, or the device's default rate type should be used.
    /// </summary>
    Unspecified = 0x00,

    /// <summary>
    /// Import (consumed) value.
    /// </summary>
    Import = 0x01,

    /// <summary>
    /// Export (produced) value.
    /// </summary>
    Export = 0x02,

    /// <summary>
    /// Both import and export values are supported. Used in the Supported Report.
    /// </summary>
    Both = 0x03,
}

/// <summary>
/// Commands for the Meter Command Class.
/// </summary>
public enum MeterCommand : byte
{
    /// <summary>
    /// Request the current meter reading from a supporting node.
    /// </summary>
    Get = 0x01,

    /// <summary>
    /// Advertise the current meter reading from a supporting node.
    /// </summary>
    Report = 0x02,

    /// <summary>
    /// Request the supported scales and capabilities from a supporting node.
    /// </summary>
    SupportedGet = 0x03,

    /// <summary>
    /// Advertise the supported scales and capabilities from a supporting node.
    /// </summary>
    SupportedReport = 0x04,

    /// <summary>
    /// Reset the accumulated meter value(s) at a supporting node.
    /// </summary>
    Reset = 0x05,
}

/// <summary>
/// Implements the Meter Command Class (CC 0x32).
/// </summary>
[CommandClass(CommandClassId.Meter)]
public sealed partial class MeterCommandClass : CommandClass<MeterCommand>
{
    internal MeterCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(MeterCommand command)
        => command switch
        {
            MeterCommand.Get => true,
            MeterCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            MeterCommand.Reset => ResetSupported.HasValue ? ResetSupported.Value : null,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(MeterCommand.SupportedGet).GetValueOrDefault())
        {
            _ = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);
        }

        _ = await GetAsync(type: null, scale: null, rateType: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((MeterCommand)frame.CommandId)
        {
            case MeterCommand.Report:
            {
                MeterReport report = MeterReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnMeterReportReceived?.Invoke(report);
                break;
            }
        }
    }
}

/// <summary>
/// Maps between a <see cref="MeterScale"/> and its wire encoding, which is specific to a
/// <see cref="MeterType"/>.
/// </summary>
/// <remarks>
/// The spec encodes a scale as a 3-bit value (0..6) plus an optional 8-bit "Scale 2" value when
/// the 3-bit value is 7 (M.S.T). This is flattened into a per-type "scale index" where indices
/// 0..6 map directly to the 3-bit value and indices 7 and 8 map to Scale 2 values 0 and 1.
/// </remarks>
internal static class MeterScaleHelper
{
    /// <summary>
    /// Determines whether a meter type value is a defined value per the specification.
    /// </summary>
    public static bool IsKnownMeterType(MeterType meterType)
        => meterType is >= MeterType.Electric and <= MeterType.Cooling;

    /// <summary>
    /// Resolves a scale index to a <see cref="MeterScale"/> for the given meter type.
    /// </summary>
    /// <param name="meterType">The meter type.</param>
    /// <param name="scaleIndex">
    /// The scale index (0..6 for the 3-bit scale field, or 7/8 for the Scale 2 field).
    /// </param>
    /// <returns>The scale, or <see langword="null"/> if the index is reserved for this meter type.</returns>
    public static MeterScale? ResolveScale(MeterType meterType, int scaleIndex)
        => (meterType, scaleIndex) switch
        {
            (MeterType.Electric, 0) => MeterScale.kWh,
            (MeterType.Electric, 1) => MeterScale.kVAh,
            (MeterType.Electric, 2) => MeterScale.W,
            (MeterType.Electric, 3) => MeterScale.PulseCount,
            (MeterType.Electric, 4) => MeterScale.V,
            (MeterType.Electric, 5) => MeterScale.A,
            (MeterType.Electric, 6) => MeterScale.PowerFactor,
            (MeterType.Electric, 7) => MeterScale.kVar,
            (MeterType.Electric, 8) => MeterScale.kVarh,
            (MeterType.Gas, 0) => MeterScale.CubicMeters,
            (MeterType.Gas, 1) => MeterScale.CubicFeet,
            (MeterType.Gas, 3) => MeterScale.PulseCount,
            (MeterType.Water, 0) => MeterScale.CubicMeters,
            (MeterType.Water, 1) => MeterScale.CubicFeet,
            (MeterType.Water, 2) => MeterScale.USGallons,
            (MeterType.Water, 3) => MeterScale.PulseCount,
            (MeterType.Heating, 0) => MeterScale.kWh,
            (MeterType.Cooling, 0) => MeterScale.kWh,
            _ => null,
        };

    /// <summary>
    /// Gets the wire encoding for a <see cref="MeterScale"/> on the given meter type.
    /// </summary>
    /// <param name="meterType">The meter type.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="scaleValue">When the method returns, the 3-bit scale value (0..7).</param>
    /// <param name="scale2Value">
    /// When the method returns, the Scale 2 value, or <see langword="null"/> if not present.
    /// </param>
    /// <returns><see langword="true"/> if the scale is valid for the meter type.</returns>
    public static bool TryGetEncoding(MeterType meterType, MeterScale scale, out byte scaleValue, out byte? scale2Value)
    {
        int? index = GetScaleIndex(meterType, scale);
        if (index is null)
        {
            scaleValue = 0;
            scale2Value = null;
            return false;
        }

        if (index.Value <= 6)
        {
            scaleValue = (byte)index.Value;
            scale2Value = null;
        }
        else
        {
            scaleValue = 7;
            scale2Value = (byte)(index.Value - 7);
        }

        return true;
    }

    private static int? GetScaleIndex(MeterType meterType, MeterScale scale)
        => (meterType, scale) switch
        {
            (MeterType.Electric, MeterScale.kWh) => 0,
            (MeterType.Electric, MeterScale.kVAh) => 1,
            (MeterType.Electric, MeterScale.W) => 2,
            (MeterType.Electric, MeterScale.PulseCount) => 3,
            (MeterType.Electric, MeterScale.V) => 4,
            (MeterType.Electric, MeterScale.A) => 5,
            (MeterType.Electric, MeterScale.PowerFactor) => 6,
            (MeterType.Electric, MeterScale.kVar) => 7,
            (MeterType.Electric, MeterScale.kVarh) => 8,
            (MeterType.Gas, MeterScale.CubicMeters) => 0,
            (MeterType.Gas, MeterScale.CubicFeet) => 1,
            (MeterType.Gas, MeterScale.PulseCount) => 3,
            (MeterType.Water, MeterScale.CubicMeters) => 0,
            (MeterType.Water, MeterScale.CubicFeet) => 1,
            (MeterType.Water, MeterScale.USGallons) => 2,
            (MeterType.Water, MeterScale.PulseCount) => 3,
            (MeterType.Heating, MeterScale.kWh) => 0,
            (MeterType.Cooling, MeterScale.kWh) => 0,
            _ => null,
        };
}
