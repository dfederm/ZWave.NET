using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class HumidityControlSetpointCommandClass
{
    /// <summary>
    /// Gets the humidity control setpoint types supported by the device, or <see langword="null"/> if not yet known.
    /// </summary>
    public IReadOnlySet<HumidityControlSetpointType>? SupportedSetpointTypes { get; private set; }

    /// <summary>
    /// Request the humidity control setpoint types supported by the device.
    /// </summary>
    public async Task<IReadOnlySet<HumidityControlSetpointType>> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<HumidityControlSetpointSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<HumidityControlSetpointType> supportedTypes = HumidityControlSetpointSupportedReportCommand.Parse(reportFrame, Logger);

        SupportedSetpointTypes = supportedTypes;

        // Rebuild the setpoint values dictionary to include keys for every supported type
        Dictionary<HumidityControlSetpointType, HumidityControlSetpointReport?> newSetpointValues = new();
        foreach (HumidityControlSetpointType st in supportedTypes)
        {
            if (!_setpointValues.TryGetValue(st, out HumidityControlSetpointReport? existingValue))
            {
                existingValue = null;
            }

            newSetpointValues.Add(st, existingValue);
        }

        _setpointValues = newSetpointValues;

        return supportedTypes;
    }

    internal readonly struct HumidityControlSetpointSupportedGetCommand : ICommand
    {
        public HumidityControlSetpointSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new HumidityControlSetpointSupportedGetCommand(frame);
        }
    }

    internal readonly struct HumidityControlSetpointSupportedReportCommand : ICommand
    {
        public HumidityControlSetpointSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<HumidityControlSetpointType> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Humidity Control Setpoint Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Humidity Control Setpoint Supported Report frame is too short");
            }

            return BitMaskHelper.ParseBitMask<HumidityControlSetpointType>(frame.CommandParameters.Span, startBit: 1);
        }
    }
}
