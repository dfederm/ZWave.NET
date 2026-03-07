using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Humidity Control Mode Report received from a device.
/// </summary>
public readonly record struct HumidityControlModeReport(
    /// <summary>
    /// The current humidity control mode.
    /// </summary>
    HumidityControlMode Mode);

public sealed partial class HumidityControlModeCommandClass
{
    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public HumidityControlModeReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Humidity Control Mode Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<HumidityControlModeReport>? OnModeReportReceived;

    /// <summary>
    /// Request the current humidity control mode from the device.
    /// </summary>
    public async Task<HumidityControlModeReport> GetAsync(CancellationToken cancellationToken)
    {
        HumidityControlModeGetCommand command = HumidityControlModeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<HumidityControlModeReportCommand>(cancellationToken).ConfigureAwait(false);
        HumidityControlModeReport report = HumidityControlModeReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnModeReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the humidity control mode in the device.
    /// </summary>
    public async Task SetAsync(HumidityControlMode mode, CancellationToken cancellationToken)
    {
        var command = HumidityControlModeSetCommand.Create(mode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct HumidityControlModeSetCommand : ICommand
    {
        public HumidityControlModeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.Set;

        public CommandClassFrame Frame { get; }

        public static HumidityControlModeSetCommand Create(HumidityControlMode mode)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)mode & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlModeSetCommand(frame);
        }
    }

    internal readonly struct HumidityControlModeGetCommand : ICommand
    {
        public HumidityControlModeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.Get;

        public CommandClassFrame Frame { get; }

        public static HumidityControlModeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new HumidityControlModeGetCommand(frame);
        }
    }

    internal readonly struct HumidityControlModeReportCommand : ICommand
    {
        public HumidityControlModeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.Report;

        public CommandClassFrame Frame { get; }

        public static HumidityControlModeReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Humidity Control Mode Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Humidity Control Mode Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            HumidityControlMode mode = (HumidityControlMode)(span[0] & 0x0F);
            return new HumidityControlModeReport(mode);
        }
    }
}
