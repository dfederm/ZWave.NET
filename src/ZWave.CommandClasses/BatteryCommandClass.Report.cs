using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

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
    BatteryRechargeOrReplaceStatus? ReplaceRechargeStatus,

    /// <summary>
    /// Advertise if the battery of a device has stopped charging due to low temperature
    /// </summary>
    bool? IsLowTemperature,

    /// <summary>
    /// Indicate if the battery is currently disconnected or removed from the node.
    /// </summary>
    bool? IsDisconnected);

public sealed partial class BatteryCommandClass
{
    /// <summary>
    /// Raised when a Battery Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<BatteryReport>? OnBatteryReportReceived;

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public BatteryReport? LastReport { get; private set; }

    /// <summary>
    /// Requests the current battery level and status.
    /// </summary>
    public async Task<BatteryReport> GetAsync(CancellationToken cancellationToken)
    {
        BatteryGetCommand command = BatteryGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BatteryReportCommand>(cancellationToken).ConfigureAwait(false);
        BatteryReport report = BatteryReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnBatteryReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct BatteryGetCommand : ICommand
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

    internal readonly struct BatteryReportCommand : ICommand
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
            BatteryRechargeOrReplaceStatus? replaceRechargeStatus = frame.CommandParameters.Length > 1
                ? (BatteryRechargeOrReplaceStatus)(frame.CommandParameters.Span[1] & 0b0000_0011)
                : null;

            bool? isLowTemperature = frame.CommandParameters.Length > 2
                ? (frame.CommandParameters.Span[2] & 0b0000_0010) != 0
                : null;
            bool? isDisconnected = frame.CommandParameters.Length > 2
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
                isDisconnected);
        }
    }
}
