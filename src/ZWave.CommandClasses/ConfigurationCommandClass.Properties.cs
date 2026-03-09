using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the properties of a configuration parameter as advertised by the device.
/// </summary>
public readonly record struct ConfigurationParameterProperties(
    /// <summary>
    /// The parameter number.
    /// </summary>
    ushort ParameterNumber,

    /// <summary>
    /// The format of the parameter value.
    /// </summary>
    ConfigurationParameterFormat Format,

    /// <summary>
    /// The size of the parameter value in bytes (0, 1, 2, or 4).
    /// A size of 0 indicates the parameter is unassigned.
    /// </summary>
    byte Size,

    /// <summary>
    /// The minimum value the parameter can assume. Zero when <see cref="Size"/> is 0.
    /// Interpreted according to <see cref="Format"/>.
    /// </summary>
    long MinValue,

    /// <summary>
    /// The maximum value the parameter can assume. Zero when <see cref="Size"/> is 0.
    /// For bit field parameters, each supported bit is set to 1.
    /// Interpreted according to <see cref="Format"/>.
    /// </summary>
    long MaxValue,

    /// <summary>
    /// The default value of the parameter. Zero when <see cref="Size"/> is 0.
    /// Interpreted according to <see cref="Format"/>.
    /// </summary>
    long DefaultValue,

    /// <summary>
    /// Whether the parameter is read-only (version 4+).
    /// <see langword="null"/> if the field is not present in the report.
    /// </summary>
    bool? ReadOnly,

    /// <summary>
    /// Whether changing the parameter alters the node's capabilities and requires re-inclusion (version 4+).
    /// <see langword="null"/> if the field is not present in the report.
    /// </summary>
    bool? AlteringCapabilities,

    /// <summary>
    /// Whether the parameter is intended for advanced use only (version 4+).
    /// <see langword="null"/> if the field is not present in the report.
    /// </summary>
    bool? Advanced,

    /// <summary>
    /// Whether the node ignores Bulk commands (version 4+).
    /// <see langword="null"/> if the field is not present in the report.
    /// </summary>
    bool? NoBulkSupport);

public sealed partial class ConfigurationCommandClass
{
    private Dictionary<ushort, ConfigurationParameterProperties>? _parameterProperties;

    /// <summary>
    /// Gets the cached parameter properties discovered during interview, keyed by parameter number.
    /// <see langword="null"/> if properties have not been queried (version 1-2 devices, or not yet interviewed).
    /// </summary>
    public IReadOnlyDictionary<ushort, ConfigurationParameterProperties>? ParameterProperties => _parameterProperties;

    /// <summary>
    /// Request the properties of a configuration parameter.
    /// </summary>
    /// <param name="parameterNumber">The parameter number to query (0-65535).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The properties of the parameter.</returns>
    public async Task<ConfigurationParameterProperties> GetPropertiesAsync(
        ushort parameterNumber,
        CancellationToken cancellationToken)
    {
        ConfigurationPropertiesGetCommand command = ConfigurationPropertiesGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ConfigurationPropertiesReportCommand>(cancellationToken)
            .ConfigureAwait(false);
        (ConfigurationParameterProperties properties, _) = ConfigurationPropertiesReportCommand.Parse(reportFrame, Logger);

        // Cache properties for non-unassigned parameters
        if (properties.Size > 0)
        {
            _parameterProperties ??= [];
            _parameterProperties[properties.ParameterNumber] = properties;
        }

        return properties;
    }

    /// <summary>
    /// Reset all configuration parameters to their default values.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ResetToDefaultAsync(CancellationToken cancellationToken)
    {
        var command = ConfigurationDefaultResetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ConfigurationDefaultResetCommand : ICommand
    {
        public ConfigurationDefaultResetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.DefaultReset;

        public CommandClassFrame Frame { get; }

        public static ConfigurationDefaultResetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ConfigurationDefaultResetCommand(frame);
        }
    }

    internal readonly struct ConfigurationPropertiesGetCommand : ICommand
    {
        public ConfigurationPropertiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.PropertiesGet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationPropertiesGetCommand Create(ushort parameterNumber)
        {
            Span<byte> commandParameters = stackalloc byte[2];
            parameterNumber.WriteBytesBE(commandParameters);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationPropertiesGetCommand(frame);
        }
    }

    internal readonly struct ConfigurationPropertiesReportCommand : ICommand
    {
        public ConfigurationPropertiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.PropertiesReport;

        public CommandClassFrame Frame { get; }

        public static ConfigurationPropertiesReportCommand Create(
            ushort parameterNumber,
            ConfigurationParameterFormat format,
            byte size,
            long minValue,
            long maxValue,
            long defaultValue,
            ushort nextParameterNumber,
            bool? readOnly = null,
            bool? alteringCapabilities = null,
            bool? advanced = null,
            bool? noBulkSupport = null)
        {
            // V3: 2 (param#) + 1 (format|size) + 3*size (min/max/default) + 2 (next param#) = 5 + 3*size
            // V4: + 1 (flags) = 6 + 3*size
            bool hasV4Fields = readOnly.HasValue || alteringCapabilities.HasValue || advanced.HasValue || noBulkSupport.HasValue;
            int length = 5 + (3 * size) + (hasV4Fields ? 1 : 0);
            Span<byte> commandParameters = stackalloc byte[length];
            parameterNumber.WriteBytesBE(commandParameters);

            byte formatAndSize = (byte)(((byte)format << 3) | (size & 0b0000_0111));
            if (readOnly == true)
            {
                formatAndSize |= 0b0100_0000;
            }

            if (alteringCapabilities == true)
            {
                formatAndSize |= 0b1000_0000;
            }

            commandParameters[2] = formatAndSize;

            int offset = 3;
            if (size > 0)
            {
                unchecked((int)minValue).WriteSignedVariableSizeBE(commandParameters.Slice(offset, size));
                offset += size;
                unchecked((int)maxValue).WriteSignedVariableSizeBE(commandParameters.Slice(offset, size));
                offset += size;
                unchecked((int)defaultValue).WriteSignedVariableSizeBE(commandParameters.Slice(offset, size));
                offset += size;
            }

            nextParameterNumber.WriteBytesBE(commandParameters.Slice(offset, 2));
            offset += 2;

            if (hasV4Fields)
            {
                byte v4Flags = 0;
                if (noBulkSupport == true)
                {
                    v4Flags |= 0b0000_0010;
                }

                if (advanced == true)
                {
                    v4Flags |= 0b0000_0001;
                }

                commandParameters[offset] = v4Flags;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationPropertiesReportCommand(frame);
        }

        public static (ConfigurationParameterProperties Properties, ushort NextParameterNumber) Parse(
            CommandClassFrame frame,
            ILogger logger)
        {
            // Minimum: 2 (param#) + 1 (format|size) + 2 (next param#) = 5 (when size=0)
            if (frame.CommandParameters.Length < 5)
            {
                logger.LogWarning(
                    "Configuration Properties Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Configuration Properties Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            ushort parameterNumber = span[..2].ToUInt16BE();

            byte formatAndSize = span[2];
            ConfigurationParameterFormat format = (ConfigurationParameterFormat)((formatAndSize >> 3) & 0b0000_0111);
            byte size = (byte)(formatAndSize & 0b0000_0111);
            bool readOnlyBit = (formatAndSize & 0b0100_0000) != 0;
            bool alteringCapabilitiesBit = (formatAndSize & 0b1000_0000) != 0;

            long minValue = 0;
            long maxValue = 0;
            long defaultValue = 0;

            int valueFieldsLength = 3 * size;
            int expectedMinLength = 3 + valueFieldsLength + 2;

            if (size > 0)
            {
                if (frame.CommandParameters.Length < expectedMinLength)
                {
                    logger.LogWarning(
                        "Configuration Properties Report frame is too short for declared size ({Length} bytes, expected {Expected})",
                        frame.CommandParameters.Length,
                        expectedMinLength);
                    ZWaveException.Throw(
                        ZWaveErrorCode.InvalidPayload,
                        "Configuration Properties Report frame is too short for declared size");
                }

                int offset = 3;
                minValue = ReadValue(span.Slice(offset, size), format);
                offset += size;
                maxValue = ReadValue(span.Slice(offset, size), format);
                offset += size;
                defaultValue = ReadValue(span.Slice(offset, size), format);
            }

            int nextParamOffset = 3 + valueFieldsLength;
            ushort nextParameterNumber = span.Slice(nextParamOffset, 2).ToUInt16BE();

            // V4 fields: check if additional byte exists after the next parameter number
            bool? readOnly = null;
            bool? alteringCapabilities = null;
            bool? advanced = null;
            bool? noBulkSupport = null;

            int v4FlagsOffset = nextParamOffset + 2;
            if (frame.CommandParameters.Length > v4FlagsOffset)
            {
                // V4 fields present
                readOnly = readOnlyBit;
                alteringCapabilities = alteringCapabilitiesBit;

                byte v4Flags = span[v4FlagsOffset];
                noBulkSupport = (v4Flags & 0b0000_0010) != 0;
                advanced = (v4Flags & 0b0000_0001) != 0;
            }

            ConfigurationParameterProperties properties = new(
                parameterNumber,
                format,
                size,
                minValue,
                maxValue,
                defaultValue,
                readOnly,
                alteringCapabilities,
                advanced,
                noBulkSupport);
            return (properties, nextParameterNumber);
        }
    }
}
