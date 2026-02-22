using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public enum WakeUpCommand : byte
{
    /// <summary>
    /// Configure the Wake Up interval and destination of a node.
    /// </summary>
    IntervalSet = 0x04,

    /// <summary>
    /// Request the Wake Up Interval and destination of a node.
    /// </summary>
    IntervalGet = 0x05,

    /// <summary>
    /// Advertise the current Wake Up interval and destination.
    /// </summary>
    IntervalReport = 0x06,

    /// <summary>
    /// Indicates that a node is awake.
    /// </summary>
    Notification = 0x07,

    /// <summary>
    /// Notify a supporting node that it may return to sleep to minimize power consumption.
    /// </summary>
    NoMoreInformation = 0x08,

    /// <summary>
    /// Request the Wake Up Interval capabilities of a node.
    /// </summary>
    IntervalCapabilitiesGet = 0x09,

    /// <summary>
    /// Advertise the Wake Up Interval capabilities of a node.
    /// </summary>
    IntervalCapabilitiesReport = 0x0a,
}

public readonly record struct WakeUpInterval(
    /// <summary>
    /// The time in seconds between Wake Up periods at the sending node
    /// </summary>
    uint WakeupIntervalInSeconds,

    /// <summary>
    /// The Wake Up destination NodeID configured at the sending node
    /// </summary>
    uint WakeupDestinationNodeId);

/// <summary>
/// Represents the wake up interval capabilities of a device.
/// </summary>
public readonly record struct WakeUpIntervalCapabilities(
    /// <summary>
    /// The minimum Wake Up Interval supported by the sending node
    /// </summary>
    uint MinimumWakeupIntervalInSeconds,

    /// <summary>
    /// The maximum Wake Up Interval supported by the sending node
    /// </summary>
    uint MaximumWakeupIntervalInSeconds,

    /// <summary>
    /// The default Wake Up Interval for the sending node
    /// </summary>
    uint DefaultWakeupIntervalInSeconds,

    /// <summary>
    /// The resolution of valid Wake Up Intervals values for the sending node.
    /// </summary>
    uint WakeupIntervalStepInSeconds,

    /// <summary>
    /// Whther the supporting node supports the Wake Up On Demand functionality
    /// </summary>
    bool? SupportsWakeUpOnDemand);

[CommandClass(CommandClassId.WakeUp)]
public sealed class WakeUpCommandClass : CommandClass<WakeUpCommand>
{
    public WakeUpCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    /// <summary>
    /// Gets the current wake up interval configuration.
    /// </summary>
    public WakeUpInterval? LastInterval { get; private set; }

    /// <summary>
    /// Gets the wake up interval capabilities.
    /// </summary>
    public WakeUpIntervalCapabilities? IntervalCapabilities { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(WakeUpCommand command)
        => command switch
        {
            WakeUpCommand.IntervalGet => true,
            WakeUpCommand.IntervalSet => true,
            WakeUpCommand.NoMoreInformation => true,
            WakeUpCommand.IntervalCapabilitiesGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    /// <summary>
    /// Request the Wake Up Interval and destination of a node.
    /// </summary>
    public async Task<WakeUpInterval> GetIntervalAsync(CancellationToken cancellationToken)
    {
        WakeUpIntervalGetCommand command = WakeUpIntervalGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<WakeUpIntervalReportCommand>(cancellationToken).ConfigureAwait(false);
        WakeUpInterval interval = WakeUpIntervalReportCommand.Parse(reportFrame, Logger);
        LastInterval = interval;
        return interval;
    }

    /// <summary>
    /// Configure the Wake Up interval and destination of a node.
    /// </summary>
    public async Task SetIntervalAsync(uint wakeupIntervalInSeconds, ushort wakeupDestinationNodeId, CancellationToken cancellationToken)
    {
        var command = WakeUpIntervalSetCommand.Create(wakeupIntervalInSeconds, wakeupDestinationNodeId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the wake up interval capabilities from the device.
    /// </summary>
    public async Task<WakeUpIntervalCapabilities> GetIntervalCapabilitiesAsync(CancellationToken cancellationToken)
    {
        WakeUpIntervalCapabilitiesGetCommand command = WakeUpIntervalCapabilitiesGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<WakeUpIntervalCapabilitiesReportCommand>(cancellationToken).ConfigureAwait(false);
        WakeUpIntervalCapabilities capabilities = WakeUpIntervalCapabilitiesReportCommand.Parse(reportFrame, Logger);
        IntervalCapabilities = capabilities;
        return capabilities;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetIntervalAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(WakeUpCommand.IntervalCapabilitiesGet).GetValueOrDefault())
        {
            _ = await GetIntervalCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((WakeUpCommand)frame.CommandId)
        {
            case WakeUpCommand.IntervalGet:
            case WakeUpCommand.IntervalSet:
            case WakeUpCommand.NoMoreInformation:
            case WakeUpCommand.IntervalCapabilitiesGet:
            {
                break;
            }
            case WakeUpCommand.IntervalReport:
            {
                LastInterval = WakeUpIntervalReportCommand.Parse(frame, Logger);
                break;
            }
            case WakeUpCommand.Notification:
            {
                // TODO: Manage node asleep/awake
                break;
            }
            case WakeUpCommand.IntervalCapabilitiesReport:
            {
                IntervalCapabilities = WakeUpIntervalCapabilitiesReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct WakeUpIntervalSetCommand : ICommand
    {
        public WakeUpIntervalSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalSet;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalSetCommand Create(uint wakeupIntervalInSeconds, ushort wakeupDestinationNodeId)
        {
            Span<byte> commandParameters = stackalloc byte[4];

            // The parameter is a 24-bit value, which .NET doesn't have built-in types for. So use a uint (32-bit),
            // convert to bytes, and ignore byte 0 (since this is a big-endian value)
            const int int24MaxValue = (1 << 24) - 1;
            if (wakeupIntervalInSeconds > int24MaxValue)
            {
                throw new ArgumentException($"Value must not be greater than {int24MaxValue}", nameof(wakeupIntervalInSeconds));
            }

            Span<byte> secondsBytes = stackalloc byte[4];
            wakeupIntervalInSeconds.WriteBytesBE(secondsBytes);
            secondsBytes[1..].CopyTo(commandParameters);

            commandParameters[3] = (byte)wakeupDestinationNodeId;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new WakeUpIntervalSetCommand(frame);
        }
    }

    private readonly struct WakeUpIntervalGetCommand : ICommand
    {
        public WakeUpIntervalGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalGet;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WakeUpIntervalGetCommand(frame);
        }
    }

    private readonly struct WakeUpIntervalReportCommand : ICommand
    {
        public WakeUpIntervalReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalReport;

        public CommandClassFrame Frame { get; }

        public static WakeUpInterval Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Wake Up Interval Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Wake Up Interval Report frame is too short");
            }

            uint wakeupIntervalInSeconds = frame.CommandParameters.Span[0..3].ToUInt32BE();
            uint wakeupDestinationNodeId = frame.CommandParameters.Span[3];
            return new WakeUpInterval(wakeupIntervalInSeconds, wakeupDestinationNodeId);
        }
    }

    private readonly struct WakeUpNotificationCommand : ICommand
    {
        public WakeUpNotificationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.Notification;

        public CommandClassFrame Frame { get; }
    }

    private readonly struct WakeUpNoMoreInformationCommand : ICommand
    {
        public WakeUpNoMoreInformationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.NoMoreInformation;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WakeUpIntervalGetCommand(frame);
        }
    }

    private readonly struct WakeUpIntervalCapabilitiesGetCommand : ICommand
    {
        public WakeUpIntervalCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalCapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalCapabilitiesGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WakeUpIntervalCapabilitiesGetCommand(frame);
        }
    }

    private readonly struct WakeUpIntervalCapabilitiesReportCommand: ICommand
    {
        public WakeUpIntervalCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.IntervalCapabilitiesReport;

        public CommandClassFrame Frame { get; }

        public static WakeUpIntervalCapabilities Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 12)
            {
                logger.LogWarning("Wake Up Interval Capabilities Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Wake Up Interval Capabilities Report frame is too short");
            }

            uint minimumWakeupIntervalInSeconds = frame.CommandParameters.Span[0..3].ToUInt32BE();
            uint maximumWakeupIntervalInSeconds = frame.CommandParameters.Span[3..6].ToUInt32BE();
            uint defaultWakeupIntervalInSeconds = frame.CommandParameters.Span[6..9].ToUInt32BE();
            uint wakeupIntervalStepInSeconds = frame.CommandParameters.Span[9..12].ToUInt32BE();
            bool? supportsWakeUpOnDemand = frame.CommandParameters.Length > 12
                ? frame.CommandParameters.Span[12] == 1
                : null;
            return new WakeUpIntervalCapabilities(
                minimumWakeupIntervalInSeconds,
                maximumWakeupIntervalInSeconds,
                defaultWakeupIntervalInSeconds,
                wakeupIntervalStepInSeconds,
                supportsWakeUpOnDemand);
        }
    }
}
