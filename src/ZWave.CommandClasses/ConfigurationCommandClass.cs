using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The format of a configuration parameter value as advertised by the Configuration Properties Report.
/// </summary>
public enum ConfigurationParameterFormat : byte
{
    /// <summary>
    /// The parameter value is a signed integer using 2's complement encoding.
    /// </summary>
    SignedInteger = 0x00,

    /// <summary>
    /// The parameter value is an unsigned integer.
    /// </summary>
    UnsignedInteger = 0x01,

    /// <summary>
    /// The parameter value is an enumerated value (treated as unsigned integer).
    /// </summary>
    Enumerated = 0x02,

    /// <summary>
    /// The parameter value is a bit field where each individual bit can be set or reset.
    /// </summary>
    BitField = 0x03,
}

/// <summary>
/// Defines the commands for the Configuration Command Class.
/// </summary>
public enum ConfigurationCommand : byte
{
    /// <summary>
    /// Reset all configuration parameters to their default values.
    /// </summary>
    DefaultReset = 0x01,

    /// <summary>
    /// Set the value of a configuration parameter.
    /// </summary>
    Set = 0x04,

    /// <summary>
    /// Request the value of a configuration parameter.
    /// </summary>
    Get = 0x05,

    /// <summary>
    /// Report the value of a configuration parameter.
    /// </summary>
    Report = 0x06,

    /// <summary>
    /// Set the value of one or more consecutive configuration parameters.
    /// </summary>
    BulkSet = 0x07,

    /// <summary>
    /// Request the value of one or more consecutive configuration parameters.
    /// </summary>
    BulkGet = 0x08,

    /// <summary>
    /// Report the value of one or more consecutive configuration parameters.
    /// </summary>
    BulkReport = 0x09,

    /// <summary>
    /// Request the name of a configuration parameter.
    /// </summary>
    NameGet = 0x0A,

    /// <summary>
    /// Report the name of a configuration parameter.
    /// </summary>
    NameReport = 0x0B,

    /// <summary>
    /// Request usage information for a configuration parameter.
    /// </summary>
    InfoGet = 0x0C,

    /// <summary>
    /// Report usage information for a configuration parameter.
    /// </summary>
    InfoReport = 0x0D,

    /// <summary>
    /// Request the properties of a configuration parameter.
    /// </summary>
    PropertiesGet = 0x0E,

    /// <summary>
    /// Report the properties of a configuration parameter.
    /// </summary>
    PropertiesReport = 0x0F,
}

/// <summary>
/// The Configuration Command Class allows product-specific configuration parameters to be changed.
/// </summary>
[CommandClass(CommandClassId.Configuration)]
public sealed partial class ConfigurationCommandClass : CommandClass<ConfigurationCommand>
{
    internal ConfigurationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ConfigurationCommand command)
        => command switch
        {
            ConfigurationCommand.Set => true,
            ConfigurationCommand.Get => true,
            ConfigurationCommand.BulkSet => Version.HasValue ? Version >= 2 : null,
            ConfigurationCommand.BulkGet => Version.HasValue ? Version >= 2 : null,
            ConfigurationCommand.NameGet => Version.HasValue ? Version >= 3 : null,
            ConfigurationCommand.InfoGet => Version.HasValue ? Version >= 3 : null,
            ConfigurationCommand.PropertiesGet => Version.HasValue ? Version >= 3 : null,
            ConfigurationCommand.DefaultReset => Version.HasValue ? Version >= 4 : null,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        // V3+ supports parameter discovery via the Properties Get chain.
        // Start at parameter 0 to discover the first available parameter, then follow
        // the next-parameter chain until it returns 0x0000 (no more parameters).
        if (IsCommandSupported(ConfigurationCommand.PropertiesGet).GetValueOrDefault())
        {
            ushort nextParameter = 0;
            do
            {
                ConfigurationPropertiesGetCommand command = ConfigurationPropertiesGetCommand.Create(nextParameter);
                await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
                CommandClassFrame reportFrame = await AwaitNextReportAsync<ConfigurationPropertiesReportCommand>(cancellationToken)
                    .ConfigureAwait(false);
                (ConfigurationParameterProperties properties, nextParameter) =
                    ConfigurationPropertiesReportCommand.Parse(reportFrame, Logger);

                // Cache properties for non-unassigned parameters (Size 0 = unassigned).
                if (properties.Size > 0)
                {
                    _parameterProperties ??= [];
                    _parameterProperties[properties.ParameterNumber] = properties;
                }
            }
            while (nextParameter != 0);
        }
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ConfigurationCommand)frame.CommandId)
        {
            case ConfigurationCommand.Report:
            {
                ConfigurationReport report = ConfigurationReportCommand.Parse(frame, Logger, GetParameterFormat(frame.CommandParameters.Span[0]));
                _parameterValues[report.ParameterNumber] = report;
                OnConfigurationReportReceived?.Invoke(report);
                break;
            }
            case ConfigurationCommand.BulkReport:
            {
                (ConfigurationBulkReport bulkReport, _) = ConfigurationBulkReportCommand.Parse(frame, Logger);
                for (int i = 0; i < bulkReport.Values.Count; i++)
                {
                    ushort parameterNumber = (ushort)(bulkReport.ParameterOffset + i);
                    ConfigurationParameterFormat? format = GetParameterFormat(parameterNumber);
                    long value = format is ConfigurationParameterFormat.UnsignedInteger
                        or ConfigurationParameterFormat.Enumerated
                        or ConfigurationParameterFormat.BitField
                        ? (uint)bulkReport.Values[i]
                        : bulkReport.Values[i];
                    ConfigurationReport singleReport = new(
                        parameterNumber,
                        bulkReport.Size,
                        format,
                        value);
                    _parameterValues[parameterNumber] = singleReport;
                }

                OnConfigurationBulkReportReceived?.Invoke(bulkReport);
                break;
            }
        }
    }

    /// <summary>
    /// Shared parser for Name Report and Info Report frames.
    /// Both have the same structure: 2 bytes param#, 1 byte reports-to-follow, then UTF-8 text bytes.
    /// </summary>
    internal static byte ParseTextReportInto(
        CommandClassFrame frame,
        List<byte> textBytes,
        ILogger logger,
        string reportName)
    {
        if (frame.CommandParameters.Length < 3)
        {
            logger.LogWarning(
                "Configuration {ReportName} Report frame is too short ({Length} bytes)",
                reportName,
                frame.CommandParameters.Length);
            ZWaveException.Throw(
                ZWaveErrorCode.InvalidPayload,
                $"Configuration {reportName} Report frame is too short");
        }

        ReadOnlySpan<byte> span = frame.CommandParameters.Span;
        byte reportsToFollow = span[2];

        if (span.Length > 3)
        {
            ReadOnlySpan<byte> textData = span[3..];
            for (int i = 0; i < textData.Length; i++)
            {
                textBytes.Add(textData[i]);
            }
        }

        return reportsToFollow;
    }
}
