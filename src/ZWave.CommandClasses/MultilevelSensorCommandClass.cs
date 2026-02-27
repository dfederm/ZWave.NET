using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands for the Multilevel Sensor Command Class.
/// </summary>
public enum MultilevelSensorCommand : byte
{
    /// <summary>
    /// Request the supported Sensor Types from a supporting node.
    /// </summary>
    SupportedSensorGet = 0x01,

    /// <summary>
    /// Advertise the supported Sensor Types by a supporting node.
    /// </summary>
    SupportedSensorReport = 0x02,

    /// <summary>
    /// Retrieve the supported scales of the specific sensor type from the Multilevel Sensor device.
    /// </summary>
    SupportedScaleGet = 0x03,

    /// <summary>
    /// Request the current reading from a multilevel sensor.
    /// </summary>
    Get = 0x04,

    /// <summary>
    /// Advertise the current sensor reading for a supported sensor type.
    /// </summary>
    Report = 0x05,

    /// <summary>
    /// Advertise the supported scales of a specified multilevel sensor type.
    /// </summary>
    SupportedScaleReport = 0x06,
}

/// <summary>
/// Implements the Multilevel Sensor Command Class.
/// </summary>
[CommandClass(CommandClassId.MultilevelSensor)]
public sealed partial class MultilevelSensorCommandClass : CommandClass<MultilevelSensorCommand>
{
    internal MultilevelSensorCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(MultilevelSensorCommand command)
        => command switch
        {
            MultilevelSensorCommand.SupportedSensorGet => Version.HasValue ? Version >= 5 : null,
            MultilevelSensorCommand.SupportedScaleGet => Version.HasValue ? Version >= 5 : null,
            MultilevelSensorCommand.Get => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(MultilevelSensorCommand.SupportedSensorGet).GetValueOrDefault())
        {
            IReadOnlySet<MultilevelSensorType> supportedSensors = await GetSupportedSensorsAsync(cancellationToken).ConfigureAwait(false);
            foreach (MultilevelSensorType sensorType in supportedSensors)
            {
                if (IsCommandSupported(MultilevelSensorCommand.SupportedScaleGet).GetValueOrDefault())
                {
                    _ = await GetSupportedScalesAsync(sensorType, cancellationToken).ConfigureAwait(false);
                }

                _ = await GetAsync(sensorType, scale: null, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            _ = await GetAsync(sensorType: null, scale: null, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((MultilevelSensorCommand)frame.CommandId)
        {
            case MultilevelSensorCommand.Report:
            {
                MultilevelSensorReport report = MultilevelSensorReportCommand.Parse(frame, Logger);
                _sensorValues[report.SensorType] = report;
                OnMultilevelSensorReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
