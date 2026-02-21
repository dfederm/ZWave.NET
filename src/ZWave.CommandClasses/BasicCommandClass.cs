using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public enum BasicCommand : byte
{
    /// <summary>
    /// Set a value in a supporting device
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the status of a supporting device
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the status of the primary functionality of the device.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents a Basic Report received from a device.
/// </summary>
public readonly record struct BasicReport(
    /// <summary>
    /// The current value of the device hardware
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

[CommandClass(CommandClassId.Basic)]
public sealed class BasicCommandClass : CommandClass<BasicCommand>
{
    internal BasicCommandClass(
        CommandClassInfo info,
        IDriver driver,
        INode node,
        ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public BasicReport? LastReport { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(BasicCommand command)
        => command switch
        {
            BasicCommand.Set => true,
            BasicCommand.Get => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the status of a supporting device
    /// </summary>
    public async Task<BasicReport> GetAsync(CancellationToken cancellationToken)
    {
        BasicGetCommand command = BasicGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<BasicReportCommand>(cancellationToken).ConfigureAwait(false);
        BasicReport report = BasicReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        return report;
    }

    /// <summary>
    /// Set a value in a supporting device
    /// </summary>
    public async Task SetAsync(GenericValue targetValue, CancellationToken cancellationToken)
    {
        var command = BasicSetCommand.Create(targetValue);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((BasicCommand)frame.CommandId)
        {
            case BasicCommand.Set:
            case BasicCommand.Get:
            {
                break;
            }
            case BasicCommand.Report:
            {
                LastReport = BasicReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct BasicSetCommand : ICommand
    {
        public BasicSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BasicCommand.Set;

        public CommandClassFrame Frame { get; }

        public GenericValue Value => Frame.CommandParameters.Span[0];

        public static BasicSetCommand Create(GenericValue value)
        {
            ReadOnlySpan<byte> commandParameters = [value.Value];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BasicSetCommand(frame);
        }
    }

    private readonly struct BasicGetCommand : ICommand
    {
        public BasicGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BasicCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BasicGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BasicGetCommand(frame);
        }
    }

    private readonly struct BasicReportCommand : ICommand
    {
        public BasicReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BasicCommand.Report;

        public CommandClassFrame Frame { get; }

        public static BasicReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Basic Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Basic Report frame is too short");
            }

            GenericValue currentValue = frame.CommandParameters.Span[0];
            GenericValue? targetValue = frame.CommandParameters.Length > 1
                ? frame.CommandParameters.Span[1]
                : null;
            DurationReport? duration = frame.CommandParameters.Length > 2
                ? frame.CommandParameters.Span[2]
                : null;
            return new BasicReport(currentValue, targetValue, duration);
        }
    }
}
