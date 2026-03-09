using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Configuration Bulk Report received from a device for consecutive parameters.
/// </summary>
public readonly record struct ConfigurationBulkReport(
    /// <summary>
    /// The first parameter number in the reported range.
    /// </summary>
    ushort ParameterOffset,

    /// <summary>
    /// Whether all reported parameters have their factory default values.
    /// </summary>
    bool IsDefault,

    /// <summary>
    /// Whether this report is a handshake response to a Bulk Set command.
    /// </summary>
    bool IsHandshake,

    /// <summary>
    /// The size of each parameter value in bytes (1, 2, or 4).
    /// </summary>
    byte Size,

    /// <summary>
    /// The parameter values interpreted as signed integers, in order starting from
    /// <see cref="ParameterOffset"/>.
    /// </summary>
    IReadOnlyList<int> Values);

public sealed partial class ConfigurationCommandClass
{
    /// <summary>
    /// Event raised when a Configuration Bulk Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ConfigurationBulkReport>? OnConfigurationBulkReportReceived;

    /// <summary>
    /// Request the values of one or more consecutive configuration parameters.
    /// </summary>
    /// <param name="parameterOffset">The first parameter number in the range (0-65535).</param>
    /// <param name="numberOfParameters">The number of consecutive parameters to query (1-255).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of bulk reports. If the response spans multiple frames, all are aggregated.</returns>
    public async Task<ConfigurationBulkReport> BulkGetAsync(
        ushort parameterOffset,
        byte numberOfParameters,
        CancellationToken cancellationToken)
    {
        ConfigurationBulkGetCommand command = ConfigurationBulkGetCommand.Create(parameterOffset, numberOfParameters);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        List<int> allValues = [];
        ushort reportedOffset;
        bool isDefault;
        bool isHandshake;
        byte size;
        byte reportsToFollow;

        do
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<ConfigurationBulkReportCommand>(cancellationToken)
                .ConfigureAwait(false);
            (ConfigurationBulkReport partialReport, reportsToFollow) =
                ConfigurationBulkReportCommand.Parse(reportFrame, Logger);

            reportedOffset = partialReport.ParameterOffset;
            isDefault = partialReport.IsDefault;
            isHandshake = partialReport.IsHandshake;
            size = partialReport.Size;

            for (int i = 0; i < partialReport.Values.Count; i++)
            {
                allValues.Add(partialReport.Values[i]);
            }
        }
        while (reportsToFollow > 0);

        ConfigurationBulkReport result = new(reportedOffset, isDefault, isHandshake, size, allValues);

        // Update per-parameter cache
        for (int i = 0; i < allValues.Count; i++)
        {
            ushort paramNumber = (ushort)(reportedOffset + i);
            ConfigurationParameterFormat? format = GetParameterFormat(paramNumber);
            long value = format is ConfigurationParameterFormat.UnsignedInteger
                or ConfigurationParameterFormat.Enumerated
                or ConfigurationParameterFormat.BitField
                ? (uint)allValues[i]
                : allValues[i];
            _parameterValues[paramNumber] = new ConfigurationReport(paramNumber, size, format, value);
        }

        OnConfigurationBulkReportReceived?.Invoke(result);
        return result;
    }

    /// <summary>
    /// Set the value of one or more consecutive configuration parameters.
    /// </summary>
    /// <remarks>
    /// The parameter size is resolved from cached properties or a prior report for
    /// <paramref name="parameterOffset"/>. If the size is not yet known, a Bulk Get is issued
    /// automatically to discover it.
    /// </remarks>
    /// <param name="parameterOffset">The first parameter number in the range (0-65535).</param>
    /// <param name="values">The values to set, one per consecutive parameter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task BulkSetAsync(
        ushort parameterOffset,
        IReadOnlyList<long> values,
        CancellationToken cancellationToken)
    {
        byte? size = TryGetParameterSize(parameterOffset);
        if (!size.HasValue)
        {
            _ = await BulkGetAsync(parameterOffset, (byte)values.Count, cancellationToken).ConfigureAwait(false);
            size = _parameterValues[parameterOffset].Size;
        }

        var command = ConfigurationBulkSetCommand.Create(parameterOffset, size.Value, values, restoreDefault: false, handshake: false);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Restore the default factory values of one or more consecutive configuration parameters.
    /// </summary>
    /// <param name="parameterOffset">The first parameter number in the range (0-65535).</param>
    /// <param name="numberOfParameters">The number of consecutive parameters to reset (1-255).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task BulkSetDefaultAsync(
        ushort parameterOffset,
        byte numberOfParameters,
        CancellationToken cancellationToken)
    {
        var command = ConfigurationBulkSetCommand.CreateDefault(parameterOffset, numberOfParameters);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ConfigurationBulkSetCommand : ICommand
    {
        public ConfigurationBulkSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.BulkSet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationBulkSetCommand Create(
            ushort parameterOffset,
            byte size,
            IReadOnlyList<long> values,
            bool restoreDefault,
            bool handshake)
        {
            int parameterCount = values.Count;
            Span<byte> commandParameters = stackalloc byte[4 + (parameterCount * size)];
            parameterOffset.WriteBytesBE(commandParameters);
            commandParameters[2] = (byte)parameterCount;

            byte flags = (byte)(size & 0b0000_0111);
            if (restoreDefault)
            {
                flags |= 0b1000_0000;
            }

            if (handshake)
            {
                flags |= 0b0100_0000;
            }

            commandParameters[3] = flags;

            for (int i = 0; i < parameterCount; i++)
            {
                unchecked((int)values[i]).WriteSignedVariableSizeBE(commandParameters.Slice(4 + (i * size), size));
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationBulkSetCommand(frame);
        }

        public static ConfigurationBulkSetCommand CreateDefault(ushort parameterOffset, byte numberOfParameters)
        {
            // Default bit set, size = 1, no values needed (they are ignored per spec)
            Span<byte> commandParameters = stackalloc byte[4];
            parameterOffset.WriteBytesBE(commandParameters);
            commandParameters[2] = numberOfParameters;
            commandParameters[3] = 0b1000_0001; // Default=1, Size=1
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationBulkSetCommand(frame);
        }
    }

    internal readonly struct ConfigurationBulkGetCommand : ICommand
    {
        public ConfigurationBulkGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.BulkGet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationBulkGetCommand Create(ushort parameterOffset, byte numberOfParameters)
        {
            Span<byte> commandParameters = stackalloc byte[3];
            parameterOffset.WriteBytesBE(commandParameters);
            commandParameters[2] = numberOfParameters;
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationBulkGetCommand(frame);
        }
    }

    internal readonly struct ConfigurationBulkReportCommand : ICommand
    {
        public ConfigurationBulkReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.BulkReport;

        public CommandClassFrame Frame { get; }

        public static ConfigurationBulkReportCommand Create(
            ushort parameterOffset,
            byte reportsToFollow,
            bool isDefault,
            bool isHandshake,
            byte size,
            IReadOnlyList<int> values)
        {
            int parameterCount = values.Count;
            Span<byte> commandParameters = stackalloc byte[5 + (parameterCount * size)];
            parameterOffset.WriteBytesBE(commandParameters);
            commandParameters[2] = (byte)parameterCount;
            commandParameters[3] = reportsToFollow;

            byte flags = (byte)(size & 0b0000_0111);
            if (isDefault)
            {
                flags |= 0b1000_0000;
            }

            if (isHandshake)
            {
                flags |= 0b0100_0000;
            }

            commandParameters[4] = flags;

            for (int i = 0; i < parameterCount; i++)
            {
                values[i].WriteSignedVariableSizeBE(commandParameters.Slice(5 + (i * size), size));
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationBulkReportCommand(frame);
        }

        public static (ConfigurationBulkReport Report, byte ReportsToFollow) Parse(
            CommandClassFrame frame,
            ILogger logger)
        {
            // Minimum: 2 (offset) + 1 (count) + 1 (reports to follow) + 1 (flags) = 5
            if (frame.CommandParameters.Length < 5)
            {
                logger.LogWarning(
                    "Configuration Bulk Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Configuration Bulk Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            ushort parameterOffset = span[..2].ToUInt16BE();
            byte numberOfParameters = span[2];
            byte reportsToFollow = span[3];

            byte flags = span[4];
            bool isDefault = (flags & 0b1000_0000) != 0;
            bool isHandshake = (flags & 0b0100_0000) != 0;
            byte size = (byte)(flags & 0b0000_0111);

            int expectedLength = 5 + (numberOfParameters * size);
            if (frame.CommandParameters.Length < expectedLength)
            {
                logger.LogWarning(
                    "Configuration Bulk Report frame is too short for declared parameters ({Length} bytes, expected {Expected})",
                    frame.CommandParameters.Length,
                    expectedLength);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Configuration Bulk Report frame is too short for declared parameters");
            }

            List<int> values = new(numberOfParameters);
            for (int i = 0; i < numberOfParameters; i++)
            {
                int value = span.Slice(5 + (i * size), size).ReadSignedVariableSizeBE();
                values.Add(value);
            }

            ConfigurationBulkReport report = new(parameterOffset, isDefault, isHandshake, size, values);
            return (report, reportsToFollow);
        }
    }
}
