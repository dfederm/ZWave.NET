using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies a color component of a color switch device.
/// </summary>
public enum ColorSwitchColorComponent : byte
{
    WarmWhite = 0x00,

    ColdWhite = 0x01,

    Red = 0x02,

    Green = 0x03,

    Blue = 0x04,

    Amber = 0x05,

    Cyan = 0x06,

    Purple = 0x07,

    Index = 0x08,
}

/// <summary>
/// The direction of a color switch level change.
/// </summary>
public enum ColorSwitchChangeDirection : byte
{
    Up = 0x00,

    Down = 0x01,
}

public enum ColorSwitchCommand : byte
{
    /// <summary>
    /// Request the supported color components of a device
    /// </summary>
    SupportedGet = 0x01,

    /// <summary>
    /// Report the supported color components of a device
    /// </summary>
    SupportedReport = 0x02,

    /// <summary>
    /// Request the status of a specified color component
    /// </summary>
    Get = 0x03,

    /// <summary>
    /// Report the status of a specified color component.
    /// </summary>
    Report = 0x04,

    /// <summary>
    /// Set the value of one or more color components.
    /// </summary>
    Set = 0x05,

    /// <summary>
    /// Initiate a color component level change.
    /// </summary>
    StartLevelChange = 0x06,

    /// <summary>
    /// Stop an ongoing color component level change.
    /// </summary>
    StopLevelChange = 0x07,
}

/// <summary>
/// Represents the state of a single color component.
/// </summary>
public readonly record struct ColorSwitchColorComponentState(
    /// <summary>
    /// The current value of the color component identified by the Color Component ID
    /// </summary>
    byte CurrentValue,

    /// <summary>
    /// The target value of an ongoing transition or the most recent transition for the advertised Color Component ID.
    /// </summary>
    byte? TargetValue,

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate.
    /// </summary>
    DurationReport? Duration);

[CommandClass(CommandClassId.ColorSwitch)]
public sealed class ColorSwitchCommandClass : CommandClass<ColorSwitchCommand>
{
    private Dictionary<ColorSwitchColorComponent, ColorSwitchColorComponentState?>? _colorComponents;

    public ColorSwitchCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    /// <summary>
    /// The color components supported by the device
    /// </summary>
    public IReadOnlySet<ColorSwitchColorComponent>? SupportedComponents { get; private set; }

    /// <summary>
    /// The state of the color components supported by the device
    /// </summary>
    public IReadOnlyDictionary<ColorSwitchColorComponent, ColorSwitchColorComponentState?>? ColorComponents => _colorComponents;

    /// <inheritdoc />
    public override bool? IsCommandSupported(ColorSwitchCommand command)
        => command switch
        {
            ColorSwitchCommand.SupportedGet => true,
            ColorSwitchCommand.Get => true,
            ColorSwitchCommand.Set => true,
            ColorSwitchCommand.StartLevelChange => true,
            ColorSwitchCommand.StopLevelChange => true,
            _ => false,
        };

    public async Task<IReadOnlySet<ColorSwitchColorComponent>> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = ColorSwitchSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ColorSwitchSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<ColorSwitchColorComponent> supportedComponents = ColorSwitchSupportedReportCommand.Parse(reportFrame, Logger);

        SupportedComponents = supportedComponents;

        var newColorComponents = new Dictionary<ColorSwitchColorComponent, ColorSwitchColorComponentState?>();
        foreach (ColorSwitchColorComponent colorComponent in supportedComponents)
        {
            // Persist any existing known state.
            if (ColorComponents == null
                || !ColorComponents.TryGetValue(colorComponent, out ColorSwitchColorComponentState? colorComponentState))
            {
                colorComponentState = null;
            }

            newColorComponents.Add(colorComponent, colorComponentState);
        }

        _colorComponents = newColorComponents;

        return supportedComponents;
    }

    public async Task<ColorSwitchColorComponentState> GetAsync(
        ColorSwitchColorComponent colorComponent,
        CancellationToken cancellationToken)
    {
        var command = ColorSwitchGetCommand.Create(colorComponent);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ColorSwitchReportCommand>(cancellationToken).ConfigureAwait(false);
        (ColorSwitchColorComponent reportedComponent, ColorSwitchColorComponentState state) = ColorSwitchReportCommand.Parse(reportFrame, Logger);
        _colorComponents![reportedComponent] = state;
        return state;
    }

    public async Task SetAsync(
        IReadOnlyDictionary<ColorSwitchColorComponent, byte> values,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = ColorSwitchSetCommand.Create(EffectiveVersion, values, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task StartLevelChangeAsync(
        ColorSwitchChangeDirection direction,
        ColorSwitchColorComponent colorComponent,
        byte? startLevel,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = ColorSwitchStartLevelChangeCommand.Create(
            EffectiveVersion,
            direction,
            colorComponent,
            startLevel,
            duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task StopLevelChangeAsync(ColorSwitchColorComponent colorComponent, CancellationToken cancellationToken)
    {
        var command = ColorSwitchStopLevelChangeCommand.Create(colorComponent);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        IReadOnlySet<ColorSwitchColorComponent> supportedColorComponents = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

        foreach (var colorComponent in supportedColorComponents)
        {
            _ = await GetAsync(colorComponent, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ColorSwitchCommand)frame.CommandId)
        {
            case ColorSwitchCommand.SupportedGet:
            case ColorSwitchCommand.Get:
            case ColorSwitchCommand.Set:
            case ColorSwitchCommand.StartLevelChange:
            case ColorSwitchCommand.StopLevelChange:
            {
                break;
            }
            case ColorSwitchCommand.SupportedReport:
            {
                IReadOnlySet<ColorSwitchColorComponent> supportedComponents = ColorSwitchSupportedReportCommand.Parse(frame, Logger);
                SupportedComponents = supportedComponents;

                var newColorComponents = new Dictionary<ColorSwitchColorComponent, ColorSwitchColorComponentState?>();
                foreach (ColorSwitchColorComponent colorComponent in supportedComponents)
                {
                    // Persist any existing known state.
                    if (ColorComponents == null
                        || !ColorComponents.TryGetValue(colorComponent, out ColorSwitchColorComponentState? colorComponentState))
                    {
                        colorComponentState = null;
                    }

                    newColorComponents.Add(colorComponent, colorComponentState);
                }

                _colorComponents = newColorComponents;

                break;
            }
            case ColorSwitchCommand.Report:
            {
                (ColorSwitchColorComponent reportedComponent, ColorSwitchColorComponentState state) = ColorSwitchReportCommand.Parse(frame, Logger);
                _colorComponents![reportedComponent] = state;
                break;
            }
        }
    }

    private readonly struct ColorSwitchSupportedGetCommand : ICommand
    {
        public ColorSwitchSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ColorSwitchSupportedGetCommand(frame);
        }
    }

    private readonly struct ColorSwitchSupportedReportCommand : ICommand
    {
        public ColorSwitchSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<ColorSwitchColorComponent> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Color Switch Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Color Switch Supported Report frame is too short");
            }

            var supportedComponents = new HashSet<ColorSwitchColorComponent>();

            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span.Slice(0, 2);
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        ColorSwitchColorComponent colorComponent = (ColorSwitchColorComponent)((byteNum << 3) + bitNum);
                        supportedComponents.Add(colorComponent);
                    }
                }
            }

            return supportedComponents;
        }
    }

    private readonly struct ColorSwitchGetCommand : ICommand
    {
        public ColorSwitchGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchGetCommand Create(ColorSwitchColorComponent colorComponent)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)colorComponent];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchGetCommand(frame);
        }
    }

    private readonly struct ColorSwitchReportCommand : ICommand
    {
        public ColorSwitchReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.Report;

        public CommandClassFrame Frame { get; }

        public static (ColorSwitchColorComponent ColorComponent, ColorSwitchColorComponentState State) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Color Switch Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Color Switch Report frame is too short");
            }

            ColorSwitchColorComponent colorComponent = (ColorSwitchColorComponent)frame.CommandParameters.Span[0];
            byte currentValue = frame.CommandParameters.Span[1];
            byte? targetValue = frame.CommandParameters.Length > 2
                ? frame.CommandParameters.Span[2]
                : null;
            DurationReport? duration = frame.CommandParameters.Length > 3
                ? frame.CommandParameters.Span[3]
                : null;
            ColorSwitchColorComponentState state = new ColorSwitchColorComponentState(currentValue, targetValue, duration);
            return (colorComponent, state);
        }
    }

    private readonly struct ColorSwitchSetCommand : ICommand
    {
        public ColorSwitchSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchSetCommand Create(
            byte version,
            IReadOnlyDictionary<ColorSwitchColorComponent, byte> values,
            DurationSet? duration)
{
            bool includeDuration = version >= 2 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[1 + (2 * values.Count) + (includeDuration ? 1 : 0)];
            commandParameters[0] = (byte)(values.Count & 0b0001_1111);

            int idx = 1;
            foreach (KeyValuePair<ColorSwitchColorComponent, byte> pair in values)
            {
                commandParameters[idx++] = (byte)pair.Key;
                commandParameters[idx++] = pair.Value;
            }

            if (includeDuration)
            {
                commandParameters[idx] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchSetCommand(frame);
        }
    }

    private readonly struct ColorSwitchStartLevelChangeCommand : ICommand
    {
        public ColorSwitchStartLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.StartLevelChange;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchStartLevelChangeCommand Create(
            byte version,
            ColorSwitchChangeDirection direction,
            ColorSwitchColorComponent colorComponent,
            byte? startLevel,
            DurationSet? duration)
        {
            bool includeDuration = version >= 3 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[3 + (includeDuration ? 1 : 0)];

            commandParameters[0] = (byte)((byte)direction << 6);
            if (!startLevel.HasValue)
            {
                // ignoreStartLevel bit
                commandParameters[0] |= 0b0010_0000;
            }

            commandParameters[1] = (byte)colorComponent;
            commandParameters[2] = startLevel.GetValueOrDefault();

            if (includeDuration)
            {
                commandParameters[3] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchStartLevelChangeCommand(frame);
        }
    }

    private readonly struct ColorSwitchStopLevelChangeCommand : ICommand
    {
        public ColorSwitchStopLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.StopLevelChange;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchStartLevelChangeCommand Create(ColorSwitchColorComponent colorComponent)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)colorComponent];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ColorSwitchStartLevelChangeCommand(frame);
        }
    }
}
