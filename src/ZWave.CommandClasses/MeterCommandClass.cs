namespace ZWave.CommandClasses;

public enum MeterCommand : byte
{
    /// <summary>
    /// Request a meter reading from a supporting node.
    /// </summary>
    Get = 0x01,

    /// <summary>
    /// Advertise a meter reading.
    /// </summary>
    Report = 0x02,

    /// <summary>
    /// Request the supported meter types and scales from a supporting node.
    /// </summary>
    SupportedGet = 0x03,

    /// <summary>
    /// Advertise the supported meter types and scales.
    /// </summary>
    SupportedReport = 0x04,

    /// <summary>
    /// Reset all accumulated meter values to zero.
    /// </summary>
    Reset = 0x05,
}

/// <summary>
/// Identifies the type of meter.
/// </summary>
public enum MeterType : byte
{
    Electric = 0x01,
    Gas = 0x02,
    Water = 0x03,
    Heating = 0x04,
    Cooling = 0x05,
}

/// <summary>
/// Identifies the rate type for a meter reading (v2+).
/// </summary>
public enum MeterRateType : byte
{
    /// <summary>
    /// The rate type is not specified.
    /// </summary>
    Unspecified = 0x00,

    /// <summary>
    /// Import (consumption).
    /// </summary>
    Import = 0x01,

    /// <summary>
    /// Export (production).
    /// </summary>
    Export = 0x02,
}

/// <summary>
/// Scale values for Electric meters (<see cref="MeterType.Electric"/>).
/// </summary>
public enum ElectricMeterScale : byte
{
    /// <summary>
    /// Energy in kilowatt-hours (kWh).
    /// </summary>
    KWh = 0x00,

    /// <summary>
    /// Energy in kilovolt-ampere-hours (kVAh).
    /// </summary>
    KVAh = 0x01,

    /// <summary>
    /// Power in watts (W).
    /// </summary>
    W = 0x02,

    /// <summary>
    /// Pulse count.
    /// </summary>
    PulseCount = 0x03,

    /// <summary>
    /// Voltage in volts (V).
    /// </summary>
    V = 0x04,

    /// <summary>
    /// Current in amperes (A).
    /// </summary>
    A = 0x05,

    /// <summary>
    /// Power factor.
    /// </summary>
    PowerFactor = 0x06,

    /// <summary>
    /// Reactive power in kilovolt-amperes reactive (kVar). V4+.
    /// </summary>
    KVar = 0x07,

    /// <summary>
    /// Reactive energy in kilovolt-ampere-hours reactive (kVarh). V4+.
    /// </summary>
    KVarh = 0x08,
}

/// <summary>
/// Scale values for Gas meters (<see cref="MeterType.Gas"/>).
/// </summary>
public enum GasMeterScale : byte
{
    /// <summary>
    /// Volume in cubic meters (m³).
    /// </summary>
    CubicMeters = 0x00,

    /// <summary>
    /// Volume in cubic feet (ft³).
    /// </summary>
    CubicFeet = 0x01,

    /// <summary>
    /// Pulse count.
    /// </summary>
    PulseCount = 0x03,
}

/// <summary>
/// Scale values for Water meters (<see cref="MeterType.Water"/>).
/// </summary>
public enum WaterMeterScale : byte
{
    /// <summary>
    /// Volume in cubic meters (m³).
    /// </summary>
    CubicMeters = 0x00,

    /// <summary>
    /// Volume in cubic feet (ft³).
    /// </summary>
    CubicFeet = 0x01,

    /// <summary>
    /// Volume in US gallons (gal).
    /// </summary>
    USGallons = 0x02,

    /// <summary>
    /// Pulse count.
    /// </summary>
    PulseCount = 0x03,
}

/// <summary>
/// Scale values for Heating meters (<see cref="MeterType.Heating"/>).
/// </summary>
public enum HeatingMeterScale : byte
{
    /// <summary>
    /// Energy in kilowatt-hours (kWh).
    /// </summary>
    KWh = 0x00,
}

/// <summary>
/// Scale values for Cooling meters (<see cref="MeterType.Cooling"/>).
/// </summary>
public enum CoolingMeterScale : byte
{
    /// <summary>
    /// Energy in kilowatt-hours (kWh).
    /// </summary>
    KWh = 0x00,
}

/// <summary>
/// Represents a meter reading.
/// </summary>
public readonly struct MeterState
{
    public MeterState(
        MeterType meterType,
        byte scale,
        MeterRateType rateType,
        double value,
        ushort? deltaTime,
        double? previousValue)
    {
        MeterType = meterType;
        Scale = scale;
        RateType = rateType;
        Value = value;
        DeltaTime = deltaTime;
        PreviousValue = previousValue;
    }

    /// <summary>
    /// The type of meter.
    /// </summary>
    public MeterType MeterType { get; }

    /// <summary>
    /// The scale used for the meter reading. Cast to the appropriate enum based on <see cref="MeterType"/>:
    /// <see cref="ElectricMeterScale"/>, <see cref="GasMeterScale"/>, <see cref="WaterMeterScale"/>,
    /// <see cref="HeatingMeterScale"/>, or <see cref="CoolingMeterScale"/>.
    /// </summary>
    public byte Scale { get; }

    /// <summary>
    /// The rate type of the meter reading (v2+).
    /// </summary>
    public MeterRateType RateType { get; }

    /// <summary>
    /// The meter reading value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// The time in seconds since the previous reading. Only present in v2+.
    /// </summary>
    public ushort? DeltaTime { get; }

    /// <summary>
    /// The previous meter reading value. Only present in v2+ when delta time is greater than zero.
    /// </summary>
    public double? PreviousValue { get; }
}

[CommandClass(CommandClassId.Meter)]
public sealed class MeterCommandClass : CommandClass<MeterCommand>
{
    internal MeterCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last reported meter state.
    /// </summary>
    public MeterState? State { get; private set; }

    /// <summary>
    /// Gets the supported meter type, or null if not yet determined.
    /// </summary>
    public MeterType? SupportedMeterType { get; private set; }

    /// <summary>
    /// Gets whether meter reset is supported, or null if not yet determined.
    /// </summary>
    public bool? IsResetSupported { get; private set; }

    /// <summary>
    /// Gets the set of supported scale values, or null if not yet determined.
    /// </summary>
    public IReadOnlySet<byte>? SupportedScales { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(MeterCommand command)
        => command switch
        {
            MeterCommand.Get => true,
            MeterCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            MeterCommand.Reset => Version.HasValue
                ? Version >= 2
                    ? IsResetSupported
                    : false
                : null,
            _ => false,
        };

    /// <summary>
    /// Request a meter reading.
    /// </summary>
    public async Task<MeterState> GetAsync(CancellationToken cancellationToken)
    {
        var command = MeterGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<MeterReportCommand>(cancellationToken).ConfigureAwait(false);
        return State!.Value;
    }

    /// <summary>
    /// Request the supported meter types and scales.
    /// </summary>
    public async Task GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = MeterSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<MeterSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reset all accumulated meter values to zero.
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var command = MeterResetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(MeterCommand.SupportedGet).GetValueOrDefault())
        {
            await GetSupportedAsync(cancellationToken).ConfigureAwait(false);
        }

        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((MeterCommand)frame.CommandId)
        {
            case MeterCommand.Get:
            case MeterCommand.SupportedGet:
            case MeterCommand.Reset:
            {
                // We don't expect to recieve these commands
                break;
            }
            case MeterCommand.Report:
            {
                var command = new MeterReportCommand(frame);
                State = new MeterState(
                    command.MeterType,
                    command.Scale,
                    command.RateType,
                    command.Value,
                    command.DeltaTime,
                    command.PreviousValue);
                break;
            }
            case MeterCommand.SupportedReport:
            {
                var command = new MeterSupportedReportCommand(frame);
                SupportedMeterType = command.MeterType;
                IsResetSupported = command.IsResetSupported;
                SupportedScales = command.SupportedScales;
                break;
            }
        }
    }

    private readonly struct MeterGetCommand : ICommand
    {
        public MeterGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MeterGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MeterGetCommand(frame);
        }
    }

    private readonly struct MeterReportCommand : ICommand
    {
        public MeterReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The type of meter.
        /// </summary>
        public MeterType MeterType => (MeterType)(Frame.CommandParameters.Span[0] & 0b0001_1111);

        /// <summary>
        /// The rate type (v2+: bits 5-6 of the meter type byte).
        /// </summary>
        public MeterRateType RateType => (MeterRateType)((Frame.CommandParameters.Span[0] & 0b0110_0000) >> 5);

        /// <summary>
        /// The scale of the meter reading. Cast to the appropriate enum based on <see cref="MeterType"/>:
        /// <see cref="ElectricMeterScale"/>, <see cref="GasMeterScale"/>, <see cref="WaterMeterScale"/>,
        /// <see cref="HeatingMeterScale"/>, or <see cref="CoolingMeterScale"/>.
        /// For V4+, when the 3-bit scale value is 7, a Scale 2 byte at the end of the report
        /// extends the range (total scale = 7 + Scale2).
        /// </summary>
        public byte Scale
        {
            get
            {
                byte scaleLow = (byte)((Frame.CommandParameters.Span[1] & 0b0001_1000) >> 3);
                byte scaleHigh = (byte)((Frame.CommandParameters.Span[0] & 0b1000_0000) >> 7);
                byte scale1 = (byte)((scaleHigh << 2) | scaleLow);

                if (scale1 == 7)
                {
                    // V4+: Scale 2 byte is appended at the end of the report
                    int valueSize = Frame.CommandParameters.Span[1] & 0b0000_0111;
                    int offset = 2 + valueSize;

                    if (Frame.CommandParameters.Length >= offset + 2)
                    {
                        ushort deltaTime = Frame.CommandParameters.Span.Slice(offset, 2).ToUInt16BE();
                        offset += 2;

                        if (deltaTime != 0 && Frame.CommandParameters.Length >= offset + valueSize)
                        {
                            offset += valueSize;
                        }
                    }

                    if (Frame.CommandParameters.Length > offset)
                    {
                        return (byte)(7 + Frame.CommandParameters.Span[offset]);
                    }
                }

                return scale1;
            }
        }

        /// <summary>
        /// The meter reading value.
        /// </summary>
        public double Value
        {
            get
            {
                int precision = (Frame.CommandParameters.Span[1] & 0b1110_0000) >> 5;

                int valueSize = Frame.CommandParameters.Span[1] & 0b0000_0111;
                ReadOnlySpan<byte> valueBytes = Frame.CommandParameters.Span.Slice(2, valueSize);

                if (valueBytes.Length > sizeof(int))
                {
                    throw new InvalidOperationException($"The value's size was more than {sizeof(int)} bytes, and currently we can't handle that");
                }

                int rawValue = valueBytes.ToInt32BE();

                return rawValue / Math.Pow(10, precision);
            }
        }

        /// <summary>
        /// The time in seconds since the previous reading (v2+).
        /// </summary>
        public ushort? DeltaTime
        {
            get
            {
                int valueSize = Frame.CommandParameters.Span[1] & 0b0000_0111;
                int deltaTimeOffset = 2 + valueSize;
                if (Frame.CommandParameters.Length < deltaTimeOffset + 2)
                {
                    return null;
                }

                return Frame.CommandParameters.Span.Slice(deltaTimeOffset, 2).ToUInt16BE();
            }
        }

        /// <summary>
        /// The previous meter reading value (v2+, only if delta time > 0).
        /// </summary>
        public double? PreviousValue
        {
            get
            {
                ushort? deltaTime = DeltaTime;
                if (!deltaTime.HasValue || deltaTime.Value == 0)
                {
                    return null;
                }

                int precision = (Frame.CommandParameters.Span[1] & 0b1110_0000) >> 5;
                int valueSize = Frame.CommandParameters.Span[1] & 0b0000_0111;
                int previousValueOffset = 2 + valueSize + 2;
                if (Frame.CommandParameters.Length < previousValueOffset + valueSize)
                {
                    return null;
                }

                ReadOnlySpan<byte> previousValueBytes = Frame.CommandParameters.Span.Slice(previousValueOffset, valueSize);

                if (previousValueBytes.Length > sizeof(int))
                {
                    throw new InvalidOperationException($"The value's size was more than {sizeof(int)} bytes, and currently we can't handle that");
                }

                int rawValue = previousValueBytes.ToInt32BE();

                return rawValue / Math.Pow(10, precision);
            }
        }
    }

    private readonly struct MeterSupportedGetCommand : ICommand
    {
        public MeterSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static MeterSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MeterSupportedGetCommand(frame);
        }
    }

    private readonly struct MeterSupportedReportCommand : ICommand
    {
        public MeterSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The supported meter type.
        /// </summary>
        public MeterType MeterType => (MeterType)(Frame.CommandParameters.Span[0] & 0b0001_1111);

        /// <summary>
        /// Whether meter reset is supported.
        /// </summary>
        public bool IsResetSupported => (Frame.CommandParameters.Span[0] & 0b1000_0000) != 0;

        /// <summary>
        /// The set of supported scales.
        /// </summary>
        public IReadOnlySet<byte> SupportedScales
        {
            get
            {
                var supportedScales = new HashSet<byte>();

                // Bit 7 of the scale supported byte indicates whether extended scales
                // are present in additional bytes (V4+)
                byte scaleByte = Frame.CommandParameters.Span[1];
                bool hasMoreScales = (scaleByte & 0b1000_0000) != 0;

                // Bits 0-6 represent scales 0-6
                for (int bitNum = 0; bitNum < 7; bitNum++)
                {
                    if ((scaleByte & (1 << bitNum)) != 0)
                    {
                        supportedScales.Add((byte)bitNum);
                    }
                }

                if (hasMoreScales && Frame.CommandParameters.Length > 2)
                {
                    // V4+: Additional scale bytes follow
                    int numScaleBytes = Frame.CommandParameters.Span[2];
                    for (int byteIdx = 0; byteIdx < numScaleBytes && (3 + byteIdx) < Frame.CommandParameters.Length; byteIdx++)
                    {
                        byte extraByte = Frame.CommandParameters.Span[3 + byteIdx];
                        for (int bitNum = 0; bitNum < 8; bitNum++)
                        {
                            if ((extraByte & (1 << bitNum)) != 0)
                            {
                                // Extended scales start at 7
                                supportedScales.Add((byte)(7 + (byteIdx * 8) + bitNum));
                            }
                        }
                    }
                }

                return supportedScales;
            }
        }
    }

    private readonly struct MeterResetCommand : ICommand
    {
        public MeterResetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.Reset;

        public CommandClassFrame Frame { get; }

        public static MeterResetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MeterResetCommand(frame);
        }
    }
}
