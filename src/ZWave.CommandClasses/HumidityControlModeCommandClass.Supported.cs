using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class HumidityControlModeCommandClass
{
    /// <summary>
    /// Gets the humidity control modes supported by the device, or <see langword="null"/> if not yet known.
    /// </summary>
    public IReadOnlySet<HumidityControlMode>? SupportedModes { get; private set; }

    /// <summary>
    /// Request the supported humidity control modes from the device.
    /// </summary>
    public async Task<IReadOnlySet<HumidityControlMode>> GetSupportedModesAsync(CancellationToken cancellationToken)
    {
        HumidityControlModeSupportedGetCommand command = HumidityControlModeSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<HumidityControlModeSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<HumidityControlMode> supportedModes = HumidityControlModeSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedModes = supportedModes;
        return supportedModes;
    }

    internal readonly struct HumidityControlModeSupportedGetCommand : ICommand
    {
        public HumidityControlModeSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static HumidityControlModeSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new HumidityControlModeSupportedGetCommand(frame);
        }
    }

    internal readonly struct HumidityControlModeSupportedReportCommand : ICommand
    {
        public HumidityControlModeSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlMode;

        public static byte CommandId => (byte)HumidityControlModeCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<HumidityControlMode> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Humidity Control Mode Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Humidity Control Mode Supported Report frame is too short");
            }

            return BitMaskHelper.ParseBitMask<HumidityControlMode>(frame.CommandParameters.Span, startBit: 1);
        }
    }
}
