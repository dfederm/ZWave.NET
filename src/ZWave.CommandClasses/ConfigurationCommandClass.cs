namespace ZWave.CommandClasses;

public enum ConfigurationCommand : byte
{
    /// <summary>
    /// Reset all configuration parameters to their default values.
    /// </summary>
    DefaultReset = 0x01,

    /// <summary>
    /// Set a configuration parameter value in a device.
    /// </summary>
    Set = 0x04,

    /// <summary>
    /// Request the value of a configuration parameter from a device.
    /// </summary>
    Get = 0x05,

    /// <summary>
    /// Advertise the value of a configuration parameter.
    /// </summary>
    Report = 0x06,

    /// <summary>
    /// Set multiple configuration parameters in a device (V2).
    /// </summary>
    BulkSet = 0x07,

    /// <summary>
    /// Request multiple configuration parameter values from a device (V2).
    /// </summary>
    BulkGet = 0x08,

    /// <summary>
    /// Advertise multiple configuration parameter values (V2).
    /// </summary>
    BulkReport = 0x09,

    /// <summary>
    /// Request the name of a configuration parameter (V3).
    /// </summary>
    NameGet = 0x0A,

    /// <summary>
    /// Advertise the name of a configuration parameter (V3).
    /// </summary>
    NameReport = 0x0B,

    /// <summary>
    /// Request additional info about a configuration parameter (V3).
    /// </summary>
    InfoGet = 0x0C,

    /// <summary>
    /// Advertise additional info about a configuration parameter (V3).
    /// </summary>
    InfoReport = 0x0D,

    /// <summary>
    /// Request the properties of a configuration parameter (V3).
    /// </summary>
    PropertiesGet = 0x0E,

    /// <summary>
    /// Advertise the properties of a configuration parameter (V3).
    /// </summary>
    PropertiesReport = 0x0F,
}

/// <summary>
/// Represents a configuration parameter value with its size.
/// </summary>
public readonly struct ConfigurationValue
{
    public ConfigurationValue(int value, byte size)
    {
        Value = value;
        Size = size;
    }

    /// <summary>
    /// The value of the configuration parameter.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// The size of the configuration parameter value in bytes (1, 2, or 4).
    /// </summary>
    public byte Size { get; }
}

/// <summary>
/// The format of a configuration parameter value.
/// </summary>
public enum ConfigurationParameterFormat : byte
{
    Signed = 0,
    Unsigned = 1,
    Enum = 2,
    BitField = 3,
}

/// <summary>
/// Represents the properties of a configuration parameter.
/// </summary>
public readonly struct ConfigurationParameterProperties
{
    public ConfigurationParameterProperties(
        ushort parameterNumber,
        byte size,
        ConfigurationParameterFormat format,
        int minValue,
        int maxValue,
        int defaultValue,
        bool isReadOnly,
        bool isAlteringCapabilities,
        ushort nextParameterNumber)
    {
        ParameterNumber = parameterNumber;
        Size = size;
        Format = format;
        MinValue = minValue;
        MaxValue = maxValue;
        DefaultValue = defaultValue;
        IsReadOnly = isReadOnly;
        IsAlteringCapabilities = isAlteringCapabilities;
        NextParameterNumber = nextParameterNumber;
    }

    public ushort ParameterNumber { get; }

    public byte Size { get; }

    public ConfigurationParameterFormat Format { get; }

    public int MinValue { get; }

    public int MaxValue { get; }

    public int DefaultValue { get; }

    public bool IsReadOnly { get; }

    public bool IsAlteringCapabilities { get; }

    public ushort NextParameterNumber { get; }
}

[CommandClass(CommandClassId.Configuration)]
public sealed class ConfigurationCommandClass : CommandClass<ConfigurationCommand>
{
    private readonly Dictionary<byte, ConfigurationValue> _parameters = new Dictionary<byte, ConfigurationValue>();

    private string? _lastNameReport;
    private string? _lastInfoReport;
    private ConfigurationParameterProperties _lastPropertiesReport;

    internal ConfigurationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the known configuration parameter values.
    /// </summary>
    public IReadOnlyDictionary<byte, ConfigurationValue> Parameters => _parameters;

    /// <inheritdoc />
    public override bool? IsCommandSupported(ConfigurationCommand command)
        => command switch
        {
            ConfigurationCommand.Set => true,
            ConfigurationCommand.Get => true,
            ConfigurationCommand.BulkSet => Version.HasValue ? Version >= 2 : null,
            ConfigurationCommand.BulkGet => Version.HasValue ? Version >= 2 : null,
            ConfigurationCommand.NameGet => Version.HasValue ? Version >= 3 : null,
            ConfigurationCommand.InfoGet => Version.HasValue ? Version >= 3 : null,
            ConfigurationCommand.PropertiesGet => Version.HasValue ? Version >= 3 : null,
            ConfigurationCommand.DefaultReset => Version.HasValue ? Version >= 4 : null,
            _ => false,
        };

    internal override Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Configuration parameters are device-specific and cannot be automatically interviewed.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Request the value of a configuration parameter from a device.
    /// </summary>
    public async Task<ConfigurationValue> GetAsync(byte parameterNumber, CancellationToken cancellationToken)
    {
        ConfigurationGetCommand command = ConfigurationGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ConfigurationReportCommand>(
            frame => frame.CommandParameters.Span[0] == parameterNumber,
            cancellationToken).ConfigureAwait(false);
        return _parameters[parameterNumber];
    }

    /// <summary>
    /// Set a configuration parameter value in a device.
    /// </summary>
    public async Task SetAsync(byte parameterNumber, int value, byte size, CancellationToken cancellationToken)
    {
        ConfigurationSetCommand command = ConfigurationSetCommand.Create(parameterNumber, value, size);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the name of a configuration parameter from a device (V3+).
    /// </summary>
    public async Task<string> GetNameAsync(ushort parameterNumber, CancellationToken cancellationToken)
    {
        ConfigurationNameGetCommand command = ConfigurationNameGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ConfigurationNameReportCommand>(
            frame => frame.CommandParameters.Span[0..2].ToUInt16BE() == parameterNumber,
            cancellationToken).ConfigureAwait(false);
        return _lastNameReport!;
    }

    /// <summary>
    /// Request additional info about a configuration parameter from a device (V3+).
    /// </summary>
    public async Task<string> GetInfoAsync(ushort parameterNumber, CancellationToken cancellationToken)
    {
        ConfigurationInfoGetCommand command = ConfigurationInfoGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ConfigurationInfoReportCommand>(
            frame => frame.CommandParameters.Span[0..2].ToUInt16BE() == parameterNumber,
            cancellationToken).ConfigureAwait(false);
        return _lastInfoReport!;
    }

    /// <summary>
    /// Request the properties of a configuration parameter from a device (V3+).
    /// </summary>
    public async Task<ConfigurationParameterProperties> GetPropertiesAsync(ushort parameterNumber, CancellationToken cancellationToken)
    {
        ConfigurationPropertiesGetCommand command = ConfigurationPropertiesGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<ConfigurationPropertiesReportCommand>(
            frame => frame.CommandParameters.Span[0..2].ToUInt16BE() == parameterNumber,
            cancellationToken).ConfigureAwait(false);
        return _lastPropertiesReport;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((ConfigurationCommand)frame.CommandId)
        {
            case ConfigurationCommand.Set:
            case ConfigurationCommand.Get:
            case ConfigurationCommand.BulkSet:
            case ConfigurationCommand.BulkGet:
            case ConfigurationCommand.NameGet:
            case ConfigurationCommand.InfoGet:
            case ConfigurationCommand.PropertiesGet:
            case ConfigurationCommand.DefaultReset:
            {
                // We don't expect to recieve these commands
                break;
            }
            case ConfigurationCommand.Report:
            {
                ConfigurationReportCommand command = new ConfigurationReportCommand(frame);
                _parameters[command.ParameterNumber] = new ConfigurationValue(
                    command.Value,
                    command.Size);
                break;
            }
            case ConfigurationCommand.BulkReport:
            {
                ConfigurationBulkReportCommand command = new ConfigurationBulkReportCommand(frame);
                ushort parameterOffset = command.ParameterOffset;
                byte size = command.Size;
                IReadOnlyList<int> values = command.Values;
                for (int i = 0; i < values.Count; i++)
                {
                    byte parameterNumber = (byte)(parameterOffset + i);
                    _parameters[parameterNumber] = new ConfigurationValue(values[i], size);
                }

                break;
            }
            case ConfigurationCommand.NameReport:
            {
                ConfigurationNameReportCommand command = new ConfigurationNameReportCommand(frame);
                _lastNameReport = command.Name;
                break;
            }
            case ConfigurationCommand.InfoReport:
            {
                ConfigurationInfoReportCommand command = new ConfigurationInfoReportCommand(frame);
                _lastInfoReport = command.Info;
                break;
            }
            case ConfigurationCommand.PropertiesReport:
            {
                ConfigurationPropertiesReportCommand command = new ConfigurationPropertiesReportCommand(frame);
                _lastPropertiesReport = new ConfigurationParameterProperties(
                    command.ParameterNumber,
                    command.Size,
                    command.Format,
                    command.MinValue,
                    command.MaxValue,
                    command.DefaultValue,
                    command.IsReadOnly,
                    command.IsAlteringCapabilities,
                    command.NextParameterNumber);
                break;
            }
        }
    }

    private readonly struct ConfigurationSetCommand : ICommand
    {
        public ConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ConfigurationSetCommand Create(byte parameterNumber, int value, byte size)
        {
            if (size != 1 && size != 2 && size != 4)
            {
                throw new ArgumentException("Size must be 1, 2, or 4.", nameof(size));
            }

            Span<byte> commandParameters = stackalloc byte[2 + size];
            commandParameters[0] = parameterNumber;
            commandParameters[1] = (byte)(size & 0x07);

            switch (size)
            {
                case 1:
                {
                    commandParameters[2] = (byte)value;
                    break;
                }
                case 2:
                {
                    ((ushort)value).WriteBytesBE(commandParameters[2..]);
                    break;
                }
                case 4:
                {
                    ((uint)value).WriteBytesBE(commandParameters[2..]);
                    break;
                }
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationSetCommand(frame);
        }
    }

    private readonly struct ConfigurationGetCommand : ICommand
    {
        public ConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ConfigurationGetCommand Create(byte parameterNumber)
        {
            ReadOnlySpan<byte> commandParameters = [parameterNumber];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationGetCommand(frame);
        }
    }

    private readonly struct ConfigurationReportCommand : ICommand
    {
        public ConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The parameter number being reported.
        /// </summary>
        public byte ParameterNumber => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The size of the value in bytes (1, 2, or 4).
        /// </summary>
        public byte Size => (byte)(Frame.CommandParameters.Span[1] & 0x07);

        /// <summary>
        /// The value of the configuration parameter, parsed based on the size field.
        /// </summary>
        public int Value => ParseValue(Frame.CommandParameters.Span[2..], Size);

        private static int ParseValue(ReadOnlySpan<byte> data, byte size)
            => size switch
            {
                1 => unchecked((sbyte)data[0]),
                2 => unchecked((short)data[0..2].ToUInt16BE()),
                4 => data[0..4].ToInt32BE(),
                _ => 0,
            };
    }

    private readonly struct ConfigurationBulkReportCommand : ICommand
    {
        public ConfigurationBulkReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.BulkReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The parameter offset (first parameter number in the bulk report).
        /// </summary>
        public ushort ParameterOffset => Frame.CommandParameters.Span[0..2].ToUInt16BE();

        /// <summary>
        /// The number of parameters reported.
        /// </summary>
        public byte NumberOfParameters => Frame.CommandParameters.Span[2];

        /// <summary>
        /// The size of each parameter value in bytes (1, 2, or 4).
        /// </summary>
        public byte Size => (byte)(Frame.CommandParameters.Span[3] & 0x07);

        /// <summary>
        /// The parameter values in the bulk report.
        /// </summary>
        public IReadOnlyList<int> Values
        {
            get
            {
                byte size = Size;
                byte count = NumberOfParameters;
                List<int> values = new List<int>(count);
                ReadOnlySpan<byte> data = Frame.CommandParameters.Span[4..];
                for (int i = 0; i < count; i++)
                {
                    int offset = i * size;
                    int value = size switch
                    {
                        1 => unchecked((sbyte)data[offset]),
                        2 => unchecked((short)data.Slice(offset, 2).ToUInt16BE()),
                        4 => data.Slice(offset, 4).ToInt32BE(),
                        _ => 0,
                    };
                    values.Add(value);
                }

                return values;
            }
        }
    }

    private readonly struct ConfigurationNameGetCommand : ICommand
    {
        public ConfigurationNameGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.NameGet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationNameGetCommand Create(ushort parameterNumber)
        {
            Span<byte> commandParameters = stackalloc byte[2];
            parameterNumber.WriteBytesBE(commandParameters);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationNameGetCommand(frame);
        }
    }

    private readonly struct ConfigurationNameReportCommand : ICommand
    {
        public ConfigurationNameReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.NameReport;

        public CommandClassFrame Frame { get; }

        public ushort ParameterNumber => Frame.CommandParameters.Span[0..2].ToUInt16BE();

        public byte ReportsToFollow => Frame.CommandParameters.Span[2];

        public string Name => System.Text.Encoding.UTF8.GetString(Frame.CommandParameters.Span[3..]);
    }

    private readonly struct ConfigurationInfoGetCommand : ICommand
    {
        public ConfigurationInfoGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.InfoGet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationInfoGetCommand Create(ushort parameterNumber)
        {
            Span<byte> commandParameters = stackalloc byte[2];
            parameterNumber.WriteBytesBE(commandParameters);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationInfoGetCommand(frame);
        }
    }

    private readonly struct ConfigurationInfoReportCommand : ICommand
    {
        public ConfigurationInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.InfoReport;

        public CommandClassFrame Frame { get; }

        public ushort ParameterNumber => Frame.CommandParameters.Span[0..2].ToUInt16BE();

        public byte ReportsToFollow => Frame.CommandParameters.Span[2];

        public string Info => System.Text.Encoding.UTF8.GetString(Frame.CommandParameters.Span[3..]);
    }

    private readonly struct ConfigurationPropertiesGetCommand : ICommand
    {
        public ConfigurationPropertiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.PropertiesGet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationPropertiesGetCommand Create(ushort parameterNumber)
        {
            Span<byte> commandParameters = stackalloc byte[2];
            parameterNumber.WriteBytesBE(commandParameters);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationPropertiesGetCommand(frame);
        }
    }

    private readonly struct ConfigurationPropertiesReportCommand : ICommand
    {
        public ConfigurationPropertiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.PropertiesReport;

        public CommandClassFrame Frame { get; }

        public ushort ParameterNumber => Frame.CommandParameters.Span[0..2].ToUInt16BE();

        private byte FormatAndSizeByte => Frame.CommandParameters.Span[2];

        public byte Size => (byte)(FormatAndSizeByte & 0x07);

        public ConfigurationParameterFormat Format => (ConfigurationParameterFormat)((FormatAndSizeByte >> 3) & 0x07);

        public bool IsReadOnly => (FormatAndSizeByte & 0x40) != 0;

        public bool IsAlteringCapabilities => (FormatAndSizeByte & 0x80) != 0;

        public int MinValue => ParseValue(Frame.CommandParameters.Span.Slice(3, Size), Size);

        public int MaxValue => ParseValue(Frame.CommandParameters.Span.Slice(3 + Size, Size), Size);

        public int DefaultValue => ParseValue(Frame.CommandParameters.Span.Slice(3 + 2 * Size, Size), Size);

        public ushort NextParameterNumber => Frame.CommandParameters.Span.Slice(3 + 3 * Size, 2).ToUInt16BE();

        private static int ParseValue(ReadOnlySpan<byte> data, byte size)
            => size switch
            {
                1 => unchecked((sbyte)data[0]),
                2 => unchecked((short)data[0..2].ToUInt16BE()),
                4 => data[0..4].ToInt32BE(),
                _ => 0,
            };
    }
}
