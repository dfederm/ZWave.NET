using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class IndicatorCommandClass
{
    private readonly Dictionary<IndicatorId, IndicatorReport> _indicators = [];

    /// <summary>
    /// Gets the last indicator report received from the device (version 1 compatibility).
    /// </summary>
    public IndicatorReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the indicator state per indicator ID (version 2+).
    /// </summary>
    public IReadOnlyDictionary<IndicatorId, IndicatorReport> Indicators => _indicators;

    /// <summary>
    /// Event raised when an Indicator Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<IndicatorReport>? OnIndicatorReportReceived;

    /// <summary>
    /// Request the state of the indicator (version 1).
    /// </summary>
    public async Task<IndicatorReport> GetAsync(CancellationToken cancellationToken)
    {
        IndicatorGetCommand command = IndicatorGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<IndicatorReportCommand>(cancellationToken).ConfigureAwait(false);
        IndicatorReport report = IndicatorReportCommand.Parse(reportFrame, Logger);
        ApplyReport(report);
        return report;
    }

    /// <summary>
    /// Request the state of a specific indicator (version 2+).
    /// </summary>
    /// <param name="indicatorId">The indicator resource to query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IndicatorReport> GetAsync(IndicatorId indicatorId, CancellationToken cancellationToken)
    {
        IndicatorGetCommand command = IndicatorGetCommand.Create(indicatorId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<IndicatorReportCommand>(cancellationToken).ConfigureAwait(false);
        IndicatorReport report = IndicatorReportCommand.Parse(reportFrame, Logger);
        ApplyReport(report);
        return report;
    }

    /// <summary>
    /// Set the indicator state (version 1 format).
    /// </summary>
    /// <param name="value">
    /// The indicator value: 0x00=off, 0x01-0x63=on, 0xFF=on.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(byte value, CancellationToken cancellationToken)
    {
        IndicatorSetCommand command = IndicatorSetCommand.Create(value);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Set one or more indicator resources (version 2+ format).
    /// </summary>
    /// <param name="objects">The indicator objects to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(IReadOnlyList<IndicatorObject> objects, CancellationToken cancellationToken)
    {
        IndicatorSetCommand command = IndicatorSetCommand.Create(objects);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Instruct the device to identify itself by blinking/beeping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per spec CL:0087.01.31.02.1, this sets Indicator 0x50 (Node Identify) to blink
    /// with an 800ms period (600ms ON, 200ms OFF) for 3 cycles.
    /// </para>
    /// <para>
    /// Requires version 3 or newer on both the controlling and supporting node.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task IdentifyAsync(CancellationToken cancellationToken)
    {
        // Per spec Table 6.45: Indicator::Identify
        IndicatorObject[] objects =
        [
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnOffPeriod, 0x08),
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnOffCycles, 0x03),
            new(IndicatorId.NodeIdentify, IndicatorPropertyId.OnTimeWithinOnOffPeriod, 0x06),
        ];
        await SetAsync(objects, cancellationToken).ConfigureAwait(false);
    }

    private void ApplyReport(IndicatorReport report)
    {
        LastReport = report;

        // Track per-indicator state for V2+ reports.
        if (report.Objects.Count > 0)
        {
            IndicatorId indicatorId = report.Objects[0].IndicatorId;
            _indicators[indicatorId] = report;
        }

        OnIndicatorReportReceived?.Invoke(report);
    }

    internal readonly struct IndicatorGetCommand : ICommand
    {
        public IndicatorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.Get;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Create a version 1 Get command (no indicator ID).
        /// </summary>
        public static IndicatorGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new IndicatorGetCommand(frame);
        }

        /// <summary>
        /// Create a version 2+ Get command for a specific indicator.
        /// </summary>
        public static IndicatorGetCommand Create(IndicatorId indicatorId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)indicatorId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorGetCommand(frame);
        }
    }

    internal readonly struct IndicatorSetCommand : ICommand
    {
        public IndicatorSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.Set;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Create a version 1 Set command with a single indicator value.
        /// </summary>
        public static IndicatorSetCommand Create(byte value)
        {
            ReadOnlySpan<byte> commandParameters = [value];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorSetCommand(frame);
        }

        /// <summary>
        /// Create a version 2+ Set command with indicator objects.
        /// </summary>
        public static IndicatorSetCommand Create(IReadOnlyList<IndicatorObject> objects)
        {
            // Format: Indicator0Value(1) + Reserved|ObjectCount(1) + [IndicatorID + PropertyID + Value](3*N)
            Span<byte> commandParameters = stackalloc byte[2 + (3 * objects.Count)];
            commandParameters[0] = 0x00; // Indicator 0 Value = 0 when objects are present
            commandParameters[1] = (byte)(objects.Count & 0b0001_1111);

            for (int i = 0; i < objects.Count; i++)
            {
                int offset = 2 + (i * 3);
                commandParameters[offset] = (byte)objects[i].IndicatorId;
                commandParameters[offset + 1] = (byte)objects[i].PropertyId;
                commandParameters[offset + 2] = objects[i].Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorSetCommand(frame);
        }
    }

    internal readonly struct IndicatorReportCommand : ICommand
    {
        public IndicatorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Indicator;

        public static byte CommandId => (byte)IndicatorCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Create a version 1 Report command.
        /// </summary>
        public static IndicatorReportCommand Create(byte indicator0Value)
        {
            ReadOnlySpan<byte> commandParameters = [indicator0Value];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorReportCommand(frame);
        }

        /// <summary>
        /// Create a version 2+ Report command with indicator objects.
        /// </summary>
        public static IndicatorReportCommand Create(byte indicator0Value, IReadOnlyList<IndicatorObject> objects)
        {
            Span<byte> commandParameters = stackalloc byte[2 + (3 * objects.Count)];
            commandParameters[0] = indicator0Value;
            commandParameters[1] = (byte)(objects.Count & 0b0001_1111);

            for (int i = 0; i < objects.Count; i++)
            {
                int offset = 2 + (i * 3);
                commandParameters[offset] = (byte)objects[i].IndicatorId;
                commandParameters[offset + 1] = (byte)objects[i].PropertyId;
                commandParameters[offset + 2] = objects[i].Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new IndicatorReportCommand(frame);
        }

        public static IndicatorReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Indicator Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Indicator Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte indicator0Value = span[0];

            List<IndicatorObject> objects = [];
            if (span.Length >= 2)
            {
                int objectCount = span[1] & 0b0001_1111;
                int expectedLength = 2 + (3 * objectCount);
                if (span.Length < expectedLength)
                {
                    logger.LogWarning(
                        "Indicator Report frame has {ObjectCount} objects but only {Length} bytes (expected {ExpectedLength})",
                        objectCount,
                        span.Length,
                        expectedLength);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Indicator Report frame is too short for the declared object count");
                }

                for (int i = 0; i < objectCount; i++)
                {
                    int offset = 2 + (i * 3);
                    IndicatorId indicatorId = (IndicatorId)span[offset];
                    IndicatorPropertyId propertyId = (IndicatorPropertyId)span[offset + 1];
                    byte value = span[offset + 2];
                    objects.Add(new IndicatorObject(indicatorId, propertyId, value));
                }
            }

            return new IndicatorReport(indicator0Value, objects);
        }
    }
}
