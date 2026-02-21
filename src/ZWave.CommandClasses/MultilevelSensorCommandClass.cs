using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public enum MultilevelSensorCommand : byte
{
    /// <summary>
    /// Request the supported Sensor Types from a supporting node
    /// </summary>
    SupportedSensorGet = 0x01,

    /// <summary>
    /// Advertise the supported Sensor Types by a supporting node
    /// </summary>
    SupportedSensorReport = 0x02,

    /// <summary>
    /// retrieve the supported scales of the specific sensor type from the Multilevel Sensor device.
    /// </summary>
    SupportedScaleGet = 0x03,

    /// <summary>
    /// Request the current reading from a multilevel sensor
    /// </summary>
    Get = 0x04,

    /// <summary>
    /// Advertise its current sensor reading for its supported sensor type
    /// </summary>
    Report = 0x05,

    /// <summary>
    /// Advertise the supported scales of a specified multilevel sensor type
    /// </summary>
    SupportedScaleReport = 0x06,
}

/// <summary>
/// Represents a multilevel sensor reading.
/// </summary>
public readonly record struct MultilevelSensorState(
    /// <summary>
    /// The sensor type of the actual sensor reading.
    /// </summary>
    MultilevelSensorType SensorType,

    /// <summary>
    /// Indicates what scale is used for the actual sensor reading.
    /// </summary>
    MultilevelSensorScale Scale,

    /// <summary>
    /// Advertise the value of the actual sensor reading.
    /// </summary>
    double Value);

[CommandClass(CommandClassId.MultilevelSensor)]
public sealed class MultilevelSensorCommandClass : CommandClass<MultilevelSensorCommand>
{
    private Dictionary<MultilevelSensorType, IReadOnlySet<MultilevelSensorScale>?>? _supportedScales;

    private Dictionary<MultilevelSensorType, MultilevelSensorState?>? _sensorValues;

    public MultilevelSensorCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    public IReadOnlySet<MultilevelSensorType>? SupportedSensorTypes { get; private set; }

    public IReadOnlyDictionary<MultilevelSensorType, IReadOnlySet<MultilevelSensorScale>?>? SupportedScales => _supportedScales;

    public IReadOnlyDictionary<MultilevelSensorType, MultilevelSensorState?>? SensorValues => _sensorValues;

    /// <inheritdoc />
    public override bool? IsCommandSupported(MultilevelSensorCommand command)
        => command switch
        {
            MultilevelSensorCommand.SupportedSensorGet => Version.HasValue ? Version >= 5 : null,
            MultilevelSensorCommand.SupportedScaleGet => Version.HasValue ? Version >= 5 : null,
            MultilevelSensorCommand.Get => true,
            _ => false,
        };

    public async Task<MultilevelSensorState> GetAsync(
        MultilevelSensorType? sensorType,
        MultilevelSensorScale? scale,
        CancellationToken cancellationToken)
    {
        byte? scaleId = null;
        if (sensorType.HasValue)
        {
            if (SupportedSensorTypes == null)
            {
                throw new ZWaveException(ZWaveErrorCode.CommandNotReady, "The supported sensor types are not yet known.");
            }

            if (!SupportedSensorTypes.Contains(sensorType.Value))
            {
                throw new ZWaveException(ZWaveErrorCode.CommandInvalidArgument, $"Sensor type '{sensorType.Value}' is not supported for this node.");
            }

            if (SupportedScales == null)
            {
                throw new ZWaveException(ZWaveErrorCode.CommandNotReady, "The supported scales are not yet known.");
            }

            if (scale != null)
            {
                if (!SupportedScales[sensorType.Value]!.Contains(scale))
                {
                    throw new ZWaveException(ZWaveErrorCode.CommandInvalidArgument, $"Scale '{scale.Label}' is not supported for this sensor.");
                }

                scaleId = scale.Id;
            }
            else
            {
                // Pick an arbitrary supported scale if one isn't provided
                scaleId = 0;
            }
        }

        var command = MultilevelSensorGetCommand.Create(EffectiveVersion, sensorType, scaleId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSensorReportCommand>(
            predicate: frame =>
            {
                // Ensure the sensor type matches. If one wasn't provided, we don't know the default sensor type, so just
                // return the next report. We can't know for sure whether this is the reply to this command as we don't
                // know the device's default sensor type, but this overload is really just here for back-compat and the
                // caller should really always provide a sensor type.
                return !sensorType.HasValue
                    || (frame.CommandParameters.Length > 0 && (MultilevelSensorType)frame.CommandParameters.Span[0] == sensorType.Value);
            },
            cancellationToken).ConfigureAwait(false);
        MultilevelSensorState sensorState = MultilevelSensorReportCommand.Parse(reportFrame, Logger);
        _sensorValues![sensorState.SensorType] = sensorState;
        return sensorState;
    }

    public async Task<IReadOnlySet<MultilevelSensorType>> GetSupportedSensorsAsync(CancellationToken cancellationToken)
    {
        var command = MultilevelSensorSupportedSensorGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSensorSupportedSensorReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<MultilevelSensorType> supportedSensorTypes = MultilevelSensorSupportedSensorReportCommand.Parse(reportFrame, Logger);

        SupportedSensorTypes = supportedSensorTypes;

        var newSupportedScales = new Dictionary<MultilevelSensorType, IReadOnlySet<MultilevelSensorScale>?>();
        var newSensorValues = new Dictionary<MultilevelSensorType, MultilevelSensorState?>();
        foreach (MultilevelSensorType st in supportedSensorTypes)
        {
            // Persist any existing known values.
            if (SupportedScales == null
                || !SupportedScales.TryGetValue(st, out IReadOnlySet<MultilevelSensorScale>? scales))
            {
                scales = null;
            }

            if (SensorValues == null
                || !SensorValues.TryGetValue(st, out MultilevelSensorState? sensorValue))
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

    public async Task<IReadOnlySet<MultilevelSensorScale>> GetSupportedScalesAsync(
        MultilevelSensorType sensorType,
        CancellationToken cancellationToken)
    {
        if (SupportedSensorTypes == null)
        {
            bool? isCommandSupported = IsCommandSupported(MultilevelSensorCommand.SupportedScaleGet);
            if (isCommandSupported == null)
            {
                throw new ZWaveException(
                    ZWaveErrorCode.CommandNotReady,
                    "The supported sensor types are not yet known.");
            }
            else if (!isCommandSupported.Value)
            {
                throw new ZWaveException(ZWaveErrorCode.CommandNotSupported, "This command is not supported by this node");
            }
            else
            {
                throw new InvalidOperationException("Unexpected state. The interview is complete, the command is supported, but the supported sensor types are unknown.");
            }
        }

        if (!SupportedSensorTypes.Contains(sensorType))
        {
            throw new ZWaveException(ZWaveErrorCode.CommandInvalidArgument, $"Sensor type '{sensorType}' is not supported.");
        }

        var command = MultilevelSensorSupportedScaleGetCommand.Create(sensorType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSensorSupportedScaleReportCommand>(cancellationToken).ConfigureAwait(false);
        (MultilevelSensorType reportedType, IReadOnlySet<MultilevelSensorScale> supportedScales) = MultilevelSensorSupportedScaleReportCommand.Parse(reportFrame, Logger);
        _supportedScales![reportedType] = supportedScales;
        return supportedScales;
    }

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

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((MultilevelSensorCommand)frame.CommandId)
        {
            case MultilevelSensorCommand.SupportedSensorGet:
            case MultilevelSensorCommand.SupportedScaleGet:
            case MultilevelSensorCommand.Get:
            {
                break;
            }
            case MultilevelSensorCommand.SupportedSensorReport:
            {
                IReadOnlySet<MultilevelSensorType> supportedSensorTypes = MultilevelSensorSupportedSensorReportCommand.Parse(frame, Logger);
                SupportedSensorTypes = supportedSensorTypes;

                var newSupportedScales = new Dictionary<MultilevelSensorType, IReadOnlySet<MultilevelSensorScale>?>();
                var newSensorValues = new Dictionary<MultilevelSensorType, MultilevelSensorState?>();
                foreach (MultilevelSensorType sensorType in supportedSensorTypes)
                {
                    // Persist any existing known values.
                    if (SupportedScales == null
                        || !SupportedScales.TryGetValue(sensorType, out IReadOnlySet<MultilevelSensorScale>? scales))
                    {
                        scales = null;
                    }

                    if (SensorValues == null
                        || !SensorValues.TryGetValue(sensorType, out MultilevelSensorState? sensorValue))
                    {
                        sensorValue = null;
                    }

                    newSupportedScales.Add(sensorType, scales);
                    newSensorValues.Add(sensorType, sensorValue);
                }

                _supportedScales = newSupportedScales;
                _sensorValues = newSensorValues;
                break;
            }
            case MultilevelSensorCommand.SupportedScaleReport:
            {
                (MultilevelSensorType reportedType, IReadOnlySet<MultilevelSensorScale> supportedScales) = MultilevelSensorSupportedScaleReportCommand.Parse(frame, Logger);
                _supportedScales![reportedType] = supportedScales;
                break;
            }
            case MultilevelSensorCommand.Report:
            {
                MultilevelSensorState sensorState = MultilevelSensorReportCommand.Parse(frame, Logger);
                _sensorValues![sensorState.SensorType] = sensorState;
                break;
            }
        }
    }

    private readonly struct MultilevelSensorGetCommand : ICommand
    {
        public MultilevelSensorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MultilevelSensorSupportedSensorGetCommand Create(
            byte version,
            MultilevelSensorType? sensorType,
            byte? scaleId)
        {
            CommandClassFrame frame;
            if (version >= 5
                && sensorType.HasValue
                && scaleId.HasValue)
            {
                ReadOnlySpan<byte> commandParameters = [(byte)sensorType.Value, (byte)((scaleId & 0b0000_0011) << 3)];
                frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            }
            else
            {
                frame = CommandClassFrame.Create(CommandClassId, CommandId);
            }

            return new MultilevelSensorSupportedSensorGetCommand(frame);
        }
    }

    private readonly struct MultilevelSensorReportCommand : ICommand
    {
        public MultilevelSensorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.Report;

        public CommandClassFrame Frame { get; }

        public static MultilevelSensorState Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Multilevel Sensor Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multilevel Sensor Report frame is too short");
            }

            MultilevelSensorType sensorType = (MultilevelSensorType)frame.CommandParameters.Span[0];
            byte scaleId = (byte)((frame.CommandParameters.Span[1] & 0b0001_1000) >> 3);
            MultilevelSensorScale scale = sensorType.GetScale(scaleId);

            int precision = (frame.CommandParameters.Span[1] & 0b1110_0000) >> 5;
            int valueSize = frame.CommandParameters.Span[1] & 0b0000_0111;

            if (frame.CommandParameters.Length < 2 + valueSize)
            {
                logger.LogWarning(
                    "Multilevel Sensor Report frame value size ({ValueSize}) exceeds remaining bytes ({Remaining})",
                    valueSize,
                    frame.CommandParameters.Length - 2);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multilevel Sensor Report frame is too short for declared value size");
            }

            ReadOnlySpan<byte> valueBytes = frame.CommandParameters.Span.Slice(2, valueSize);

            if (valueBytes.Length > sizeof(int))
            {
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, $"The value's size was more than {sizeof(int)} bytes, and currently we can't handle that");
            }

            int rawValue = valueBytes.ToInt32BE();
            double value = rawValue / Math.Pow(10, precision);

            return new MultilevelSensorState(sensorType, scale, value);
        }
    }

    private readonly struct MultilevelSensorSupportedSensorGetCommand : ICommand
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

    private readonly struct MultilevelSensorSupportedSensorReportCommand : ICommand
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

            var supportedSensorTypes = new HashSet<MultilevelSensorType>();

            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span;
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        // As per the spec, bit 0 corresponds to Sensor Type 0x01, so we need to add 1.
                        MultilevelSensorType sensorType = (MultilevelSensorType)((byteNum << 3) + bitNum + 1);
                        supportedSensorTypes.Add(sensorType);
                    }
                }
            }

            return supportedSensorTypes;
        }
    }

    private readonly struct MultilevelSensorSupportedScaleGetCommand : ICommand
    {
        public MultilevelSensorSupportedScaleGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.SupportedScaleGet;

        public CommandClassFrame Frame { get; }

        public static MultilevelSensorSupportedScaleGetCommand Create(MultilevelSensorType sensorType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)sensorType];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultilevelSensorSupportedScaleGetCommand(frame);
        }
    }

    private readonly struct MultilevelSensorSupportedScaleReportCommand : ICommand
    {
        public MultilevelSensorSupportedScaleReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSensor;

        public static byte CommandId => (byte)MultilevelSensorCommand.SupportedScaleReport;

        public CommandClassFrame Frame { get; }

        public static (MultilevelSensorType SensorType, IReadOnlySet<MultilevelSensorScale> SupportedScales) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Multilevel Sensor Supported Scale Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multilevel Sensor Supported Scale Report frame is too short");
            }

            MultilevelSensorType sensorType = (MultilevelSensorType)frame.CommandParameters.Span[0];

            var supportedScales = new HashSet<MultilevelSensorScale>();
            byte bitMask = (byte)(frame.CommandParameters.Span[1] & 0b0000_1111);
            for (int bitNum = 0; bitNum < 4; bitNum++)
            {
                if ((bitMask & (1 << bitNum)) != 0)
                {
                    byte scaleId = (byte)bitNum;
                    MultilevelSensorScale scale = sensorType.GetScale(scaleId);
                    supportedScales.Add(scale);
                }
            }

            return (sensorType, supportedScales);
        }
    }
}
