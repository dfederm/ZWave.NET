using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a meter reading.
/// </summary>
public readonly record struct MeterReport(
    /// <summary>
    /// The type of metering physical unit being reported.
    /// </summary>
    MeterType Type,

    /// <summary>
    /// The rate type of the reading (import, export, or unspecified).
    /// </summary>
    MeterRateType RateType,

    /// <summary>
    /// The unit of measure (scale) of the reading.
    /// </summary>
    MeterScale Scale,

    /// <summary>
    /// The value of the reading, with the reported precision applied.
    /// </summary>
    double Value,

    /// <summary>
    /// The elapsed time between the previous reading and this one, or <see langword="null"/> if
    /// there is no previous value or the elapsed time is unknown.
    /// </summary>
    TimeSpan? DeltaTime,

    /// <summary>
    /// The previous reading, or <see langword="null"/> if not reported.
    /// </summary>
    double? PreviousValue);

public sealed partial class MeterCommandClass
{
    // Gets currently awaiting their report: the requested signature, and the ownership token of
    // the call that registered it (see GetAsync).
    private readonly Dictionary<MeterGetSignature, object> _inFlightGets =
        new Dictionary<MeterGetSignature, object>();

    // What a Get requests: the concrete scale (0 on the wire is the device default and is stored
    // as null), its Scale 2 value for M.S.T scales, and the rate type. A null field requests the
    // device default, which is a specific (unknown) value, not a wildcard.
    private readonly record struct MeterGetSignature(byte? Scale, byte? Scale2, byte? RateType)
    {
        /// <summary>
        /// Determines whether the reports of the two requests can be told apart: at least one
        /// field must carry a different concrete value on both sides.
        /// </summary>
        public bool Distinguishes(MeterGetSignature other)
        {
            if (Scale.HasValue && other.Scale.HasValue)
            {
                if (Scale.Value != other.Scale.Value
                    || (Scale.Value == 7 && Scale2 != other.Scale2))
                {
                    return true;
                }
            }

            return RateType.HasValue && other.RateType.HasValue && RateType.Value != other.RateType.Value;
        }
    }

    /// <summary>
    /// Gets the last meter report received, or <see langword="null"/> if none has been received.
    /// </summary>
    public MeterReport? LastReport { get; private set; }

    /// <summary>
    /// Occurs when a Meter Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<MeterReport>? OnMeterReportReceived;

    /// <summary>
    /// Requests the current meter reading from the device.
    /// </summary>
    /// <param name="type">
    /// The meter type the requested scale applies to. If <see langword="null"/>, the device's
    /// known meter type (from the interview) is used.
    /// </param>
    /// <param name="scale">
    /// The scale to request, or <see langword="null"/> to request the device's default scale.
    /// </param>
    /// <param name="rateType">
    /// The rate type to request, or <see langword="null"/> to request the device's default rate type.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The meter reading.</returns>
    /// <remarks>
    /// The requested scale and rate type must be representable in the negotiated command class
    /// version: the Scale field does not exist at V1, holds values 0-3 at V2, and values 0-6 at V3;
    /// the M.S.T scales (with the Scale 2 field) and the Rate Type field only exist at V4 and later.
    /// A Get that differs from an in-flight Get only in fields where either side requests the
    /// device default is rejected, as is an identical duplicate: the device's reports cannot be
    /// attributed to such requests, and a duplicate would not offer independent cancellation.
    /// </remarks>
    /// <exception cref="ZWaveException">
    /// <see cref="ZWaveErrorCode.CommandInvalidArgument"/> if the rate type is <c>Both</c>, the requested
    /// scale is not valid for the meter type, or the requested scale or rate type cannot be represented
    /// in the negotiated command class version;
    /// <see cref="ZWaveErrorCode.CommandNotReady"/> if a Meter Get that cannot be distinguished from this
    /// request, or an identical Get, is already in flight.
    /// </exception>
    public async Task<MeterReport> GetAsync(
        MeterType? type,
        MeterScale? scale,
        MeterRateType? rateType,
        CancellationToken cancellationToken)
    {
        if (rateType == MeterRateType.Both)
        {
            ZWaveException.Throw(
                ZWaveErrorCode.CommandInvalidArgument,
                "Rate type 'Both' cannot be requested with a Meter Get; request 'Import' or 'Export' instead.");
        }

        byte? scaleValue = null;
        byte? scale2Value = null;
        if (scale.HasValue)
        {
            MeterType effectiveType = type ?? GetEffectiveMeterType();
            if (!MeterScaleHelper.TryGetEncoding(effectiveType, scale.Value, out byte scaleByte, out byte? scale2Byte))
            {
                ZWaveException.Throw(
                    ZWaveErrorCode.CommandInvalidArgument,
                    $"Scale '{scale.Value}' is not a valid scale for meter type '{effectiveType}'.");
            }

            scaleValue = scaleByte;
            scale2Value = scale2Byte;
        }

        byte? rateTypeValue = rateType is null or MeterRateType.Unspecified ? null : (byte)rateType.Value;

        // Scale 0 is the device default on the wire, the same as requesting no scale; a missing
        // Scale 2 for an M.S.T request defaults to 0, matching the wire encoding.
        byte? constrainedScale = scaleValue is > 0 ? scaleValue : null;
        byte? constrainedScale2 = constrainedScale == 7 ? (scale2Value ?? 0) : null;
        var signature = new MeterGetSignature(constrainedScale, constrainedScale2, rateTypeValue);

        object token = new object();
        lock (_inFlightGets)
        {
            foreach (MeterGetSignature inFlight in _inFlightGets.Keys)
            {
                if (!signature.Distinguishes(inFlight))
                {
                    ZWaveException.Throw(
                        ZWaveErrorCode.CommandNotReady,
                        "A Meter Get that cannot be distinguished from this request (including an identical Get) is already in flight; wait for it to complete first.");
                }
            }

            _inFlightGets.Add(signature, token);
        }

        try
        {
            var command = MeterGetCommand.Create(EffectiveVersion, rateTypeValue, constrainedScale, constrainedScale2);
            await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

            CommandClassFrame reportFrame = await AwaitNextReportAsync<MeterReportCommand>(
                predicate: frame => MeterReportCommand.MatchesRequest(frame, constrainedScale, constrainedScale2, rateTypeValue),
                cancellationToken).ConfigureAwait(false);

            // Release the in-flight slot before the report event so an event handler can issue a
            // follow-up Get; the report has been matched and consumed at this point.
            RemoveInFlightGet(signature, token);

            MeterReport report = MeterReportCommand.Parse(reportFrame, Logger);
            LastReport = report;
            OnMeterReportReceived?.Invoke(report);
            return report;
        }
        finally
        {
            RemoveInFlightGet(signature, token);
        }
    }

    private void RemoveInFlightGet(MeterGetSignature signature, object token)
    {
        // Ownership-checked: a follow-up Get issued from the report event may already hold this
        // signature's slot, and the completing call must not remove it.
        lock (_inFlightGets)
        {
            if (_inFlightGets.TryGetValue(signature, out object? current)
                && ReferenceEquals(current, token))
            {
                _inFlightGets.Remove(signature);
            }
        }
    }

    private MeterType GetEffectiveMeterType()
    {
        if (SupportedMeterType.HasValue)
        {
            return SupportedMeterType.Value;
        }

        ZWaveException.Throw(ZWaveErrorCode.CommandNotReady, "The meter type is not yet known; interview the node first.");
        return default;
    }

    internal readonly struct MeterGetCommand : ICommand
    {
        public MeterGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MeterGetCommand Create(byte version, byte? rateType, byte? scale, byte? scale2)
        {
            if (scale.HasValue && (version <= 1 || (version == 2 && scale.Value > 3) || (version == 3 && scale.Value > 6)))
            {
                // The Scale field does not exist at V1, holds 0-3 (2 bits) at V2, and 0-6 (3 bits) at
                // V3; the M.S.T scale (7) with its Scale 2 field is only available from V4.
                ZWaveException.Throw(
                    ZWaveErrorCode.CommandInvalidArgument,
                    $"Scale value {scale.Value} cannot be requested with a Meter Get at version {version}.");
            }

            if (rateType.HasValue && version < 4)
            {
                // The Rate Type field only exists at V4+.
                ZWaveException.Throw(
                    ZWaveErrorCode.CommandInvalidArgument,
                    $"Rate type cannot be requested with a Meter Get at version {version}.");
            }

            if (version <= 1)
            {
                // The V1 Meter Get command has no parameters.
                return new MeterGetCommand(CommandClassFrame.Create(CommandClassId, CommandId));
            }

            int scaleValue = scale.GetValueOrDefault(0);
            int rateTypeValue = rateType.GetValueOrDefault(0);

            // Scale occupies bits 5-3 for V2+; the rate type occupies bits 7-6 starting at V4.
            byte rateTypeBits = (byte)(version >= 4 ? (rateTypeValue & 0b0000_0011) : 0);
            byte byte0 = (byte)((rateTypeBits << 6) | ((scaleValue & 0b0000_0111) << 3));

            // The Scale 2 field is present only at V4+ when the scale field indicates M.S.T (7).
            if (version >= 4 && scaleValue == 7)
            {
                byte scale2Value = scale2.GetValueOrDefault(0);
                return new MeterGetCommand(CommandClassFrame.Create(CommandClassId, CommandId, [byte0, scale2Value]));
            }

            return new MeterGetCommand(CommandClassFrame.Create(CommandClassId, CommandId, [byte0]));
        }
    }

    internal readonly struct MeterReportCommand : ICommand
    {
        public MeterReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.Report;

        public CommandClassFrame Frame { get; }

        public static MeterReport Parse(CommandClassFrame frame, ILogger logger)
        {
            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            // Minimum: the two header bytes.
            if (span.Length < 2)
            {
                logger.LogWarning("Meter Report frame is too short ({Length} bytes)", span.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Report frame is too short");
            }

            byte header0 = span[0];
            byte header1 = span[1];

            MeterType meterType = (MeterType)(header0 & 0b0001_1111);
            if (!MeterScaleHelper.IsKnownMeterType(meterType))
            {
                logger.LogWarning("Meter Report frame has unknown meter type {MeterType}", (byte)meterType);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, $"Meter Report frame has unknown meter type {(byte)meterType}");
            }

            MeterRateType rateType = (MeterRateType)((header0 >> 5) & 0b0000_0011);

            // The scale is a 3-bit field composed of bit 7 of header0 (Scale 2) and bits 4-3 of header1.
            byte scaleByte = (byte)((((header0 & 0b1000_0000) >> 7) << 2) | ((header1 >> 3) & 0b0000_0011));
            byte precision = (byte)((header1 >> 5) & 0b0000_0111);
            byte valueSize = (byte)(header1 & 0b0000_0111);

            if (valueSize is not (1 or 2 or 4))
            {
                logger.LogWarning("Meter Report frame has invalid value size {Size}", valueSize);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, $"Meter Report frame has invalid value size {valueSize}");
            }

            int offset = 2;

            if (span.Length < offset + valueSize)
            {
                logger.LogWarning("Meter Report frame is too short for value size {Size} ({Length} bytes)", valueSize, span.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Report frame is too short for value size");
            }

            int rawValue = span.Slice(offset, valueSize).ReadSignedVariableSizeBE();
            offset += valueSize;
            double value = rawValue / BinaryExtensions.PowersOfTen[precision];

            double? previousValue = null;
            TimeSpan? deltaTimeResult = null;
            byte scale2Byte = 0;

            if (span.Length == offset)
            {
                // V1 reports end after the Meter Value (Delta Time and Previous Meter Value
                // were added in V2). A V2+ frame truncated exactly after the value is
                // indistinguishable from a valid V1 frame and is accepted as such.
                if (scaleByte == 7)
                {
                    logger.LogWarning("Meter Report frame uses the M.S.T scale without a Scale 2 byte ({Length} bytes)", span.Length);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Report frame uses the M.S.T scale without a Scale 2 byte");
                }
            }
            else
            {
                if (span.Length < offset + 2)
                {
                    logger.LogWarning("Meter Report frame has a truncated Delta Time field ({Length} bytes)", span.Length);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Report frame has a truncated Delta Time field");
                }

                ushort deltaTime = span.Slice(offset, 2).ToUInt16BE();
                offset += 2;

                if (deltaTime != 0)
                {
                    if (span.Length < offset + valueSize)
                    {
                        logger.LogWarning("Meter Report frame is too short for previous value ({Length} bytes)", span.Length);
                        ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Report frame is too short for previous value");
                    }

                    int rawPrevious = span.Slice(offset, valueSize).ReadSignedVariableSizeBE();
                    offset += valueSize;
                    previousValue = rawPrevious / BinaryExtensions.PowersOfTen[precision];

                    // 0xFFFF indicates an unknown elapsed time.
                    if (deltaTime != 0xFFFF)
                    {
                        deltaTimeResult = TimeSpan.FromSeconds(deltaTime);
                    }
                }

                if (scaleByte == 7)
                {
                    if (span.Length < offset + 1)
                    {
                        logger.LogWarning("Meter Report frame is too short for Scale 2 ({Length} bytes)", span.Length);
                        ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Report frame is too short for Scale 2");
                    }

                    scale2Byte = span[offset];
                    offset += 1;
                }
            }

            int scaleIndex = scaleByte < 7 ? scaleByte : 7 + scale2Byte;
            MeterScale? resolvedScale = MeterScaleHelper.ResolveScale(meterType, scaleIndex);
            if (resolvedScale is null)
            {
                logger.LogWarning(
                    "Meter Report frame has unsupported scale (Scale={Scale}, Scale2={Scale2}) for meter type {MeterType}",
                    scaleByte,
                    scale2Byte,
                    meterType);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Report frame has an unsupported scale for the meter type");
            }

            return new MeterReport(meterType, rateType, resolvedScale.Value, value, deltaTimeResult, previousValue);
        }

        /// <summary>
        /// Determines whether a Meter Report frame matches a Meter Get request.
        /// </summary>
        /// <param name="frame">The report frame to inspect.</param>
        /// <param name="scale">
        /// The requested 3-bit scale value (0-7), or <see langword="null"/>/0 for the device default.
        /// </param>
        /// <param name="scale2">The requested Scale 2 value when <paramref name="scale"/> is 7 (M.S.T).</param>
        /// <param name="rateType">The requested rate type, or <see langword="null"/> for the device default.</param>
        /// <returns>
        /// True when the frame matches the request, or when the request placed no constraint on a given field.
        /// </returns>
        internal static bool MatchesRequest(CommandClassFrame frame, byte? scale, byte? scale2, byte? rateType)
        {
            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            if (span.Length < 2)
            {
                return false;
            }

            byte header0 = span[0];
            byte header1 = span[1];

            if (rateType.HasValue)
            {
                byte reportRateType = (byte)((header0 >> 5) & 0b0000_0011);
                if (reportRateType != rateType.Value)
                {
                    return false;
                }
            }

            // A scale of 0 (or null) requests the device default, which is not known in advance.
            if (scale is > 0)
            {
                // The scale is a 3-bit field composed of bit 7 of header0 and bits 4-3 of header1.
                byte reportScale = (byte)((((header0 & 0b1000_0000) >> 7) << 2) | ((header1 >> 3) & 0b0000_0011));
                if (reportScale != scale.Value)
                {
                    return false;
                }

                if (scale.Value == 7)
                {
                    // M.S.T: the Scale 2 byte follows the optional Previous Meter Value, so its
                    // position is computed from the Size field, bounds-checking each step.
                    byte valueSize = (byte)(header1 & 0b0000_0111);
                    if (valueSize is not (1 or 2 or 4))
                    {
                        return false;
                    }

                    int offset = 2 + valueSize;
                    if (span.Length < offset + 2)
                    {
                        return false;
                    }

                    ushort deltaTime = span.Slice(offset, 2).ToUInt16BE();
                    offset += 2;
                    if (deltaTime != 0)
                    {
                        if (span.Length < offset + valueSize)
                        {
                            return false;
                        }

                        offset += valueSize;
                    }

                    if (span.Length < offset + 1)
                    {
                        return false;
                    }

                    if (span[offset] != (scale2 ?? 0))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static MeterReportCommand Create(
            MeterType meterType,
            MeterRateType rateType,
            MeterScale scale,
            int precision,
            double value,
            int size,
            TimeSpan? deltaTime,
            double? previousValue)
        {
            if (!MeterScaleHelper.TryGetEncoding(meterType, scale, out byte scaleValue, out byte? scale2Value))
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, $"Scale '{scale}' is not a valid scale for meter type '{meterType}'.");
            }

            if (size is not (1 or 2 or 4))
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, $"Invalid value size {size}; expected 1, 2, or 4.");
            }

            if (precision is < 0 or > 7)
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, $"Invalid precision {precision}; expected 0..7.");
            }

            int rawValue = (int)Math.Round(value * BinaryExtensions.PowersOfTen[precision]);
            int rawPrevious = previousValue.HasValue ? (int)Math.Round(previousValue.Value * BinaryExtensions.PowersOfTen[precision]) : 0;

            ushort deltaTimeValue = 0;
            if (previousValue.HasValue)
            {
                // A present previous value with an unknown elapsed time is encoded as 0xFFFF.
                deltaTimeValue = (ushort)(deltaTime.HasValue ? (int)deltaTime.Value.TotalSeconds : 0xFFFF);
            }

            Span<byte> parameters = stackalloc byte[2 + size + 2 + (previousValue.HasValue ? size : 0) + (scale2Value.HasValue ? 1 : 0)];
            // Byte 0: Scale (2) | Rate Type (2) | Meter Type (5). Scale (2) is the top bit of the 3-bit scale.
            parameters[0] = (byte)((((scaleValue >> 2) & 0b1) << 7)
                | (((byte)rateType & 0b0000_0011) << 5)
                | ((byte)meterType & 0b0001_1111));
            // Byte 1: Precision (3) | Scale (1:0) (2) | Size (3). Scale (1:0) is the low two bits of the 3-bit scale.
            parameters[1] = (byte)(((precision & 0b0000_0111) << 5)
                | (((scaleValue & 0b0000_0011) << 3) & 0b0001_1000)
                | (byte)(size & 0b0000_0111));

            int offset = 2;
            rawValue.WriteSignedVariableSizeBE(parameters.Slice(offset, size));
            offset += size;
            deltaTimeValue.WriteBytesBE(parameters.Slice(offset, 2));
            offset += 2;
            if (previousValue.HasValue)
            {
                rawPrevious.WriteSignedVariableSizeBE(parameters.Slice(offset, size));
                offset += size;
            }
            if (scale2Value.HasValue)
            {
                parameters[offset] = scale2Value.Value;
            }

            return new MeterReportCommand(CommandClassFrame.Create(CommandClassId, CommandId, parameters));
        }
    }
}
