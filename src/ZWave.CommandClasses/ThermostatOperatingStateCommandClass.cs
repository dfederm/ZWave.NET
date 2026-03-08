using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The operating state of a thermostat device.
/// </summary>
public enum ThermostatOperatingState : byte
{
    /// <summary>
    /// The thermostat is idle.
    /// </summary>
    Idle = 0x00,

    /// <summary>
    /// The thermostat is heating.
    /// </summary>
    Heating = 0x01,

    /// <summary>
    /// The thermostat is cooling.
    /// </summary>
    Cooling = 0x02,

    /// <summary>
    /// The thermostat is running the fan only.
    /// </summary>
    FanOnly = 0x03,

    /// <summary>
    /// Pending heat. Short cycle prevention feature used in heat pump applications to protect the compressor.
    /// </summary>
    PendingHeat = 0x04,

    /// <summary>
    /// Pending cool. Short cycle prevention feature used in heat pump applications to protect the compressor.
    /// </summary>
    PendingCool = 0x05,

    /// <summary>
    /// The thermostat is in vent/economizer mode.
    /// </summary>
    VentEconomizer = 0x06,

    /// <summary>
    /// The thermostat is using auxiliary heating.
    /// </summary>
    AuxHeating = 0x07,

    /// <summary>
    /// The thermostat is in 2nd stage heating.
    /// </summary>
    SecondStageHeating = 0x08,

    /// <summary>
    /// The thermostat is in 2nd stage cooling.
    /// </summary>
    SecondStageCooling = 0x09,

    /// <summary>
    /// The thermostat is in 2nd stage auxiliary heat.
    /// </summary>
    SecondStageAuxHeat = 0x0A,

    /// <summary>
    /// The thermostat is in 3rd stage auxiliary heat.
    /// </summary>
    ThirdStageAuxHeat = 0x0B,
}

public enum ThermostatOperatingStateCommand : byte
{
    /// <summary>
    /// Request the operating state logging supported by the device.
    /// </summary>
    LoggingSupportedGet = 0x01,

    /// <summary>
    /// Request the operating state from the device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the operating state of the device.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Report the operating state logging supported by the device.
    /// </summary>
    LoggingSupportedReport = 0x04,

    /// <summary>
    /// Request the operating state logging from the device.
    /// </summary>
    LoggingGet = 0x05,

    /// <summary>
    /// Report the operating state logged for requested operating states.
    /// </summary>
    LoggingReport = 0x06,
}

/// <summary>
/// Represents a Thermostat Operating State Report received from a device.
/// </summary>
public readonly record struct ThermostatOperatingStateReport(
    /// <summary>
    /// The current operating state of the thermostat.
    /// </summary>
    ThermostatOperatingState OperatingState);

/// <summary>
/// Represents a logged operating state entry with usage statistics for today and yesterday.
/// </summary>
public readonly record struct ThermostatOperatingStateLogEntry(
    /// <summary>
    /// The operating state this log entry is for.
    /// </summary>
    ThermostatOperatingState OperatingState,

    /// <summary>
    /// The time the thermostat has been in this operating state since 12:00 AM of the current day.
    /// </summary>
    TimeSpan UsageToday,

    /// <summary>
    /// The time the thermostat was in this operating state between 12:00 AM and 11:59 PM of the previous day.
    /// </summary>
    TimeSpan UsageYesterday);

/// <summary>
/// The Thermostat Operating State Command Class is used to obtain the operating state and
/// operating state logs of the thermostat.
/// </summary>
[CommandClass(CommandClassId.ThermostatOperatingState)]
public sealed class ThermostatOperatingStateCommandClass : CommandClass<ThermostatOperatingStateCommand>
{
    internal ThermostatOperatingStateCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last operating state report received from the device.
    /// </summary>
    public ThermostatOperatingStateReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the set of operating states for which logging is supported, or <c>null</c> if not yet queried or
    /// the device does not support logging (version 1).
    /// </summary>
    public IReadOnlySet<ThermostatOperatingState>? SupportedLoggingStates { get; private set; }

    /// <summary>
    /// Event raised when a Thermostat Operating State Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ThermostatOperatingStateReport>? OnThermostatOperatingStateReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(ThermostatOperatingStateCommand command)
        => command switch
        {
            ThermostatOperatingStateCommand.Get => true,
            ThermostatOperatingStateCommand.LoggingSupportedGet => Version.HasValue ? Version >= 2 : null,
            ThermostatOperatingStateCommand.LoggingGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(ThermostatOperatingStateCommand.LoggingSupportedGet).GetValueOrDefault())
        {
            _ = await GetSupportedLoggingStatesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request the current operating state from the device.
    /// </summary>
    public async Task<ThermostatOperatingStateReport> GetAsync(CancellationToken cancellationToken)
    {
        ThermostatOperatingStateGetCommand command = ThermostatOperatingStateGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ThermostatOperatingStateReportCommand>(cancellationToken).ConfigureAwait(false);
        ThermostatOperatingStateReport report = ThermostatOperatingStateReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnThermostatOperatingStateReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Request the supported operating state logging types from the device.
    /// </summary>
    public async Task<IReadOnlySet<ThermostatOperatingState>> GetSupportedLoggingStatesAsync(CancellationToken cancellationToken)
    {
        ThermostatOperatingStateLoggingSupportedGetCommand command = ThermostatOperatingStateLoggingSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ThermostatOperatingStateLoggingSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        HashSet<ThermostatOperatingState> supportedStates = ThermostatOperatingStateLoggingSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedLoggingStates = supportedStates;
        return supportedStates;
    }

    /// <summary>
    /// Request operating state logs for the specified states from the device.
    /// Results are aggregated across multiple report frames if necessary.
    /// </summary>
    public async Task<IReadOnlyList<ThermostatOperatingStateLogEntry>> GetLoggingAsync(
        IReadOnlySet<ThermostatOperatingState> requestedStates,
        CancellationToken cancellationToken)
    {
        var command = ThermostatOperatingStateLoggingGetCommand.Create(requestedStates);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        List<ThermostatOperatingStateLogEntry> allEntries = [];
        byte reportsToFollow;
        do
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<ThermostatOperatingStateLoggingReportCommand>(cancellationToken).ConfigureAwait(false);
            reportsToFollow = ThermostatOperatingStateLoggingReportCommand.ParseInto(reportFrame, allEntries, Logger);
        }
        while (reportsToFollow > 0);

        return allEntries;
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ThermostatOperatingStateCommand)frame.CommandId)
        {
            case ThermostatOperatingStateCommand.Report:
            {
                ThermostatOperatingStateReport report = ThermostatOperatingStateReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnThermostatOperatingStateReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct ThermostatOperatingStateGetCommand : ICommand
    {
        public ThermostatOperatingStateGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ThermostatOperatingStateGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatOperatingStateGetCommand(frame);
        }
    }

    internal readonly struct ThermostatOperatingStateReportCommand : ICommand
    {
        public ThermostatOperatingStateReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ThermostatOperatingStateReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Thermostat Operating State Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Thermostat Operating State Report frame is too short");
            }

            // V1: upper 4 bits reserved, lower 4 bits = operating state
            // V2: full 8-bit operating state field
            // Per forward-compatibility, do NOT mask reserved bits.
            ThermostatOperatingState operatingState = (ThermostatOperatingState)frame.CommandParameters.Span[0];
            return new ThermostatOperatingStateReport(operatingState);
        }
    }

    internal readonly struct ThermostatOperatingStateLoggingSupportedGetCommand : ICommand
    {
        public ThermostatOperatingStateLoggingSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.LoggingSupportedGet;

        public CommandClassFrame Frame { get; }

        public static ThermostatOperatingStateLoggingSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatOperatingStateLoggingSupportedGetCommand(frame);
        }
    }

    internal readonly struct ThermostatOperatingStateLoggingSupportedReportCommand : ICommand
    {
        public ThermostatOperatingStateLoggingSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.LoggingSupportedReport;

        public CommandClassFrame Frame { get; }

        public static HashSet<ThermostatOperatingState> Parse(CommandClassFrame frame, ILogger logger)
        {
            // Per spec: bit 0 in bitmask 1 is NOT allocated and MUST be zero.
            // Use startBit: 1 to skip bit 0.
            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span;
            HashSet<ThermostatOperatingState> supportedStates = BitMaskHelper.ParseBitMask<ThermostatOperatingState>(bitMask, startBit: 1);
            return supportedStates;
        }
    }

    internal readonly struct ThermostatOperatingStateLoggingGetCommand : ICommand
    {
        public ThermostatOperatingStateLoggingGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.LoggingGet;

        public CommandClassFrame Frame { get; }

        public static ThermostatOperatingStateLoggingGetCommand Create(IReadOnlySet<ThermostatOperatingState> requestedStates)
        {
            // Build bitmask for the requested states.
            // Per spec: bit 0 in bitmask 1 is not allocated and MUST be set to zero.
            // Find the highest bit needed to determine the bitmask size.
            int maxBit = 0;
            foreach (ThermostatOperatingState state in requestedStates)
            {
                int bit = (byte)state;
                if (bit > maxBit)
                {
                    maxBit = bit;
                }
            }

            int byteCount = (maxBit / 8) + 1;
            Span<byte> bitMask = stackalloc byte[byteCount];

            foreach(ThermostatOperatingState state in requestedStates)
            {
                int bit = (byte)state;
                bitMask[bit / 8] |= (byte)(1 << (bit % 8));
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, bitMask);
            return new ThermostatOperatingStateLoggingGetCommand(frame);
        }
    }

    internal readonly struct ThermostatOperatingStateLoggingReportCommand : ICommand
    {
        public ThermostatOperatingStateLoggingReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatOperatingState;

        public static byte CommandId => (byte)ThermostatOperatingStateCommand.LoggingReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Parse a single Thermostat Operating State Logging Report frame, appending log entries to the provided list.
        /// </summary>
        /// <returns>The reports-to-follow count from this frame.</returns>
        public static byte ParseInto(
            CommandClassFrame frame,
            List<ThermostatOperatingStateLogEntry> entries,
            ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Thermostat Operating State Logging Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Thermostat Operating State Logging Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte reportsToFollow = span[0];

            // Each log entry is 5 bytes: Reserved(4 bits) + LogType(4 bits), UsageTodayHours, UsageTodayMinutes, UsageYesterdayHours, UsageYesterdayMinutes
            int offset = 1;

            while (offset + 5 <= span.Length)
            {
                // Byte layout: upper 4 bits = reserved, lower 4 bits = operating state log type
                ThermostatOperatingState logType = (ThermostatOperatingState)(span[offset] & 0x0F);
                byte todayHours = span[offset + 1];
                byte todayMinutes = span[offset + 2];
                byte yesterdayHours = span[offset + 3];
                byte yesterdayMinutes = span[offset + 4];

                TimeSpan usageToday = new(todayHours, todayMinutes, 0);
                TimeSpan usageYesterday = new(yesterdayHours, yesterdayMinutes, 0);

                entries.Add(new ThermostatOperatingStateLogEntry(logType, usageToday, usageYesterday));
                offset += 5;
            }

            return reportsToFollow;
        }
    }
}
