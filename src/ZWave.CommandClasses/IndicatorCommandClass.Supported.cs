using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class IndicatorCommandClass
{
    private readonly Dictionary<IndicatorId, IReadOnlySet<IndicatorPropertyId>> _supportedIndicators = [];

    /// <summary>
    /// Gets the supported property IDs for each discovered indicator.
    /// </summary>
    public IReadOnlyDictionary<IndicatorId, IReadOnlySet<IndicatorPropertyId>> SupportedIndicators => _supportedIndicators;

    /// <summary>
    /// Request the supported properties of a specific indicator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To discover all supported indicators, start with indicator ID 0x00. The interview
    /// performs this discovery automatically by following the internal next-indicator chain.
    /// </para>
    /// </remarks>
    /// <param name="indicatorId">
    /// The indicator resource to query. Set to 0x00 to discover the first supported indicator.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IReadOnlySet<IndicatorPropertyId>> GetSupportedAsync(
        IndicatorId indicatorId,
        CancellationToken cancellationToken)
    {
        IndicatorSupportedGetCommand command = IndicatorSupportedGetCommand.Create(indicatorId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<IndicatorSupportedReportCommand>(
            predicate: frame => frame.CommandParameters.Length >= 1
                && (IndicatorId)frame.CommandParameters.Span[0] == indicatorId,
            cancellationToken).ConfigureAwait(false);
        (IndicatorId _, IReadOnlySet<IndicatorPropertyId> propertyIds) =
            IndicatorSupportedReportCommand.Parse(reportFrame, Logger);

        if (propertyIds.Count > 0)
        {
            _supportedIndicators[indicatorId] = propertyIds;
        }

        return propertyIds;
    }

    internal readonly struct IndicatorSupportedGetCommand : ICommand
    {
        public IndicatorSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static IndicatorSupportedGetCommand Create(IndicatorId indicatorId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)indicatorId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorSupportedGetCommand(frame);
        }
    }

    internal readonly struct IndicatorSupportedReportCommand : ICommand
    {
        public IndicatorSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Create a Supported Report command.
        /// </summary>
        public static IndicatorSupportedReportCommand Create(
            IndicatorId indicatorId,
            IndicatorId nextIndicatorId,
            IReadOnlySet<IndicatorPropertyId> supportedPropertyIds)
        {
            // Determine the minimum bitmask length needed.
            int maxPropertyId = 0;
            foreach (IndicatorPropertyId propertyId in supportedPropertyIds)
            {
                if ((byte)propertyId > maxPropertyId)
                {
                    maxPropertyId = (byte)propertyId;
                }
            }

            int bitmaskLength = maxPropertyId > 0
                ? ((maxPropertyId / 8) + 1)
                : 0;

            // Format: IndicatorID(1) + NextIndicatorID(1) + Reserved|BitmaskLength(1) + Bitmask(N)
            Span<byte> commandParameters = stackalloc byte[3 + bitmaskLength];
            commandParameters[0] = (byte)indicatorId;
            commandParameters[1] = (byte)nextIndicatorId;
            commandParameters[2] = (byte)(bitmaskLength & 0b0001_1111);

            foreach (IndicatorPropertyId propertyId in supportedPropertyIds)
            {
                int bitIndex = (byte)propertyId;
                commandParameters[3 + (bitIndex / 8)] |= (byte)(1 << (bitIndex % 8));
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorSupportedReportCommand(frame);
        }

        public static (IndicatorId NextIndicatorId, IReadOnlySet<IndicatorPropertyId> SupportedPropertyIds) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning(
                    "Indicator Supported Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Indicator Supported Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            IndicatorId nextIndicatorId = (IndicatorId)span[1];
            int bitmaskLength = span[2] & 0b0001_1111;

            HashSet<IndicatorPropertyId> supportedPropertyIds;
            if (bitmaskLength > 0 && span.Length >= 3 + bitmaskLength)
            {
                // Per spec: bit 0 in bitmask 1 is reserved and must be set to zero.
                supportedPropertyIds = BitMaskHelper.ParseBitMask<IndicatorPropertyId>(
                    span.Slice(3, bitmaskLength),
                    startBit: 1);
            }
            else
            {
                supportedPropertyIds = [];
            }

            return (nextIndicatorId, supportedPropertyIds);
        }
    }
}
