using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a battery level value.
/// </summary>
public readonly struct BatteryLevel
{
    public BatteryLevel(byte value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the raw battery level byte.
    /// </summary>
    public byte Value { get; }

    /// <summary>
    /// Gets the battery level as a percentage (0-100).
    /// </summary>
    public int Level => Value == 0xff ? 0 : Value;

    /// <summary>
    /// Gets a value indicating whether the battery level is critically low.
    /// </summary>
    public bool IsLow => Value == 0xff;

    public static implicit operator BatteryLevel(byte b) => new BatteryLevel(b);
}

/// <summary>
/// Indicates the charging status of a battery.
/// </summary>
public enum BatteryChargingStatus : byte
{
    Discharging = 0x00,

    Charging = 0x01,

    Maintaining = 0x02,
}

/// <summary>
/// Indicates whether the battery needs to be recharged or replaced.
/// </summary>
public enum BatteryRechargeOrReplaceStatus : byte
{
    /// <summary>
    /// The battery does not need to be recharged or replaced.
    /// </summary>
    Ok = 0x00,

    /// <summary>
    /// The battery must be recharged or replaced soon.
    /// </summary>
    Soon = 0x01,

    // Value 2 is undefined. From the spec: "If bit 1 is set to 1, bit 0 MUST also be set to 1."

    /// <summary>
    /// The battery must be recharged or replaced now.
    /// </summary>
    Now = 0x03,
}

/// <summary>
/// The temperature scale used for battery temperature readings.
/// </summary>
public enum BatteryTemperatureScale : byte
{
    Celsius = 0x00,
}

public enum BatteryCommand : byte
{
    /// <summary>
    /// Request the level of a battery.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the battery level of a battery operated device
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Query the health of the battery, particularly the battery temperature and maximum capacity.
    /// </summary>
    HealthGet = 0x04,

    /// <summary>
    /// Report the maximum capacity of the battery as well as the temperature of the battery.
    /// </summary>
    HealthReport = 0x05,
}

[CommandClass(CommandClassId.Battery)]
public sealed partial class BatteryCommandClass : CommandClass<BatteryCommand>
{
    internal BatteryCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(BatteryCommand command)
        => command switch
        {
            BatteryCommand.Get => true,
            BatteryCommand.HealthGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(BatteryCommand.HealthGet).GetValueOrDefault())
        {
            _ = await GetHealthAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((BatteryCommand)frame.CommandId)
        {
            case BatteryCommand.Report:
            {
                BatteryReport report = BatteryReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnBatteryReportReceived?.Invoke(report);
                break;
            }
            case BatteryCommand.HealthReport:
            {
                BatteryHealth report = BatteryHealthReportCommand.Parse(frame, Logger);
                LastHealthReport = report;
                OnBatteryHealthReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
