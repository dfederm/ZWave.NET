using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Configuration Report received from a device for a single parameter.
/// </summary>
public readonly record struct ConfigurationReport(
    /// <summary>
    /// The parameter number.
    /// </summary>
    ushort ParameterNumber,

    /// <summary>
    /// The size of the parameter value in bytes (1, 2, or 4).
    /// </summary>
    byte Size,

    /// <summary>
    /// The parameter format, or <see langword="null"/> if unknown (version 1-2 devices or not yet interviewed).
    /// When <see langword="null"/>, the value is interpreted as signed per the V1-V2 spec default.
    /// </summary>
    ConfigurationParameterFormat? Format,

    /// <summary>
    /// The parameter value, correctly interpreted based on <see cref="Format"/>.
    /// Signed values are sign-extended; unsigned values are zero-extended.
    /// </summary>
    long Value);

public sealed partial class ConfigurationCommandClass
{
    private readonly Dictionary<ushort, ConfigurationReport> _parameterValues = [];

    /// <summary>
    /// Gets the cached parameter values, keyed by parameter number.
    /// </summary>
    public IReadOnlyDictionary<ushort, ConfigurationReport> ParameterValues => _parameterValues;

    /// <summary>
    /// Event raised when a Configuration Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ConfigurationReport>? OnConfigurationReportReceived;

    /// <summary>
    /// Request the value of a configuration parameter.
    /// </summary>
    /// <param name="parameterNumber">The parameter number to query (0-255).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration report for the requested parameter.</returns>
    public async Task<ConfigurationReport> GetAsync(byte parameterNumber, CancellationToken cancellationToken)
    {
        ConfigurationGetCommand command = ConfigurationGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ConfigurationReportCommand>(
            predicate: frame => frame.CommandParameters.Length > 0
                && frame.CommandParameters.Span[0] == parameterNumber,
            cancellationToken).ConfigureAwait(false);
        ConfigurationReport report = ConfigurationReportCommand.Parse(reportFrame, Logger, GetParameterFormat(parameterNumber));
        _parameterValues[report.ParameterNumber] = report;
        OnConfigurationReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the value of a configuration parameter.
    /// </summary>
    /// <remarks>
    /// The parameter size is resolved from cached properties (V3+ interview) or a prior
    /// <see cref="GetAsync"/> result. If the size is not yet known, a Get is issued automatically
    /// to discover it.
    /// </remarks>
    /// <param name="parameterNumber">The parameter number to set (0-255).</param>
    /// <param name="value">The value to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(byte parameterNumber, long value, CancellationToken cancellationToken)
    {
        byte size = await GetParameterSizeAsync(parameterNumber, cancellationToken).ConfigureAwait(false);
        var command = ConfigurationSetCommand.Create(parameterNumber, size, value);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Restore the default factory value of a configuration parameter.
    /// </summary>
    /// <param name="parameterNumber">The parameter number to reset (0-255).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetDefaultAsync(byte parameterNumber, CancellationToken cancellationToken)
    {
        var command = ConfigurationSetCommand.CreateDefault(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private ConfigurationParameterFormat? GetParameterFormat(ushort parameterNumber)
        => _parameterProperties?.TryGetValue(parameterNumber, out ConfigurationParameterProperties props) == true
            ? props.Format
            : null;

    private byte? TryGetParameterSize(ushort parameterNumber)
    {
        if (_parameterProperties?.TryGetValue(parameterNumber, out ConfigurationParameterProperties props) == true
            && props.Size > 0)
        {
            return props.Size;
        }

        if (_parameterValues.TryGetValue(parameterNumber, out ConfigurationReport report)
            && report.Size > 0)
        {
            return report.Size;
        }

        return null;
    }

    private async Task<byte> GetParameterSizeAsync(byte parameterNumber, CancellationToken cancellationToken)
    {
        byte? size = TryGetParameterSize(parameterNumber);
        if (size.HasValue)
        {
            return size.Value;
        }

        // Size not cached — issue a Get to discover it.
        _ = await GetAsync(parameterNumber, cancellationToken).ConfigureAwait(false);
        return _parameterValues[parameterNumber].Size;
    }

    /// <summary>
    /// Reads a configuration value using signed or unsigned interpretation based on the format.
    /// Returns <see cref="long"/> which can hold both <see cref="int"/> and <see cref="uint"/> ranges.
    /// </summary>
    internal static long ReadValue(ReadOnlySpan<byte> bytes, ConfigurationParameterFormat? format)
        => format is ConfigurationParameterFormat.UnsignedInteger
            or ConfigurationParameterFormat.Enumerated
            or ConfigurationParameterFormat.BitField
            ? bytes.ReadUnsignedVariableSizeBE()
            : bytes.ReadSignedVariableSizeBE();

    internal readonly struct ConfigurationSetCommand : ICommand
    {
        public ConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ConfigurationSetCommand Create(byte parameterNumber, byte size, long value)
        {
            Span<byte> commandParameters = stackalloc byte[2 + size];
            commandParameters[0] = parameterNumber;
            commandParameters[1] = (byte)(size & 0b0000_0111);
            unchecked((int)value).WriteSignedVariableSizeBE(commandParameters[2..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationSetCommand(frame);
        }

        public static ConfigurationSetCommand CreateDefault(byte parameterNumber)
        {
            // Default bit is bit 7 of byte 1, size can be anything valid (use 1).
            ReadOnlySpan<byte> commandParameters = [parameterNumber, 0b1000_0001, 0x00];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationSetCommand(frame);
        }
    }

    internal readonly struct ConfigurationGetCommand : ICommand
    {
        public ConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ConfigurationGetCommand Create(byte parameterNumber)
        {
            ReadOnlySpan<byte> commandParameters = [parameterNumber];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationGetCommand(frame);
        }
    }

    internal readonly struct ConfigurationReportCommand : ICommand
    {
        public ConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ConfigurationReportCommand Create(
            byte parameterNumber,
            byte size,
            long value,
            ConfigurationParameterFormat? format = null)
        {
            Span<byte> commandParameters = stackalloc byte[2 + size];
            commandParameters[0] = parameterNumber;
            commandParameters[1] = (byte)(size & 0b0000_0111);
            unchecked((int)value).WriteSignedVariableSizeBE(commandParameters[2..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationReportCommand(frame);
        }

        public static ConfigurationReport Parse(
            CommandClassFrame frame,
            ILogger logger,
            ConfigurationParameterFormat? format = null)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning(
                    "Configuration Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Configuration Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            byte parameterNumber = span[0];
            byte size = (byte)(span[1] & 0b0000_0111);

            if (frame.CommandParameters.Length < 2 + size)
            {
                logger.LogWarning(
                    "Configuration Report frame is too short for declared size ({Length} bytes, expected {Expected})",
                    frame.CommandParameters.Length,
                    2 + size);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Configuration Report frame is too short for declared size");
            }

            long value = ReadValue(span.Slice(2, size), format);

            return new ConfigurationReport(parameterNumber, size, format, value);
        }
    }
}
