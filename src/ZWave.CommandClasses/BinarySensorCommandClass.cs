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
public sealed class BinarySensorCommandClass : CommandClass<BinarySensorCommand>
{
    private Dictionary<BinarySensorType, bool?>? _sensorValues;

    public BinarySensorCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// The supported sensor types by the binary sensor device.
    /// </summary>
    public IReadOnlySet<BinarySensorType>? SupportedSensorTypes { get; private set; }

    /// <summary>
    /// The values of each supported sensor type.
    /// </summary>
    public IReadOnlyDictionary<BinarySensorType, bool?>? SensorValues => _sensorValues;

    /// <inheritdoc />
    public override bool? IsCommandSupported(BinarySensorCommand command)
        => command switch
        {
            BinarySensorCommand.Get => true,
            BinarySensorCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    public async Task<bool> GetAsync(
        BinarySensorType? sensorType,
        CancellationToken cancellationToken)
    {
        BinarySensorGetCommand command = BinarySensorGetCommand.Create(EffectiveVersion, sensorType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<BinarySensorReportCommand>(
            predicate: frame =>
            {
                // Ensure the sensor type matches. If one wasn't provided, we don't know the default sensor type, so just
                // return the next report. We can't know for sure whether this is the reply to this command as we don't
                // know the device's default sensor type, but this overload is really just here for back-compat and the
                // caller should really always provide a sensor type.
                if (!sensorType.HasValue || sensorType.Value == BinarySensorType.FirstSupported)
                {
                    return true;
                }

                return frame.CommandParameters.Length > 1
                    && (BinarySensorType)frame.CommandParameters.Span[1] == sensorType.Value;
            },
            cancellationToken).ConfigureAwait(false);
        (BinarySensorType? reportSensorType, bool sensorValue) = BinarySensorReportCommand.Parse(reportFrame, Logger);
        BinarySensorType key = reportSensorType.GetValueOrDefault(BinarySensorType.FirstSupported);
        _sensorValues![key] = sensorValue;
        return sensorValue;
    }

    public async Task<IReadOnlySet<BinarySensorType>> GetSupportedSensorTypesAsync(CancellationToken cancellationToken)
    {
        BinarySensorSupportedGetCommand command = BinarySensorSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BinarySensorSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<BinarySensorType> supportedTypes = BinarySensorSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedSensorTypes = supportedTypes;

        Dictionary<BinarySensorType, bool?> newSensorValues = new Dictionary<BinarySensorType, bool?>();
        foreach (BinarySensorType type in supportedTypes)
        {
            // Persist any existing known state.
            if (SensorValues == null
                || !SensorValues.TryGetValue(type, out bool? existing))
            {
                existing = null;
            }

            newSensorValues.Add(type, existing);
        }

        _sensorValues = newSensorValues;

        return supportedTypes;
    }

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
            case BinarySensorCommand.Get:
            case BinarySensorCommand.SupportedGet:
            {
                break;
            }
            case BinarySensorCommand.Report:
            {
                (BinarySensorType? parsedSensorType, bool sensorValue) = BinarySensorReportCommand.Parse(frame, Logger);
                BinarySensorType key = parsedSensorType.GetValueOrDefault(BinarySensorType.FirstSupported);
                _sensorValues![key] = sensorValue;
                break;
            }
            case BinarySensorCommand.SupportedReport:
            {
                IReadOnlySet<BinarySensorType> supportedTypes = BinarySensorSupportedReportCommand.Parse(frame, Logger);
                SupportedSensorTypes = supportedTypes;

                Dictionary<BinarySensorType, bool?> newSensorValues = new Dictionary<BinarySensorType, bool?>();
                foreach (BinarySensorType sensorType in supportedTypes)
                {
                    // Persist any existing known state.
                    if (SensorValues == null
                        || !SensorValues.TryGetValue(sensorType, out bool? sensorValue))
                    {
                        sensorValue = null;
                    }

                    newSensorValues.Add(sensorType, sensorValue);
                }

                _sensorValues = newSensorValues;

                break;
            }
        }
    }

    private readonly struct BinarySensorGetCommand : ICommand
    {
        public BinarySensorGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BinarySensorGetCommand Create(byte version, BinarySensorType? sensorType)
        {
            if (version >= 2 && sensorType.HasValue)
            {
                ReadOnlySpan<byte> commandParameters = [(byte)sensorType.Value];
                CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
                return new BinarySensorGetCommand(frame);
            }
            else
            {
                CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
                return new BinarySensorGetCommand(frame);
            }
        }
    }

    private readonly struct BinarySensorReportCommand : ICommand
    {
        public BinarySensorReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.Report;

        public CommandClassFrame Frame { get; }

        public static (BinarySensorType? SensorType, bool SensorValue) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Binary Sensor Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Binary Sensor Report frame is too short");
            }

            bool sensorValue = frame.CommandParameters.Span[0] == 0xff;
            BinarySensorType? sensorType = frame.CommandParameters.Length > 1
                ? (BinarySensorType)frame.CommandParameters.Span[1]
                : null;
            return (sensorType, sensorValue);
        }
    }

    private readonly struct BinarySensorSupportedGetCommand : ICommand
    {
        public BinarySensorSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static BinarySensorSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BinarySensorSupportedGetCommand(frame);
        }
    }

    private readonly struct BinarySensorSupportedReportCommand : ICommand
    {
        public BinarySensorSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySensor;

        public static byte CommandId => (byte)BinarySensorCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<BinarySensorType> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Binary Sensor Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Binary Sensor Supported Report frame is too short");
            }

            HashSet<BinarySensorType> supportedSensorTypes = new HashSet<BinarySensorType>();

            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span[1..];
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        BinarySensorType sensorType = (BinarySensorType)((byteNum << 3) + bitNum);
                        supportedSensorTypes.Add(sensorType);
                    }
                }
            }

            return supportedSensorTypes;
        }
    }
}
