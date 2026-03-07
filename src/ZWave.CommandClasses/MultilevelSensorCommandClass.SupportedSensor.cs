using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class MultilevelSensorCommandClass
{
    /// <summary>
    /// Gets the supported sensor types, or <see langword="null"/> if not yet known.
    /// </summary>
    public IReadOnlySet<MultilevelSensorType>? SupportedSensorTypes { get; private set; }

    /// <summary>
    /// Request the supported sensor types from a supporting node.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The set of supported sensor types.</returns>
    public async Task<IReadOnlySet<MultilevelSensorType>> GetSupportedSensorsAsync(CancellationToken cancellationToken)
    {
        var command = MultilevelSensorSupportedSensorGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSensorSupportedSensorReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<MultilevelSensorType> supportedSensorTypes = MultilevelSensorSupportedSensorReportCommand.Parse(reportFrame, Logger);

        SupportedSensorTypes = supportedSensorTypes;

        var newSupportedScales = new Dictionary<MultilevelSensorType, IReadOnlySet<MultilevelSensorScale>?>();
        var newSensorValues = new Dictionary<MultilevelSensorType, MultilevelSensorReport?>();
        foreach (MultilevelSensorType st in supportedSensorTypes)
        {
            // Persist any existing known values.
            if (SupportedScales == null
                || !SupportedScales.TryGetValue(st, out IReadOnlySet<MultilevelSensorScale>? scales))
            {
                scales = null;
            }

            if (!SensorValues.TryGetValue(st, out MultilevelSensorReport? sensorValue))
            {
                sensorValue = null;
            }

            newSupportedScales.Add(st, scales);
            newSensorValues.Add(st, sensorValue);
        }

        _supportedScales = newSupportedScales;
        _sensorValues = newSensorValues;

        return supportedSensorTypes;
    }

    internal readonly struct MultilevelSensorSupportedSensorGetCommand : ICommand
    {
        public MultilevelSensorSupportedSensorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.SupportedSensorGet;

        public CommandClassFrame Frame { get; }

        public static MultilevelSensorSupportedSensorGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultilevelSensorSupportedSensorGetCommand(frame);
        }
    }

    internal readonly struct MultilevelSensorSupportedSensorReportCommand : ICommand
    {
        public MultilevelSensorSupportedSensorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.SupportedSensorReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<MultilevelSensorType> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Multilevel Sensor Supported Sensor Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multilevel Sensor Supported Sensor Report frame is too short");
            }

            // As per the spec, bit 0 corresponds to Sensor Type 0x01, so offset by 1.
            return BitMaskHelper.ParseBitMask<MultilevelSensorType>(frame.CommandParameters.Span, offset: 1);
        }
    }
}
