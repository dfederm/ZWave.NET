using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the type of a binary sensor.
/// </summary>
public enum BinarySensorType : byte
{
    /// <summary>
    /// General purpose sensor.
    /// </summary>
    GeneralPurpose = 0x01,

    /// <summary>
    /// Smoke sensor.
    /// </summary>
    Smoke = 0x02,

    /// <summary>
    /// Carbon monoxide sensor.
    /// </summary>
    CO = 0x03,

    /// <summary>
    /// Carbon dioxide sensor.
    /// </summary>
    CO2 = 0x04,

    /// <summary>
    /// Heat sensor.
    /// </summary>
    Heat = 0x05,

    /// <summary>
    /// Water sensor.
    /// </summary>
    Water = 0x06,

    /// <summary>
    /// Freeze sensor.
    /// </summary>
    Freeze = 0x07,

    /// <summary>
    /// Tamper sensor.
    /// </summary>
    Tamper = 0x08,

    /// <summary>
    /// Auxiliary sensor.
    /// </summary>
    Aux = 0x09,

    /// <summary>
    /// Door/window sensor.
    /// </summary>
    DoorWindow = 0x0a,

    /// <summary>
    /// Tilt sensor.
    /// </summary>
    Tilt = 0x0b,

    /// <summary>
    /// Motion sensor.
    /// </summary>
    Motion = 0x0c,

    /// <summary>
    /// Glass break sensor.
    /// </summary>
    GlassBreak = 0x0d,

    /// <summary>
    /// Request the first supported sensor type.
    /// </summary>
    FirstSupported = 0xff,
}

public enum BinarySensorCommand : byte
{
    /// <summary>
    /// Request the status of the specific sensor device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise a sensor value.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported sensor types from the binary sensor device.
    /// </summary>
    SupportedGet = 0x01,

    /// <summary>
    /// Indicates the supported sensor types of the binary sensor device.
    /// </summary>
    SupportedReport = 0x04,
}

[CommandClass(CommandClassId.BinarySensor)]
public sealed partial class BinarySensorCommandClass : CommandClass<BinarySensorCommand>
{
    internal BinarySensorCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(BinarySensorCommand command)
        => command switch
        {
            BinarySensorCommand.Get => true,
            BinarySensorCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(BinarySensorCommand.SupportedGet).GetValueOrDefault())
        {
            IReadOnlySet<BinarySensorType> supportedSensorTypes = await GetSupportedSensorTypesAsync(cancellationToken).ConfigureAwait(false);
            foreach (BinarySensorType sensorType in supportedSensorTypes)
            {
                _ = await GetAsync(sensorType, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            _ = await GetAsync(sensorType: null, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((BinarySensorCommand)frame.CommandId)
        {
            case BinarySensorCommand.Report:
            {
                BinarySensorReport report = BinarySensorReportCommand.Parse(frame, Logger);
                LastReport = report;
                BinarySensorType key = report.SensorType.GetValueOrDefault(BinarySensorType.FirstSupported);
                _sensorValues[key] = report.Value;
                OnBinarySensorReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
