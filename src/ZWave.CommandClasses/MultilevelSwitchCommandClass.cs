using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The direction of a multilevel switch level change.
/// </summary>
public enum MultilevelSwitchChangeDirection : byte
{
    Up = 0x00,

    Down = 0x01,

    // 0x02 is Reserved

    // Secondary switch is obsolete, so no need to support this.
    // None = 0x03,
}

/// <summary>
/// Identifies the type of a multilevel switch.
/// </summary>
public enum MultilevelSwitchType : byte
{
    NotSupported = 0x00,

    UpDown = 0x01,

    DownUp = 0x02,

    CloseOpen = 0x03,

    CounterClockwiseClockwise = 0x04,

    LeftRight = 0x05,

    ReverseForward = 0x06,

    PullPush = 0x07,
}

public enum MultilevelSwitchCommand : byte
{
    /// <summary>
    /// Set a multilevel value in a supporting device.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the status of a multilevel device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the status of a multilevel device
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Initiate a transition to a new level.
    /// </summary>
    StartLevelChange = 0x04,

    /// <summary>
    /// Stop an ongoing transition.
    /// </summary>
    StopLevelChange = 0x05,

    /// <summary>
    /// Request the supported Switch Types of a supporting device
    /// </summary>
    SupportedGet = 0x06,

    /// <summary>
    /// Advertise the supported Switch Types implemented by a supporting device
    /// </summary>
    SupportedReport = 0x07,
}

/// <summary>
/// Represents a Multilevel Switch Report received from a device.
/// </summary>
public readonly record struct MultilevelSwitchReport(
    /// <summary>
    /// The current value at the sending node
    /// </summary>
    GenericValue CurrentValue,

    /// <summary>
    /// The target value of an ongoing transition or the most recent transition.
    /// </summary>
    GenericValue? TargetValue,

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate.
    /// </summary>
    DurationReport? Duration);

// Note: We are not implementing the secondary switch at all since it's deprecated.
[CommandClass(CommandClassId.MultilevelSwitch)]
public sealed class MultilevelSwitchCommandClass : CommandClass<MultilevelSwitchCommand>
{
    public MultilevelSwitchCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public MultilevelSwitchReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the switch type.
    /// </summary>
    public MultilevelSwitchType? SwitchType { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(MultilevelSwitchCommand command)
        => command switch
        {
            MultilevelSwitchCommand.Set => true,
            MultilevelSwitchCommand.Get => true,
            MultilevelSwitchCommand.StartLevelChange => true,
            MultilevelSwitchCommand.StopLevelChange => true,
            MultilevelSwitchCommand.SupportedGet => Version.HasValue ? Version >= 3 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(MultilevelSwitchCommand.SupportedGet).GetValueOrDefault())
        {
            _ = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Set a multilevel value in a supporting device.
    /// </summary>
    public async Task SetAsync(
        GenericValue value,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchSetCommand.Create(EffectiveVersion, value, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the status of a multilevel device.
    /// </summary>
    public async Task<MultilevelSwitchReport> GetAsync(CancellationToken cancellationToken)
    {
        MultilevelSwitchGetCommand command = MultilevelSwitchGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSwitchReportCommand>(cancellationToken).ConfigureAwait(false);
        MultilevelSwitchReport report = MultilevelSwitchReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        return report;
    }

    /// <summary>
    /// Initiate a transition to a new level.
    /// </summary>
    public async Task StartLevelChangeAsync(
        MultilevelSwitchChangeDirection direction,
        GenericValue? startLevel,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchStartLevelChangeCommand.Create(
            EffectiveVersion,
            direction,
            startLevel,
            duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop an ongoing transition.
    /// </summary>
    public async Task StopLevelChangeAsync(CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchStopLevelChangeCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the supported Switch Types of a supporting device
    /// </summary>
    public async Task<MultilevelSwitchType> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSwitchSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        MultilevelSwitchType switchType = MultilevelSwitchSupportedReportCommand.Parse(reportFrame, Logger);
        SwitchType = switchType;
        return switchType;
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((MultilevelSwitchCommand)frame.CommandId)
        {
            case MultilevelSwitchCommand.Set:
            case MultilevelSwitchCommand.Get:
            case MultilevelSwitchCommand.StartLevelChange:
            case MultilevelSwitchCommand.StopLevelChange:
            case MultilevelSwitchCommand.SupportedGet:
            {
                break;
            }
            case MultilevelSwitchCommand.Report:
            {
                LastReport = MultilevelSwitchReportCommand.Parse(frame, Logger);
                break;
            }
            case MultilevelSwitchCommand.SupportedReport:
            {
                SwitchType = MultilevelSwitchSupportedReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct MultilevelSwitchSetCommand : ICommand
    {
        public MultilevelSwitchSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.Set;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchSetCommand Create(byte version, GenericValue value, DurationSet? duration)
        {
            bool includeDuration = version >= 2 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[1 + (includeDuration ? 1 : 0)];
            commandParameters[0] = value.Value;
            if (includeDuration)
            {
                commandParameters[1] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultilevelSwitchSetCommand(frame);
        }
    }

    private readonly struct MultilevelSwitchGetCommand : ICommand
    {
        public MultilevelSwitchGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.Get;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultilevelSwitchGetCommand(frame);
        }
    }

    private readonly struct MultilevelSwitchReportCommand : ICommand
    {
        public MultilevelSwitchReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.Report;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Multilevel Switch Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multilevel Switch Report frame is too short");
            }

            GenericValue currentValue = frame.CommandParameters.Span[0];
            GenericValue? targetValue = frame.CommandParameters.Length > 1
                ? frame.CommandParameters.Span[1]
                : null;
            DurationReport? duration = frame.CommandParameters.Length > 2
                ? frame.CommandParameters.Span[2]
                : null;
            return new MultilevelSwitchReport(currentValue, targetValue, duration);
        }
    }

    private readonly struct MultilevelSwitchStartLevelChangeCommand : ICommand
    {
        public MultilevelSwitchStartLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.StartLevelChange;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchStartLevelChangeCommand Create(
            byte version,
            MultilevelSwitchChangeDirection direction,
            GenericValue? startLevel,
            DurationSet? duration)
        {
            bool includeDuration = version >= 2 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[2 + (includeDuration ? 1 : 0)];

            commandParameters[0] = (byte)((byte)direction << 6);
            if (!startLevel.HasValue)
            {
                // ignoreStartLevel bit
                commandParameters[0] |= 0b0010_0000;
            }

            commandParameters[1] = startLevel.GetValueOrDefault().Value;

            if (includeDuration)
            {
                commandParameters[2] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new MultilevelSwitchStartLevelChangeCommand(frame);
        }
    }

    private readonly struct MultilevelSwitchStopLevelChangeCommand : ICommand
    {
        public MultilevelSwitchStopLevelChangeCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.StopLevelChange;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchStartLevelChangeCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultilevelSwitchStartLevelChangeCommand(frame);
        }
    }

    private readonly struct MultilevelSwitchSupportedGetCommand : ICommand
    {
        public MultilevelSwitchSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultilevelSwitchSupportedGetCommand(frame);
        }
    }

    private readonly struct MultilevelSwitchSupportedReportCommand : ICommand
    {
        public MultilevelSwitchSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchType Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Multilevel Switch Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multilevel Switch Supported Report frame is too short");
            }

            return (MultilevelSwitchType)(frame.CommandParameters.Span[0] & 0b0001_1111);
        }
    }
}
