namespace ZWave.CommandClasses;

public enum HumidityControlSetpointCommand : byte
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
    /// Advertise the supported scales for a given setpoint type.
    /// </summary>
    ScaleSupportedReport = 0x07,

    /// <summary>
    /// Request the capabilities for a given setpoint type.
    /// </summary>
    CapabilitiesGet = 0x08,

    /// <summary>
    /// Advertise the capabilities for a given setpoint type.
    /// </summary>
    CapabilitiesReport = 0x09,
}

/// <summary>
/// Represents a humidity control setpoint type.
/// </summary>
public enum HumidityControlSetpointType : byte
{
    /// <summary>
    /// Humidifier setpoint.
    /// </summary>
    Humidifier = 0x01,

    /// <summary>
    /// Dehumidifier setpoint.
    /// </summary>
    Dehumidifier = 0x02,

    /// <summary>
    /// Auto setpoint.
    /// </summary>
    Auto = 0x03,
}

/// <summary>
/// Represents the scale used for humidity control setpoint values.
/// </summary>
public enum HumidityControlSetpointScale : byte
{
    /// <summary>
    /// Percentage (%).
    /// </summary>
    Percentage = 0x00,

    /// <summary>
    /// Absolute humidity (g/m³).
    /// </summary>
    Absolute = 0x01,
}

/// <summary>
/// Represents a humidity control setpoint value.
/// </summary>
public readonly struct HumidityControlSetpointValue
{
    public HumidityControlSetpointValue(HumidityControlSetpointType setpointType, HumidityControlSetpointScale scale, double value)
    {
        SetpointType = setpointType;
        Scale = scale;
        Value = value;
    }

    /// <summary>
    /// The setpoint type.
    /// </summary>
    public HumidityControlSetpointType SetpointType { get; }

    /// <summary>
    /// The scale used for the setpoint value.
    /// </summary>
    public HumidityControlSetpointScale Scale { get; }

    /// <summary>
    /// The setpoint value.
    /// </summary>
    public double Value { get; }
}

/// <summary>
/// Represents the capabilities of a humidity control setpoint type.
/// </summary>
public readonly struct HumidityControlSetpointCapabilities
{
    public HumidityControlSetpointCapabilities(
        HumidityControlSetpointType setpointType,
        HumidityControlSetpointScale minScale,
        double minValue,
        HumidityControlSetpointScale maxScale,
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
    public HumidityControlSetpointType SetpointType { get; }

    /// <summary>
    /// The scale used for the minimum value.
    /// </summary>
    public HumidityControlSetpointScale MinScale { get; }

    /// <summary>
    /// The minimum setpoint value.
    /// </summary>
    public double MinValue { get; }

    /// <summary>
    /// The scale used for the maximum value.
    /// </summary>
    public HumidityControlSetpointScale MaxScale { get; }

    /// <summary>
    /// The maximum setpoint value.
    /// </summary>
    public double MaxValue { get; }
}

[CommandClass(CommandClassId.HumidityControlSetpoint)]
public sealed class HumidityControlSetpointCommandClass : CommandClass<HumidityControlSetpointCommand>
{
    private Dictionary<HumidityControlSetpointType, HumidityControlSetpointValue?>? _setpointValues;
    private IReadOnlySet<HumidityControlSetpointScale>? _lastScaleSupported;
    private HumidityControlSetpointCapabilities? _lastCapabilities;

    internal HumidityControlSetpointCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the supported setpoint types, or null if not yet determined.
    /// </summary>
    public IReadOnlySet<HumidityControlSetpointType>? SupportedSetpointTypes { get; private set; }

    /// <summary>
    /// Gets the last reported setpoint values keyed by setpoint type.
    /// </summary>
    public IReadOnlyDictionary<HumidityControlSetpointType, HumidityControlSetpointValue?>? SetpointValues => _setpointValues;

    /// <inheritdoc />
    public override bool? IsCommandSupported(HumidityControlSetpointCommand command)
        => command switch
        {
            HumidityControlSetpointCommand.Set => true,
            HumidityControlSetpointCommand.Get => true,
            HumidityControlSetpointCommand.SupportedGet => true,
            HumidityControlSetpointCommand.ScaleSupportedGet => true,
            HumidityControlSetpointCommand.CapabilitiesGet => true,
            _ => false,
        };

    /// <summary>
    /// Request the setpoint value for a given setpoint type.
    /// </summary>
    public async Task<HumidityControlSetpointValue> GetAsync(
        HumidityControlSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        var reportFrame = await AwaitNextReportAsync<HumidityControlSetpointReportCommand>(
            predicate: frame =>
            {
                var report = new HumidityControlSetpointReportCommand(frame);
                return report.SetpointType == setpointType;
            },
            cancellationToken).ConfigureAwait(false);
        var reportCommand = new HumidityControlSetpointReportCommand(reportFrame);
        return SetpointValues![reportCommand.SetpointType]!.Value;
    }

    /// <summary>
    /// Set the setpoint value for a given setpoint type.
    /// </summary>
    public async Task SetAsync(
        HumidityControlSetpointType setpointType,
        HumidityControlSetpointScale scale,
        double value,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointSetCommand.Create(setpointType, scale, value);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported setpoint types.
    /// </summary>
    public async Task<IReadOnlySet<HumidityControlSetpointType>> GetSupportedSetpointTypesAsync(CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<HumidityControlSetpointSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedSetpointTypes!;
    }

    /// <summary>
    /// Request the supported scales for a given setpoint type.
    /// </summary>
    public async Task<IReadOnlySet<HumidityControlSetpointScale>> GetScaleSupportedAsync(
        HumidityControlSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointScaleSupportedGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<HumidityControlSetpointScaleSupportedReportCommand>(
            predicate: frame =>
            {
                var report = new HumidityControlSetpointScaleSupportedReportCommand(frame);
                return report.SetpointType == setpointType;
            },
            cancellationToken).ConfigureAwait(false);
        return _lastScaleSupported!;
    }

    /// <summary>
    /// Request the capabilities for a given setpoint type.
    /// </summary>
    public async Task<HumidityControlSetpointCapabilities> GetCapabilitiesAsync(
        HumidityControlSetpointType setpointType,
        CancellationToken cancellationToken)
    {
        var command = HumidityControlSetpointCapabilitiesGetCommand.Create(setpointType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<HumidityControlSetpointCapabilitiesReportCommand>(
            predicate: frame =>
            {
                var report = new HumidityControlSetpointCapabilitiesReportCommand(frame);
                return report.SetpointType == setpointType;
            },
            cancellationToken).ConfigureAwait(false);
        return _lastCapabilities!.Value;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(HumidityControlSetpointCommand.SupportedGet).GetValueOrDefault())
        {
            IReadOnlySet<HumidityControlSetpointType> supportedTypes = await GetSupportedSetpointTypesAsync(cancellationToken).ConfigureAwait(false);
            foreach (HumidityControlSetpointType setpointType in supportedTypes)
            {
                _ = await GetAsync(setpointType, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((HumidityControlSetpointCommand)frame.CommandId)
        {
            case HumidityControlSetpointCommand.Set:
            case HumidityControlSetpointCommand.Get:
            case HumidityControlSetpointCommand.SupportedGet:
            case HumidityControlSetpointCommand.ScaleSupportedGet:
            case HumidityControlSetpointCommand.CapabilitiesGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case HumidityControlSetpointCommand.SupportedReport:
            {
                var command = new HumidityControlSetpointSupportedReportCommand(frame);
                SupportedSetpointTypes = command.SupportedSetpointTypes;

                var newSetpointValues = new Dictionary<HumidityControlSetpointType, HumidityControlSetpointValue?>();
                foreach (HumidityControlSetpointType setpointType in SupportedSetpointTypes)
                {
                    // Persist any existing known values.
                    if (_setpointValues == null
                        || !_setpointValues.TryGetValue(setpointType, out HumidityControlSetpointValue? existingValue))
                    {
                        existingValue = null;
                    }

                    newSetpointValues.Add(setpointType, existingValue);
                }

                _setpointValues = newSetpointValues;
                break;
            }
            case HumidityControlSetpointCommand.Report:
            {
                var command = new HumidityControlSetpointReportCommand(frame);
                var setpointValue = new HumidityControlSetpointValue(
                    command.SetpointType,
                    command.Scale,
                    command.Value);

                if (_setpointValues != null)
                {
                    _setpointValues[command.SetpointType] = setpointValue;
                }
                else
                {
                    _setpointValues = new Dictionary<HumidityControlSetpointType, HumidityControlSetpointValue?>
                    {
                        [command.SetpointType] = setpointValue,
                    };
                }

                break;
            }
            case HumidityControlSetpointCommand.ScaleSupportedReport:
            {
                var command = new HumidityControlSetpointScaleSupportedReportCommand(frame);
                _lastScaleSupported = command.SupportedScales;
                break;
            }
            case HumidityControlSetpointCommand.CapabilitiesReport:
            {
                var command = new HumidityControlSetpointCapabilitiesReportCommand(frame);
                _lastCapabilities = new HumidityControlSetpointCapabilities(
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

    private readonly struct HumidityControlSetpointSetCommand : ICommand
    {
        public HumidityControlSetpointSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.Set;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointSetCommand Create(
            HumidityControlSetpointType setpointType,
            HumidityControlSetpointScale scale,
            double value)
        {
            (int precision, int size) = DetermineEncoding(value);
            int rawValue = EncodeValue(value, precision, size);

            Span<byte> commandParameters = stackalloc byte[2 + size];
            commandParameters[0] = (byte)((byte)setpointType & 0x0F);
            commandParameters[1] = (byte)((precision << 5) | (((byte)scale & 0x03) << 3) | (size & 0x07));
            WriteIntBE(commandParameters.Slice(2, size), rawValue, size);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointSetCommand(frame);
        }
    }

    private readonly struct HumidityControlSetpointGetCommand : ICommand
    {
        public HumidityControlSetpointGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.Get;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointGetCommand Create(HumidityControlSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointGetCommand(frame);
        }
    }

    private readonly struct HumidityControlSetpointReportCommand : ICommand
    {
        public HumidityControlSetpointReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The setpoint type being reported.
        /// </summary>
        public HumidityControlSetpointType SetpointType => (HumidityControlSetpointType)(Frame.CommandParameters.Span[0] & 0x0F);

        /// <summary>
        /// The scale used for the setpoint value.
        /// </summary>
        public HumidityControlSetpointScale Scale => (HumidityControlSetpointScale)((Frame.CommandParameters.Span[1] & 0b0001_1000) >> 3);

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

    private readonly struct HumidityControlSetpointSupportedGetCommand : ICommand
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

    private readonly struct HumidityControlSetpointSupportedReportCommand : ICommand
    {
        public HumidityControlSetpointSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The supported setpoint types.
        /// </summary>
        public IReadOnlySet<HumidityControlSetpointType> SupportedSetpointTypes
        {
            get
            {
                var supportedSetpointTypes = new HashSet<HumidityControlSetpointType>();

                ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            var setpointType = (HumidityControlSetpointType)((byteNum << 3) + bitNum);
                            supportedSetpointTypes.Add(setpointType);
                        }
                    }
                }

                return supportedSetpointTypes;
            }
        }
    }

    private readonly struct HumidityControlSetpointScaleSupportedGetCommand : ICommand
    {
        public HumidityControlSetpointScaleSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.ScaleSupportedGet;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointScaleSupportedGetCommand Create(HumidityControlSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointScaleSupportedGetCommand(frame);
        }
    }

    private readonly struct HumidityControlSetpointScaleSupportedReportCommand : ICommand
    {
        public HumidityControlSetpointScaleSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.ScaleSupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The setpoint type being reported.
        /// </summary>
        public HumidityControlSetpointType SetpointType => (HumidityControlSetpointType)(Frame.CommandParameters.Span[0] & 0x0F);

        /// <summary>
        /// The supported scales.
        /// </summary>
        public IReadOnlySet<HumidityControlSetpointScale> SupportedScales
        {
            get
            {
                var supportedScales = new HashSet<HumidityControlSetpointScale>();
                byte scaleBitmask = (byte)(Frame.CommandParameters.Span[1] & 0x0F);

                for (int bitNum = 0; bitNum < 4; bitNum++)
                {
                    if ((scaleBitmask & (1 << bitNum)) != 0)
                    {
                        supportedScales.Add((HumidityControlSetpointScale)bitNum);
                    }
                }

                return supportedScales;
            }
        }
    }

    private readonly struct HumidityControlSetpointCapabilitiesGetCommand : ICommand
    {
        public HumidityControlSetpointCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.CapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static HumidityControlSetpointCapabilitiesGetCommand Create(HumidityControlSetpointType setpointType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)((byte)setpointType & 0x0F)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new HumidityControlSetpointCapabilitiesGetCommand(frame);
        }
    }

    private readonly struct HumidityControlSetpointCapabilitiesReportCommand : ICommand
    {
        public HumidityControlSetpointCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.HumidityControlSetpoint;

        public static byte CommandId => (byte)HumidityControlSetpointCommand.CapabilitiesReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The setpoint type being reported.
        /// </summary>
        public HumidityControlSetpointType SetpointType => (HumidityControlSetpointType)(Frame.CommandParameters.Span[0] & 0x0F);

        /// <summary>
        /// The scale used for the minimum value.
        /// </summary>
        public HumidityControlSetpointScale MinScale => (HumidityControlSetpointScale)((Frame.CommandParameters.Span[1] >> 3) & 0x03);

        /// <summary>
        /// The minimum setpoint value.
        /// </summary>
        public double MinValue
        {
            get
            {
                int precision = (Frame.CommandParameters.Span[1] >> 5) & 0x07;
                int size = Frame.CommandParameters.Span[1] & 0x07;
                ReadOnlySpan<byte> valueBytes = Frame.CommandParameters.Span.Slice(2, size);

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
        public HumidityControlSetpointScale MaxScale
        {
            get
            {
                int minSize = Frame.CommandParameters.Span[1] & 0x07;
                int maxPrecisionScaleSizeOffset = 2 + minSize;
                return (HumidityControlSetpointScale)((Frame.CommandParameters.Span[maxPrecisionScaleSizeOffset] >> 3) & 0x03);
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
                int maxPrecisionScaleSizeOffset = 2 + minSize;
                int precision = (Frame.CommandParameters.Span[maxPrecisionScaleSizeOffset] >> 5) & 0x07;
                int size = Frame.CommandParameters.Span[maxPrecisionScaleSizeOffset] & 0x07;
                ReadOnlySpan<byte> valueBytes = Frame.CommandParameters.Span.Slice(maxPrecisionScaleSizeOffset + 1, size);

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
