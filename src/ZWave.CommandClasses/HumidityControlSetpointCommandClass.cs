using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The humidity control setpoint type.
/// </summary>
public enum HumidityControlSetpointType : byte
{
    /// <summary>
    /// Humidifier setpoint.
    /// </summary>
    Humidifier = 0x01,

    /// <summary>
    /// De-humidifier setpoint.
    /// </summary>
    Dehumidifier = 0x02,

    /// <summary>
    /// Auto setpoint.
    /// </summary>
    Auto = 0x03,
}

/// <summary>
/// The humidity control setpoint scale.
/// </summary>
public enum HumidityControlSetpointScale : byte
{
    /// <summary>
    /// Percentage value.
    /// </summary>
    Percentage = 0x00,

    /// <summary>
    /// Absolute humidity (g/m³).
    /// </summary>
    AbsoluteHumidity = 0x01,
}

/// <summary>
/// Commands for the Humidity Control Setpoint Command Class.
/// </summary>
public enum HumidityControlSetpointCommand : byte
{
    /// <summary>
    /// Set a humidity control setpoint in the device.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the given humidity control setpoint type from the device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the value of the humidity control setpoint type from the device.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the humidity control setpoint types supported by the device.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Report the humidity control setpoint types supported by the device.
    /// </summary>
    SupportedReport = 0x05,

    /// <summary>
    /// Request the supported scales for a given setpoint type.
    /// </summary>
    ScaleSupportedGet = 0x06,

    /// <summary>
    /// Report the supported scales for a given setpoint type.
    /// </summary>
    ScaleSupportedReport = 0x07,

    /// <summary>
    /// Request the minimum and maximum setpoint values for a given setpoint type.
    /// </summary>
    CapabilitiesGet = 0x08,

    /// <summary>
    /// Report the minimum and maximum setpoint values for a given setpoint type.
    /// </summary>
    CapabilitiesReport = 0x09,
}

/// <summary>
/// Implements the Humidity Control Setpoint Command Class (V1-2).
/// </summary>
[CommandClass(CommandClassId.HumidityControlSetpoint)]
public sealed partial class HumidityControlSetpointCommandClass : CommandClass<HumidityControlSetpointCommand>
{
    internal HumidityControlSetpointCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(HumidityControlSetpointCommand command)
        => command switch
        {
            HumidityControlSetpointCommand.Set => true,
            HumidityControlSetpointCommand.Get => true,
            HumidityControlSetpointCommand.SupportedGet => true,
            HumidityControlSetpointCommand.ScaleSupportedGet => true,
            HumidityControlSetpointCommand.CapabilitiesGet => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        IReadOnlySet<HumidityControlSetpointType> supportedTypes = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

        foreach (HumidityControlSetpointType setpointType in supportedTypes)
        {
            _ = await GetScaleSupportedAsync(setpointType, cancellationToken).ConfigureAwait(false);
            _ = await GetCapabilitiesAsync(setpointType, cancellationToken).ConfigureAwait(false);
            _ = await GetAsync(setpointType, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((HumidityControlSetpointCommand)frame.CommandId)
        {
            case HumidityControlSetpointCommand.Report:
            {
                HumidityControlSetpointReport report = HumidityControlSetpointReportCommand.Parse(frame, Logger);
                _setpointValues[report.SetpointType] = report;
                OnSetpointReportReceived?.Invoke(report);
                break;
            }
        }
    }

    /// <summary>
    /// Parse a precision/scale/size byte and extract the value from the following bytes.
    /// </summary>
    internal static (int Precision, HumidityControlSetpointScale Scale, int ValueSize) ParsePrecisionScaleSize(byte pss)
    {
        int precision = (pss & 0b1110_0000) >> 5;
        HumidityControlSetpointScale scale = (HumidityControlSetpointScale)((pss & 0b0001_1000) >> 3);
        int valueSize = pss & 0b0000_0111;
        return (precision, scale, valueSize);
    }

    /// <summary>
    /// Parse a signed big-endian value of 1, 2, or 4 bytes with the given precision.
    /// </summary>
    internal static double ParseValue(ReadOnlySpan<byte> valueBytes, int precision)
    {
        int rawValue = valueBytes.ReadSignedVariableSizeBE();
        return rawValue / BinaryExtensions.PowersOfTen[precision];
    }

    /// <summary>
    /// Encode a precision/scale/size byte.
    /// </summary>
    internal static byte EncodePrecisionScaleSize(int precision, HumidityControlSetpointScale scale, int valueSize)
    {
        return (byte)(((precision & 0x07) << 5) | (((byte)scale & 0x03) << 3) | (valueSize & 0x07));
    }

    /// <summary>
    /// Determine the precision and raw integer value for a decimal setpoint value.
    /// </summary>
    internal static (int RawValue, int Size, int Precision) EncodeValue(double value)
    {
        // Determine precision: count decimal places (up to 7)
        int precision = 0;
        double scaled = value;
        while (precision < 7 && Math.Abs(scaled - Math.Round(scaled)) > 1e-9)
        {
            precision++;
            scaled = value * BinaryExtensions.PowersOfTen[precision];
        }

        int rawValue = (int)Math.Round(value * BinaryExtensions.PowersOfTen[precision]);
        int valueSize = rawValue.GetSignedVariableSize();
        return (rawValue, valueSize, precision);
    }
}
