using System.Text;

namespace ZWave.CommandClasses;

public enum SoundSwitchCommand : byte
{
    /// <summary>
    /// Request the number of supported tones from a node.
    /// </summary>
    TonesNumberGet = 0x01,

    /// <summary>
    /// Advertise the number of supported tones at the sending node.
    /// </summary>
    TonesNumberReport = 0x02,

    /// <summary>
    /// Request information about a specific tone from a node.
    /// </summary>
    ToneInfoGet = 0x03,

    /// <summary>
    /// Advertise information about a specific tone at the sending node.
    /// </summary>
    ToneInfoReport = 0x04,

    /// <summary>
    /// Set the default tone configuration at the receiving node.
    /// </summary>
    ConfigurationSet = 0x05,

    /// <summary>
    /// Request the default tone configuration from a node.
    /// </summary>
    ConfigurationGet = 0x06,

    /// <summary>
    /// Advertise the default tone configuration at the sending node.
    /// </summary>
    ConfigurationReport = 0x07,

    /// <summary>
    /// Start or stop playing a tone at the receiving node.
    /// </summary>
    TonePlaySet = 0x08,

    /// <summary>
    /// Request the current tone play state from a node.
    /// </summary>
    TonePlayGet = 0x09,

    /// <summary>
    /// Advertise the current tone play state at the sending node.
    /// </summary>
    TonePlayReport = 0x0A,
}

/// <summary>
/// Represents information about a specific tone.
/// </summary>
public readonly struct SoundSwitchToneInfo
{
    public SoundSwitchToneInfo(byte toneIdentifier, ushort toneDurationSeconds, string name)
    {
        ToneIdentifier = toneIdentifier;
        ToneDurationSeconds = toneDurationSeconds;
        Name = name;
    }

    /// <summary>
    /// The tone identifier.
    /// </summary>
    public byte ToneIdentifier { get; }

    /// <summary>
    /// The tone duration in seconds.
    /// </summary>
    public ushort ToneDurationSeconds { get; }

    /// <summary>
    /// The name of the tone.
    /// </summary>
    public string Name { get; }
}

[CommandClass(CommandClassId.SoundSwitch)]
public sealed class SoundSwitchCommandClass : CommandClass<SoundSwitchCommand>
{
    internal SoundSwitchCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the number of supported tones.
    /// </summary>
    public byte? SupportedTones { get; private set; }

    /// <summary>
    /// Gets the default volume (0-100, 0xFF for default).
    /// </summary>
    public byte? DefaultVolume { get; private set; }

    /// <summary>
    /// Gets the default tone identifier (0x00 for last played tone).
    /// </summary>
    public byte? DefaultToneIdentifier { get; private set; }

    /// <summary>
    /// Gets the currently playing tone identifier (0x00 for no active tone).
    /// </summary>
    public byte? CurrentToneIdentifier { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(SoundSwitchCommand command) => true;

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetTonesNumberAsync(cancellationToken).ConfigureAwait(false);
        await GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the number of supported tones from a node.
    /// </summary>
    public async Task<byte> GetTonesNumberAsync(CancellationToken cancellationToken)
    {
        var command = SoundSwitchTonesNumberGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<SoundSwitchTonesNumberReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedTones!.Value;
    }

    /// <summary>
    /// Request information about a specific tone from a node.
    /// </summary>
    public async Task<SoundSwitchToneInfo> GetToneInfoAsync(byte toneIdentifier, CancellationToken cancellationToken)
    {
        var command = SoundSwitchToneInfoGetCommand.Create(toneIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame frame = await AwaitNextReportAsync<SoundSwitchToneInfoReportCommand>(cancellationToken).ConfigureAwait(false);
        var report = new SoundSwitchToneInfoReportCommand(frame);
        return new SoundSwitchToneInfo(report.ToneIdentifier, report.ToneDuration, report.Name);
    }

    /// <summary>
    /// Set the default tone configuration at the receiving node.
    /// </summary>
    /// <param name="volume">The default volume (0-100, 0xFF for device default).</param>
    /// <param name="defaultToneIdentifier">The default tone identifier (0x00 for last played tone).</param>
    public async Task SetConfigurationAsync(byte volume, byte defaultToneIdentifier, CancellationToken cancellationToken)
    {
        var command = SoundSwitchConfigurationSetCommand.Create(volume, defaultToneIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the default tone configuration from a node.
    /// </summary>
    public async Task GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var command = SoundSwitchConfigurationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<SoundSwitchConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Start or stop playing a tone at the receiving node.
    /// </summary>
    /// <param name="toneIdentifier">The tone identifier (0x00 to stop, 0xFF for default tone).</param>
    /// <param name="volume">The playback volume (v2+).</param>
    public async Task SetTonePlayAsync(byte toneIdentifier, byte? volume, CancellationToken cancellationToken)
    {
        var command = SoundSwitchTonePlaySetCommand.Create(EffectiveVersion, toneIdentifier, volume);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current tone play state from a node.
    /// </summary>
    public async Task<byte> GetTonePlayAsync(CancellationToken cancellationToken)
    {
        var command = SoundSwitchTonePlayGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<SoundSwitchTonePlayReportCommand>(cancellationToken).ConfigureAwait(false);
        return CurrentToneIdentifier!.Value;
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((SoundSwitchCommand)frame.CommandId)
        {
            case SoundSwitchCommand.TonesNumberGet:
            case SoundSwitchCommand.ToneInfoGet:
            case SoundSwitchCommand.ConfigurationSet:
            case SoundSwitchCommand.ConfigurationGet:
            case SoundSwitchCommand.TonePlaySet:
            case SoundSwitchCommand.TonePlayGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case SoundSwitchCommand.TonesNumberReport:
            {
                var command = new SoundSwitchTonesNumberReportCommand(frame);
                SupportedTones = command.SupportedTones;
                break;
            }
            case SoundSwitchCommand.ToneInfoReport:
            {
                // Tone info is returned on demand via GetToneInfoAsync
                break;
            }
            case SoundSwitchCommand.ConfigurationReport:
            {
                var command = new SoundSwitchConfigurationReportCommand(frame);
                DefaultVolume = command.Volume;
                DefaultToneIdentifier = command.DefaultToneIdentifier;
                break;
            }
            case SoundSwitchCommand.TonePlayReport:
            {
                var command = new SoundSwitchTonePlayReportCommand(frame);
                CurrentToneIdentifier = command.ToneIdentifier;
                break;
            }
        }
    }

    private readonly struct SoundSwitchTonesNumberGetCommand : ICommand
    {
        public SoundSwitchTonesNumberGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonesNumberGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchTonesNumberGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new SoundSwitchTonesNumberGetCommand(frame);
        }
    }

    private readonly struct SoundSwitchTonesNumberReportCommand : ICommand
    {
        public SoundSwitchTonesNumberReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonesNumberReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The number of tones supported by the device.
        /// </summary>
        public byte SupportedTones => Frame.CommandParameters.Span[0];
    }

    private readonly struct SoundSwitchToneInfoGetCommand : ICommand
    {
        public SoundSwitchToneInfoGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ToneInfoGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchToneInfoGetCommand Create(byte toneIdentifier)
        {
            ReadOnlySpan<byte> commandParameters = [toneIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SoundSwitchToneInfoGetCommand(frame);
        }
    }

    private readonly struct SoundSwitchToneInfoReportCommand : ICommand
    {
        public SoundSwitchToneInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ToneInfoReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The tone identifier.
        /// </summary>
        public byte ToneIdentifier => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The tone duration in seconds.
        /// </summary>
        public ushort ToneDuration => Frame.CommandParameters.Span[1..3].ToUInt16BE();

        /// <summary>
        /// The name of the tone (UTF-8 encoded).
        /// </summary>
        public string Name
        {
            get
            {
                byte nameLength = Frame.CommandParameters.Span[3];
                return Encoding.UTF8.GetString(Frame.CommandParameters.Span.Slice(4, nameLength));
            }
        }
    }

    private readonly struct SoundSwitchConfigurationSetCommand : ICommand
    {
        public SoundSwitchConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ConfigurationSet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchConfigurationSetCommand Create(byte volume, byte defaultToneIdentifier)
        {
            ReadOnlySpan<byte> commandParameters = [volume, defaultToneIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SoundSwitchConfigurationSetCommand(frame);
        }
    }

    private readonly struct SoundSwitchConfigurationGetCommand : ICommand
    {
        public SoundSwitchConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ConfigurationGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchConfigurationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new SoundSwitchConfigurationGetCommand(frame);
        }
    }

    private readonly struct SoundSwitchConfigurationReportCommand : ICommand
    {
        public SoundSwitchConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ConfigurationReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The configured default volume (0-100, 0xFF for device default).
        /// </summary>
        public byte Volume => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The configured default tone identifier (0x00 for last played tone).
        /// </summary>
        public byte DefaultToneIdentifier => Frame.CommandParameters.Span[1];
    }

    private readonly struct SoundSwitchTonePlaySetCommand : ICommand
    {
        public SoundSwitchTonePlaySetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonePlaySet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchTonePlaySetCommand Create(byte version, byte toneIdentifier, byte? volume)
        {
            bool includeVolume = version >= 2 && volume.HasValue;
            Span<byte> commandParameters = stackalloc byte[1 + (includeVolume ? 1 : 0)];
            commandParameters[0] = toneIdentifier;
            if (includeVolume)
            {
                commandParameters[1] = volume!.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SoundSwitchTonePlaySetCommand(frame);
        }
    }

    private readonly struct SoundSwitchTonePlayGetCommand : ICommand
    {
        public SoundSwitchTonePlayGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonePlayGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchTonePlayGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new SoundSwitchTonePlayGetCommand(frame);
        }
    }

    private readonly struct SoundSwitchTonePlayReportCommand : ICommand
    {
        public SoundSwitchTonePlayReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonePlayReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The currently playing tone identifier (0x00 for no active tone).
        /// </summary>
        public byte ToneIdentifier => Frame.CommandParameters.Span[0];
    }
}
