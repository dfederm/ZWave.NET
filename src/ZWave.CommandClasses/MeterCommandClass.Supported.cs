using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the supported scales and capabilities advertised by a meter.
/// </summary>
public readonly record struct MeterSupportedReport(
    /// <summary>
    /// The meter type implemented by the device.
    /// </summary>
    MeterType Type,

    /// <summary>
    /// The rate type(s) supported by the device (import, export, or both).
    /// </summary>
    MeterRateType RateType,

    /// <summary>
    /// Indicates whether the device supports the Meter Reset command.
    /// </summary>
    bool ResetSupported,

    /// <summary>
    /// The set of scales supported by the device for the advertised meter type.
    /// </summary>
    IReadOnlySet<MeterScale> SupportedScales);

public sealed partial class MeterCommandClass
{
    private MeterSupportedReport? _supported;

    /// <summary>
    /// Gets the meter type implemented by the device, or <see langword="null"/> if not yet known.
    /// </summary>
    public MeterType? SupportedMeterType => _supported?.Type;

    /// <summary>
    /// Gets the rate type(s) supported by the device, or <see langword="null"/> if not yet known.
    /// </summary>
    public MeterRateType? SupportedRateType => _supported?.RateType;

    /// <summary>
    /// Gets whether the device supports the Meter Reset command, or <see langword="null"/> if not yet known.
    /// </summary>
    public bool? ResetSupported => _supported?.ResetSupported;

    /// <summary>
    /// Gets the scales supported by the device, or <see langword="null"/> if not yet known.
    /// </summary>
    public IReadOnlySet<MeterScale>? SupportedScales => _supported?.SupportedScales;

    /// <summary>
    /// Requests the supported scales and capabilities from the device.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The supported scales and capabilities.</returns>
    public async Task<MeterSupportedReport> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = MeterSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MeterSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        MeterSupportedReport report = MeterSupportedReportCommand.Parse(reportFrame, Logger);

        _supported = report;

        return report;
    }

    internal readonly struct MeterSupportedGetCommand : ICommand
    {
        public MeterSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static MeterSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MeterSupportedGetCommand(frame);
        }
    }

    internal readonly struct MeterSupportedReportCommand : ICommand
    {
        public MeterSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static MeterSupportedReport Parse(CommandClassFrame frame, ILogger logger)
        {
            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            // Minimum: the two header bytes.
            if (span.Length < 2)
            {
                logger.LogWarning("Meter Supported Report frame is too short ({Length} bytes)", span.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Supported Report frame is too short");
            }

            byte header0 = span[0];
            byte header1 = span[1];

            bool resetSupported = (header0 & 0b1000_0000) != 0;
            MeterRateType rateType = (MeterRateType)((header0 >> 5) & 0b0000_0011);

            MeterType meterType = (MeterType)(header0 & 0b0001_1111);
            if (!MeterScaleHelper.IsKnownMeterType(meterType))
            {
                logger.LogWarning("Meter Supported Report frame has unknown meter type {MeterType}", (byte)meterType);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, $"Meter Supported Report frame has unknown meter type {(byte)meterType}");
            }

            bool moreScaleTypes = (header1 & 0b1000_0000) != 0;

            HashSet<MeterScale> scales = new HashSet<MeterScale>();

            // "Scale Supported Byte 1": bits 0-6 map to scale indices 0-6.
            byte firstScaleByte = (byte)(header1 & 0b0111_1111);
            AddScalesForByte(scales, meterType, firstScaleByte, startIndex: 0, maxBits: 7);

            if (moreScaleTypes)
            {
                if (span.Length < 3)
                {
                    logger.LogWarning("Meter Supported Report frame is too short for the scale byte count ({Length} bytes)", span.Length);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Supported Report frame is too short for the scale byte count");
                }

                int count = span[2];
                if (span.Length < 3 + count)
                {
                    logger.LogWarning("Meter Supported Report frame is too short for {Count} scale bytes ({Length} bytes)", count, span.Length);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Meter Supported Report frame is too short for the declared scale byte count");
                }

                // Subsequent bytes use all 8 bits, continuing the scale index from 7.
                for (int i = 0; i < count; i++)
                {
                    AddScalesForByte(scales, meterType, span[3 + i], startIndex: 7 + (i * 8), maxBits: 8);
                }
            }

            return new MeterSupportedReport(meterType, rateType, resetSupported, scales);
        }

        private static void AddScalesForByte(
            HashSet<MeterScale> scales,
            MeterType meterType,
            byte bitMask,
            int startIndex,
            int maxBits)
        {
            for (int bit = 0; bit < maxBits; bit++)
            {
                if ((bitMask & (1 << bit)) != 0)
                {
                    if (MeterScaleHelper.ResolveScale(meterType, startIndex + bit) is MeterScale scale)
                    {
                        scales.Add(scale);
                    }
                }
            }
        }

        public static MeterSupportedReportCommand Create(
            MeterType meterType,
            MeterRateType rateType,
            bool resetSupported,
            IReadOnlySet<MeterScale> supportedScales)
        {
            int maxIndex = -1;
            int firstByte = 0;
            foreach (MeterScale scale in supportedScales)
            {
                int? index = GetIndex(meterType, scale);
                if (index is null)
                {
                    continue;
                }

                if (index.Value <= 6)
                {
                    firstByte |= 1 << index.Value;
                }

                if (index.Value > maxIndex)
                {
                    maxIndex = index.Value;
                }
            }

            byte byte0 = (byte)(
                (resetSupported ? 0b1000_0000 : 0)
                | ((byte)rateType << 5)
                | ((byte)meterType & 0b0001_1111));

            if (maxIndex <= 6)
            {
                byte header1Compact = (byte)(firstByte & 0b0111_1111);
                return new MeterSupportedReportCommand(CommandClassFrame.Create(CommandClassId, CommandId, [byte0, header1Compact]));
            }

            // More scale types: set the M.S.T bit, then the byte count, then the scale bytes.
            byte header1 = (byte)(0b1000_0000 | (firstByte & 0b0111_1111));
            int count = (maxIndex - 7) / 8 + 1;
            Span<byte> scaleBytes = stackalloc byte[count];
            for (int i = 0; i < count; i++)
            {
                int baseIndex = 7 + (i * 8);
                int bits = 0;
                foreach (MeterScale scale in supportedScales)
                {
                    int? sIndex = GetIndex(meterType, scale);
                    if (sIndex.HasValue && sIndex.Value >= baseIndex && sIndex.Value < baseIndex + 8)
                    {
                        bits |= 1 << (sIndex.Value - baseIndex);
                    }
                }

                scaleBytes[i] = (byte)bits;
            }

            Span<byte> parameters = stackalloc byte[3 + count];
            parameters[0] = byte0;
            parameters[1] = header1;
            parameters[2] = (byte)count;
            scaleBytes.CopyTo(parameters[3..]);

            return new MeterSupportedReportCommand(CommandClassFrame.Create(CommandClassId, CommandId, parameters));
        }

        private static int? GetIndex(MeterType meterType, MeterScale scale)
        {
            // Mirror of MeterScaleHelper.TryGetEncoding, but returns the flat index for encoding.
            if (MeterScaleHelper.TryGetEncoding(meterType, scale, out byte scaleValue, out byte? scale2Value))
            {
                return scaleValue < 7 ? scaleValue : 7 + (scale2Value ?? 0);
            }

            return null;
        }
    }
}
