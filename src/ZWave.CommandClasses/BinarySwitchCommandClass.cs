using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public enum BinarySwitchCommand : byte
{
    /// <summary>
    /// Set the On/Off state at the receiving node.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current On/Off state from a node.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current On/Off state at the sending node.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents a Binary Switch Report received from a device.
/// </summary>
public readonly record struct BinarySwitchReport(
    /// <summary>
    /// The current On/Off state at the sending node.
    /// </summary>
    GenericValue CurrentValue,

    /// <summary>
    /// The target value of an ongoing transition or the most recent transition.
    /// </summary>
    GenericValue? TargetValue,

    /// <summary>
    /// The duration of a transition from the Current Value to the Target Value.
    /// </summary>
    DurationReport? Duration);

[CommandClass(CommandClassId.BinarySwitch)]
public sealed class BinarySwitchCommandClass : CommandClass<BinarySwitchCommand>
{
    internal BinarySwitchCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public BinarySwitchReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Binary Switch Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<BinarySwitchReport>? OnBinarySwitchReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(BinarySwitchCommand command)
        => command switch
        {
            BinarySwitchCommand.Set => true,
            BinarySwitchCommand.Get => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current On/Off state from a node.
    /// </summary>
    public async Task<BinarySwitchReport> GetAsync(CancellationToken cancellationToken)
    {
        BinarySwitchGetCommand command = BinarySwitchGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BinarySwitchReportCommand>(cancellationToken).ConfigureAwait(false);
        BinarySwitchReport report = BinarySwitchReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnBinarySwitchReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the On/Off state at the receiving node.
    /// </summary>
    public async Task SetAsync(
        bool targetValue,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = BinarySwitchSetCommand.Create(EffectiveVersion, targetValue, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((BinarySwitchCommand)frame.CommandId)
        {
            case BinarySwitchCommand.Report:
            {
                BinarySwitchReport report = BinarySwitchReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnBinarySwitchReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct BinarySwitchSetCommand : ICommand
    {
        public BinarySwitchSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySwitch;

        public static byte CommandId => (byte)BinarySwitchCommand.Set;

        public CommandClassFrame Frame { get; }

        public static BinarySwitchSetCommand Create(byte version, bool value, DurationSet? duration)
        {
            bool includeDuration = version >= 2 && duration.HasValue;
            Span<byte> commandParameters = stackalloc byte[1 + (includeDuration ? 1 : 0)];
            commandParameters[0] = value ? (byte)0xff : (byte)0x00;
            if (includeDuration)
            {
                commandParameters[1] = duration!.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BinarySwitchSetCommand(frame);
        }
    }

    internal readonly struct BinarySwitchGetCommand : ICommand
    {
        public BinarySwitchGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySwitch;

        public static byte CommandId => (byte)BinarySwitchCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BinarySwitchGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BinarySwitchGetCommand(frame);
        }
    }

    internal readonly struct BinarySwitchReportCommand : ICommand
    {
        public BinarySwitchReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BinarySwitch;

        public static byte CommandId => (byte)BinarySwitchCommand.Report;

        public CommandClassFrame Frame { get; }

        public static BinarySwitchReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Binary Switch Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Binary Switch Report frame is too short");
            }

            GenericValue currentValue = frame.CommandParameters.Span[0];
            GenericValue? targetValue = frame.CommandParameters.Length > 1
                ? frame.CommandParameters.Span[1]
                : null;
            DurationReport? duration = frame.CommandParameters.Length > 2
                ? frame.CommandParameters.Span[2]
                : null;
            return new BinarySwitchReport(currentValue, targetValue, duration);
        }
    }
}
