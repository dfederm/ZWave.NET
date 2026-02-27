using System.Buffers.Binary;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Battery Health Report received from a device.
/// </summary>
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

public sealed partial class BatteryCommandClass
{
    /// <summary>
    /// Raised when a Battery Health Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<BatteryHealth>? OnBatteryHealthReportReceived;

    /// <summary>
    /// Gets the last reported battery health.
    /// </summary>
    public BatteryHealth? LastHealthReport { get; private set; }

    /// <summary>
    /// Requests the current battery health (maximum capacity and temperature).
    /// </summary>
    public async Task<BatteryHealth> GetHealthAsync(CancellationToken cancellationToken)
    {
        BatteryHealthGetCommand command = BatteryHealthGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BatteryHealthReportCommand>(cancellationToken).ConfigureAwait(false);
        BatteryHealth report = BatteryHealthReportCommand.Parse(reportFrame, Logger);
        LastHealthReport = report;
        OnBatteryHealthReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct BatteryHealthGetCommand : ICommand
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

    internal readonly struct BatteryHealthReportCommand : ICommand
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
                // CC:0080.02.05.11.004: Size MUST be 0, 1, 2, or 4
                if (valueSize != 1 && valueSize != 2 && valueSize != 4)
                {
                    logger.LogWarning("Battery Health Report has invalid Size value ({Size}). Expected 0, 1, 2, or 4", valueSize);
                    throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Battery Health Report has invalid Size value");
                }

                if (frame.CommandParameters.Length < 2 + valueSize)
                {
                    logger.LogWarning(
                        "Battery Health Report frame value size ({ValueSize}) exceeds remaining bytes ({Remaining})",
                        valueSize,
                        frame.CommandParameters.Length - 2);
                    throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Battery Health Report frame is too short for declared value size");
                }

                ReadOnlySpan<byte> valueBytes = frame.CommandParameters.Span.Slice(2, valueSize);

                // CC:0080.02.05.11.010: signed big-endian encoding
                int rawValue = valueSize switch
                {
                    1 => (sbyte)valueBytes[0],
                    2 => BinaryPrimitives.ReadInt16BigEndian(valueBytes),
                    4 => BinaryPrimitives.ReadInt32BigEndian(valueBytes),
                    _ => throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Unexpected value size"),
                };
                batteryTemperature = rawValue / Math.Pow(10, precision);
            }

            return new BatteryHealth(maximumCapacity, batteryTemperatureScale, batteryTemperature);
        }
    }
}
