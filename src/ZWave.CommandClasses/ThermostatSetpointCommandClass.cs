namespace ZWave.CommandClasses;

public enum ThermostatSetpointCommand : byte
{
    /// <summary>
    /// Set the setpoint value for a given setpoint type.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the setpoint value for a given setpoint type.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the setpoint value for a given setpoint type.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported setpoint types from a supporting node.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Advertise the supported setpoint types by a supporting node.
    /// </summary>
    SupportedReport = 0x05,

    /// <summary>
    /// Request the supported scales for a given setpoint type.
    /// </summary>
    ScaleSupportedGet = 0x06,

    /// <summary>
    /// Request the capabilities for a given setpoint type.
    /// </summary>
    CapabilitiesGet = 0x09,

    /// <summary>
    /// Advertise the capabilities for a given setpoint type.
    /// </summary>
    CapabilitiesReport = 0x0A,
}

/// <summary>
/// Represents a thermostat setpoint type.
/// </summary>
public enum ThermostatSetpointType : byte
{
    NotSupported = 0x00,
    Heating = 0x01,
    Cooling = 0x02,
    Furnace = 0x07,
    DryAir = 0x08,
    MoistAir = 0x09,
    AutoChangeover = 0x0A,
    EnergySaveHeating = 0x0B,
    EnergySaveCooling = 0x0C,
    AwayHeating = 0x0D,
    AwayCooling = 0x0E,
    FullPower = 0x0F,
}

/// <summary>
/// Represents the scale used for a thermostat setpoint value.
/// </summary>
public enum ThermostatSetpointScale : byte
{
    Celsius = 0x00,
    Fahrenheit = 0x01,
}

/// <summary>
/// Represents the capabilities of a thermostat setpoint type.
/// </summary>
public readonly struct ThermostatSetpointCapabilities
{
    public ThermostatSetpointCapabilities(
        ThermostatSetpointType setpointType,
        ThermostatSetpointScale minScale,
        double minValue,
        ThermostatSetpointScale maxScale,
        double maxValue)
    {
        SetpointType = setpointType;
        MinScale = minScale;
        MinValue = minValue;
        MaxScale = maxScale;
        MaxValue = maxValue;
    }

    /// <summary>
    /// The setpoint type.
    /// </summary>
    public ThermostatSetpointType SetpointType { get; }

    /// <summary>
    /// The scale used for the minimum value.
    /// </summary>
    public ThermostatSetpointScale MinScale { get; }

    /// <summary>
    /// The minimum setpoint value.
    /// </summary>
    public double MinValue { get; }

    /// <summary>
    /// The scale used for the maximum value.
    /// </summary>
    public ThermostatSetpointScale MaxScale { get; }

    /// <summary>
    /// The maximum setpoint value.
    /// </summary>
    public double MaxValue { get; }
}

/// <summary>
/// Represents a thermostat setpoint value.
/// </summary>
public readonly struct ThermostatSetpointValue
{
    public ThermostatSetpointValue(ThermostatSetpointType setpointType, ThermostatSetpointScale scale, double value)
    {
        SetpointType = setpointType;
        Scale = scale;
        Value = value;
    }

    /// <summary>
    /// The setpoint type.
    /// </summary>
    public ThermostatSetpointType SetpointType { get; }

    /// <summary>
    /// The scale used for the setpoint value.
    /// </summary>
    public ThermostatSetpointScale Scale { get; }

    /// <summary>
    /// The setpoint value.
    /// </summary>
    public double Value { get; }
}

[CommandClass(CommandClassId.ThermostatSetpoint)]
public sealed class ThermostatSetpointCommandClass : CommandClass<ThermostatSetpointCommand>
{
    private Dictionary<ThermostatSetpointType, ThermostatSetpointValue?>? _setpointValues;
    private ThermostatSetpointCapabilities? _lastCapabilities;

    public ThermostatSetpointCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the supported setpoint types, or null if not yet determined.
    /// </summary>
    public IReadOnlySet<ThermostatSetpointType>? SupportedSetpointTypes { get; private set; }

    /// <summary>
    /// Gets the last reported setpoint values keyed by setpoint type.
    /// </summary>
    public IReadOnlyDictionary<ThermostatSetpointType, ThermostatSetpointValue?>? SetpointValues => _setpointValues;

    /// <inheritdoc />
    public override bool? IsCommandSupported(ThermostatSetpointCommand command)
        => command switch
        {
            ThermostatSetpointCommand.Set => true,
            ThermostatSetpointCommand.Get => true,
            ThermostatSetpointCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            ThermostatSetpointCommand.CapabilitiesGet => Version.HasValue ? Version >= 3 : null,
            _ => false,
        };

    /// <summary>
    /// Request the setpoint value for a given setpoint type.
    /// </summary>
    public async Task<ThermostatSetpointValue> GetAsync(
        ThermostatSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = ThermostatSetpointGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        var reportFrame = await AwaitNextReportAsync<ThermostatSetpointReportCommand>(
            predicate: frame =>
            {
                var report = new ThermostatSetpointReportCommand(frame);
                return report.SetpointType == setpointType;
            },
            cancellationToken).ConfigureAwait(false);
        var reportCommand = new ThermostatSetpointReportCommand(reportFrame);
        return SetpointValues![reportCommand.SetpointType]!.Value;
    }

    /// <summary>
    /// Set the setpoint value for a given setpoint type.
    /// </summary>
    public async Task SetAsync(
        ThermostatSetpointType setpointType,
        ThermostatSetpointScale scale,
        double value,
        CancellationToken cancellationToken)
    {
        var command = ThermostatSetpointSetCommand.Create(setpointType, scale, value);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported setpoint types.
    /// </summary>
    public async Task<IReadOnlySet<ThermostatSetpointType>> GetSupportedSetpointTypesAsync(CancellationToken cancellationToken)
    {
        var command = ThermostatSetpointSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatSetpointSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedSetpointTypes!;
    }

    /// <summary>
    /// Request the capabilities for a given setpoint type.
    /// </summary>
    public async Task<ThermostatSetpointCapabilities> GetCapabilitiesAsync(
        ThermostatSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = ThermostatSetpointCapabilitiesGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ThermostatSetpointCapabilitiesReportCommand>(
            predicate: frame =>
            {
                var report = new ThermostatSetpointCapabilitiesReportCommand(frame);
                return report.SetpointType == setpointType;
            },
            cancellationToken).ConfigureAwait(false);
        return _lastCapabilities!.Value;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(ThermostatSetpointCommand.SupportedGet).GetValueOrDefault())
        {
            IReadOnlySet<ThermostatSetpointType> supportedTypes = await GetSupportedSetpointTypesAsync(cancellationToken).ConfigureAwait(false);
            foreach (ThermostatSetpointType setpointType in supportedTypes)
            {
                _ = await GetAsync(setpointType, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ThermostatSetpointCommand)frame.CommandId)
        {
            case ThermostatSetpointCommand.Set:
            case ThermostatSetpointCommand.Get:
            case ThermostatSetpointCommand.SupportedGet:
            case ThermostatSetpointCommand.ScaleSupportedGet:
            case ThermostatSetpointCommand.CapabilitiesGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ThermostatSetpointCommand.SupportedReport:
            {
                var command = new ThermostatSetpointSupportedReportCommand(frame);
                SupportedSetpointTypes = command.SupportedSetpointTypes;

                var newSetpointValues = new Dictionary<ThermostatSetpointType, ThermostatSetpointValue?>();
                foreach (ThermostatSetpointType setpointType in SupportedSetpointTypes)
                {
                    // Persist any existing known values.
                    if (_setpointValues == null
                        || !_setpointValues.TryGetValue(setpointType, out ThermostatSetpointValue? existingValue))
                    {
                        existingValue = null;
                    }

                    newSetpointValues.Add(setpointType, existingValue);
                }

                _setpointValues = newSetpointValues;
                break;
            }
            case ThermostatSetpointCommand.Report:
            {
                var command = new ThermostatSetpointReportCommand(frame);
                var setpointValue = new ThermostatSetpointValue(
                    command.SetpointType,
                    command.Scale,
                    command.Value);

                if (_setpointValues != null)
                {
                    _setpointValues[command.SetpointType] = setpointValue;
                }
                else
                {
                    _setpointValues = new Dictionary<ThermostatSetpointType, ThermostatSetpointValue?>
                    {
                        [command.SetpointType] = setpointValue,
                    };
                }

                break;
            }
            case ThermostatSetpointCommand.CapabilitiesReport:
            {
                var command = new ThermostatSetpointCapabilitiesReportCommand(frame);
                _lastCapabilities = new ThermostatSetpointCapabilities(
                    command.SetpointType,
                    command.MinScale,
                    command.MinValue,
                    command.MaxScale,
                    command.MaxValue);
                break;
            }
        }
    }

    private static int EncodeValue(double value, int precision, int size)
    {
        int rawValue = (int)Math.Round(value * Math.Pow(10, precision));
        return rawValue;
    }

    private static void WriteIntBE(Span<byte> destination, int value, int size)
    {
        switch (size)
        {
            case 1:
                destination[0] = (byte)value;
                break;
            case 2:
                destination[0] = (byte)(value >> 8);
                destination[1] = (byte)value;
                break;
            case 4:
                destination[0] = (byte)(value >> 24);
                destination[1] = (byte)(value >> 16);
                destination[2] = (byte)(value >> 8);
                destination[3] = (byte)value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Size must be 1, 2, or 4.");
        }
    }

    private static (int Precision, int Size) DetermineEncoding(double value)
    {
        // Determine the number of decimal places needed (up to 3)
        int precision = 0;
        double scaled = value;
        while (precision < 3 && Math.Abs(scaled - Math.Round(scaled)) > 0.0001)
        {
            precision++;
            scaled = value * Math.Pow(10, precision);
        }

        int rawValue = (int)Math.Round(value * Math.Pow(10, precision));

        int size;
        if (rawValue >= sbyte.MinValue && rawValue <= sbyte.MaxValue)
        {
            size = 1;
        }
        else if (rawValue >= short.MinValue && rawValue <= short.MaxValue)
        {
            size = 2;
        }
        else
        {
            size = 4;
        }

        return (precision, size);
    }

    private readonly struct ThermostatSetpointSetCommand : ICommand
    {
        public ThermostatSetpointSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetpoint;

        public static byte CommandId => (byte)ThermostatSetpointCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ThermostatSetpointSetCommand Create(
            ThermostatSetpointType setpointType,
            ThermostatSetpointScale scale,
            double value)
        {
            (int precision, int size) = DetermineEncoding(value);
            int rawValue = EncodeValue(value, precision, size);

            Span<byte> commandParameters = stackalloc byte[2 + size];
            commandParameters[0] = (byte)((byte)setpointType & 0x0F);
            commandParameters[1] = (byte)((precision << 5) | (((byte)scale & 0x03) << 3) | (size & 0x07));
            WriteIntBE(commandParameters.Slice(2, size), rawValue, size);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ThermostatSetpointSetCommand(frame);
        }
    }

    private readonly struct ThermostatSetpointGetCommand : ICommand
    {
        public ThermostatSetpointGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetpoint;

        public static byte CommandId => (byte)ThermostatSetpointCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ThermostatSetpointGetCommand Create(ThermostatSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ThermostatSetpointGetCommand(frame);
        }
    }

    private readonly struct ThermostatSetpointReportCommand : ICommand
    {
        public ThermostatSetpointReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetpoint;

        public static byte CommandId => (byte)ThermostatSetpointCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The setpoint type being reported.
        /// </summary>
        public ThermostatSetpointType SetpointType => (ThermostatSetpointType)(Frame.CommandParameters.Span[0] & 0x0F);

        /// <summary>
        /// The scale used for the setpoint value.
        /// </summary>
        public ThermostatSetpointScale Scale => (ThermostatSetpointScale)((Frame.CommandParameters.Span[1] & 0b0001_1000) >> 3);

        /// <summary>
        /// The setpoint value.
        /// </summary>
        public double Value
        {
            get
            {
                int precision = (Frame.CommandParameters.Span[1] & 0b1110_0000) >> 5;

                int valueSize = Frame.CommandParameters.Span[1] & 0b0000_0111;
                var valueBytes = Frame.CommandParameters.Span.Slice(2, valueSize);

                if (valueBytes.Length > sizeof(int))
                {
                    throw new InvalidOperationException($"The value's size was more than {sizeof(int)} bytes, and currently we can't handle that");
                }

                int rawValue = valueBytes.ToInt32BE();

                return rawValue / Math.Pow(10, precision);
            }
        }
    }

    private readonly struct ThermostatSetpointSupportedGetCommand : ICommand
    {
        public ThermostatSetpointSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetpoint;

        public static byte CommandId => (byte)ThermostatSetpointCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static ThermostatSetpointSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ThermostatSetpointSupportedGetCommand(frame);
        }
    }

    private readonly struct ThermostatSetpointSupportedReportCommand : ICommand
    {
        public ThermostatSetpointSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetpoint;

        public static byte CommandId => (byte)ThermostatSetpointCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The supported setpoint types.
        /// </summary>
        public IReadOnlySet<ThermostatSetpointType> SupportedSetpointTypes
        {
            get
            {
                // The bitmask maps bit positions to setpoint types via a lookup table (interpretation A).
                // Bit positions do NOT directly correspond to the enum values because types 0x03-0x06 don't exist.
                ReadOnlySpan<byte> setpointTypeMap = [0x00, 0x01, 0x02, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];

                var supportedSetpointTypes = new HashSet<ThermostatSetpointType>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            int bitIndex = (byteNum * 8) + bitNum;
                            if (bitIndex < setpointTypeMap.Length)
                            {
                                ThermostatSetpointType setpointType = (ThermostatSetpointType)setpointTypeMap[bitIndex];
                                if (setpointType != ThermostatSetpointType.NotSupported)
                                {
                                    supportedSetpointTypes.Add(setpointType);
                                }
                            }
                        }
                    }
                }

                return supportedSetpointTypes;
            }
        }
    }

    private readonly struct ThermostatSetpointCapabilitiesGetCommand : ICommand
    {
        public ThermostatSetpointCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetpoint;

        public static byte CommandId => (byte)ThermostatSetpointCommand.CapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static ThermostatSetpointCapabilitiesGetCommand Create(ThermostatSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ThermostatSetpointCapabilitiesGetCommand(frame);
        }
    }

    private readonly struct ThermostatSetpointCapabilitiesReportCommand : ICommand
    {
        public ThermostatSetpointCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ThermostatSetpoint;

        public static byte CommandId => (byte)ThermostatSetpointCommand.CapabilitiesReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The setpoint type being reported.
        /// </summary>
        public ThermostatSetpointType SetpointType => (ThermostatSetpointType)(Frame.CommandParameters.Span[0] & 0x0F);

        /// <summary>
        /// The scale used for the minimum value.
        /// </summary>
        public ThermostatSetpointScale MinScale => (ThermostatSetpointScale)((Frame.CommandParameters.Span[1] >> 3) & 0x03);

        /// <summary>
        /// The minimum setpoint value.
        /// </summary>
        public double MinValue
        {
            get
            {
                int precision = (Frame.CommandParameters.Span[1] >> 5) & 0x07;
                int size = Frame.CommandParameters.Span[1] & 0x07;
                var valueBytes = Frame.CommandParameters.Span.Slice(2, size);

                if (valueBytes.Length > sizeof(int))
                {
                    throw new InvalidOperationException($"The value's size was more than {sizeof(int)} bytes, and currently we can't handle that");
                }

                int rawValue = valueBytes.ToInt32BE();
                return rawValue / Math.Pow(10, precision);
            }
        }

        /// <summary>
        /// The scale used for the maximum value.
        /// </summary>
        public ThermostatSetpointScale MaxScale
        {
            get
            {
                int minSize = Frame.CommandParameters.Span[1] & 0x07;
                return (ThermostatSetpointScale)((Frame.CommandParameters.Span[2 + minSize] >> 3) & 0x03);
            }
        }

        /// <summary>
        /// The maximum setpoint value.
        /// </summary>
        public double MaxValue
        {
            get
            {
                int minSize = Frame.CommandParameters.Span[1] & 0x07;
                byte precisionScaleSize = Frame.CommandParameters.Span[2 + minSize];
                int precision = (precisionScaleSize >> 5) & 0x07;
                int size = precisionScaleSize & 0x07;
                var valueBytes = Frame.CommandParameters.Span.Slice(3 + minSize, size);

                if (valueBytes.Length > sizeof(int))
                {
                    throw new InvalidOperationException($"The value's size was more than {sizeof(int)} bytes, and currently we can't handle that");
                }

                int rawValue = valueBytes.ToInt32BE();
                return rawValue / Math.Pow(10, precision);
            }
        }
    }
}
