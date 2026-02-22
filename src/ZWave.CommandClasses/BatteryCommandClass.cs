using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a battery level value.
/// </summary>
public struct BatteryLevel
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
public enum BatterRechargeOrReplaceStatus : byte
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
    Celcius = 0x00,
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

/// <summary>
/// Represents a Battery Report received from a device.
/// </summary>
public readonly record struct BatteryReport(
    /// <summary>
    /// The percentage indicating the battery level
    /// </summary>
    BatteryLevel BatteryLevel,

    /// <summary>
    /// The charging status of a battery.
    /// </summary>
    BatteryChargingStatus? ChargingStatus,

    /// <summary>
    /// Indicates if the battery is rechargeable or not
    /// </summary>
    bool? IsRechargeable,

    /// <summary>
    /// Illustrate if the battery is utilized for back-up purposes of a mains powered connected device.
    /// </summary>
    bool? IsBackupBattery,

    /// <summary>
    /// Indicate if overheating is detected at the battery.
    /// </summary>
    bool? IsOverheating,

    /// <summary>
    /// Indicate if the battery fluid is low and should be refilled
    /// </summary>
    bool? HasLowFluid,

    /// <summary>
    /// Indicate if the battery needs to be recharged or replaced.
    /// </summary>
    BatterRechargeOrReplaceStatus? ReplaceRechargeStatus,

    /// <summary>
    /// Advertise if the battery of a device has stopped charging due to low temperature
    /// </summary>
    bool? IsLowTemperature,

    /// <summary>
    /// Indicate if the battery is currently disconnected or removed from the node.
    /// </summary>
    bool? Disconnected);

public readonly record struct BatteryHealth(
    /// <summary>
    /// Report the percentage indicating the maximum capacity of the battery
    /// </summary>
    byte? MaximumCapacity,

    /// <summary>
    /// The scale used for the battery temperature value
    /// </summary>
    BatteryTemperatureScale BatteryTemperatureScale,

    /// <summary>
    /// The temperature of the battery
    /// </summary>
    double? BatteryTemperature);

[CommandClass(CommandClassId.Battery)]
public sealed class BatteryCommandClass : CommandClass<BatteryCommand>
{
    public BatteryCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public BatteryReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the last reported battery health.
    /// </summary>
    public BatteryHealth? LastHealthReport { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(BatteryCommand command)
        => command switch
        {
            BatteryCommand.Get => true,
            BatteryCommand.HealthGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    public async Task<BatteryReport> GetAsync(CancellationToken cancellationToken)
    {
        BatteryGetCommand command = BatteryGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BatteryReportCommand>(cancellationToken).ConfigureAwait(false);
        BatteryReport report = BatteryReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        return report;
    }

    public async Task<BatteryHealth> GetHealthAsync(CancellationToken cancellationToken)
    {
        BatteryHealthGetCommand command = BatteryHealthGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BatteryHealthReportCommand>(cancellationToken).ConfigureAwait(false);
        BatteryHealth report = BatteryHealthReportCommand.Parse(reportFrame, Logger);
        LastHealthReport = report;
        return report;
    }

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
            case BatteryCommand.Get:
            case BatteryCommand.HealthGet:
            {
                break;
            }
            case BatteryCommand.Report:
            {
                LastReport = BatteryReportCommand.Parse(frame, Logger);
                break;
            }
            case BatteryCommand.HealthReport:
            {
                LastHealthReport = BatteryHealthReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct BatteryGetCommand : ICommand
    {
        public BatteryGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Battery;

        public static byte CommandId => (byte)BatteryCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BatteryGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BatteryGetCommand(frame);
        }
    }

    private readonly struct BatteryReportCommand : ICommand
    {
        public BatteryReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Battery;

        public static byte CommandId => (byte)BatteryCommand.Report;

        public CommandClassFrame Frame { get; }

        public static BatteryReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Battery Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Battery Report frame is too short");
            }

            BatteryLevel batteryLevel = frame.CommandParameters.Span[0];

            BatteryChargingStatus? chargingStatus = frame.CommandParameters.Length > 1
                ? (BatteryChargingStatus)((frame.CommandParameters.Span[1] & 0b1100_0000) >> 6)
                : null;
            bool? isRechargeable = frame.CommandParameters.Length > 1
                ? (frame.CommandParameters.Span[1] & 0b0010_0000) != 0
                : null;
            bool? isBackupBattery = frame.CommandParameters.Length > 1
                ? (frame.CommandParameters.Span[1] & 0b0001_0000) != 0
                : null;
            bool? isOverheating = frame.CommandParameters.Length > 1
                ? (frame.CommandParameters.Span[1] & 0b0000_1000) != 0
                : null;
            bool? hasLowFluid = frame.CommandParameters.Length > 1
                ? (frame.CommandParameters.Span[1] & 0b0000_0100) != 0
                : null;
            // This is spec'd as a bitmask but it's basically just an enum
            BatterRechargeOrReplaceStatus? replaceRechargeStatus = frame.CommandParameters.Length > 1
                ? (BatterRechargeOrReplaceStatus)(frame.CommandParameters.Span[1] & 0b0000_0011)
                : null;

            bool? isLowTemperature = frame.CommandParameters.Length > 2
                ? (frame.CommandParameters.Span[2] & 0b0000_0010) != 0
                : null;
            bool? disconnected = frame.CommandParameters.Length > 2
                ? (frame.CommandParameters.Span[2] & 0b0000_0001) != 0
                : null;

            return new BatteryReport(
                batteryLevel,
                chargingStatus,
                isRechargeable,
                isBackupBattery,
                isOverheating,
                hasLowFluid,
                replaceRechargeStatus,
                isLowTemperature,
                disconnected);
        }
    }

    private readonly struct BatteryHealthGetCommand : ICommand
    {
        public BatteryHealthGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Battery;

        public static byte CommandId => (byte)BatteryCommand.HealthGet;

        public CommandClassFrame Frame { get; }

        public static BatteryHealthGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BatteryHealthGetCommand(frame);
        }
    }

    private readonly struct BatteryHealthReportCommand : ICommand
    {
        public BatteryHealthReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Battery;

        public static byte CommandId => (byte)BatteryCommand.HealthReport;

        public CommandClassFrame Frame { get; }

        public static BatteryHealth Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Battery Health Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Battery Health Report frame is too short");
            }

            // 0xff means unknown.
            byte rawCapacity = frame.CommandParameters.Span[0];
            byte? maximumCapacity = rawCapacity == 0xff ? null : rawCapacity;

            BatteryTemperatureScale batteryTemperatureScale
                = (BatteryTemperatureScale)((frame.CommandParameters.Span[1] & 0b0001_1000) >> 3);

            int precision = (frame.CommandParameters.Span[1] & 0b1110_0000) >> 5;
            int valueSize = frame.CommandParameters.Span[1] & 0b0000_0111;
            double? batteryTemperature;
            if (valueSize == 0)
            {
                // The battery temperature is unknown
                batteryTemperature = null;
            }
            else
            {
                if (frame.CommandParameters.Length < 2 + valueSize)
                {
                    logger.LogWarning(
                        "Battery Health Report frame value size ({ValueSize}) exceeds remaining bytes ({Remaining})",
                        valueSize,
                        frame.CommandParameters.Length - 2);
                    throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Battery Health Report frame is too short for declared value size");
                }

                ReadOnlySpan<byte> valueBytes = frame.CommandParameters.Span.Slice(2, valueSize);

                if (valueBytes.Length > sizeof(int))
                {
                    throw new ZWaveException(ZWaveErrorCode.InvalidPayload, $"The value's size was more than {sizeof(int)} bytes, and currently we can't handle that");
                }

                int rawValue = valueBytes.ToInt32BE();
                batteryTemperature = rawValue / Math.Pow(10, precision);
            }

            return new BatteryHealth(maximumCapacity, batteryTemperatureScale, batteryTemperature);
        }
    }
}
