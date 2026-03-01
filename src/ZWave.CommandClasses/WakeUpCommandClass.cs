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

[CommandClass(CommandClassId.WakeUp)]
public sealed partial class WakeUpCommandClass : CommandClass<WakeUpCommand>
{
    internal WakeUpCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Raised when a Wake Up Notification is received from the device, indicating it is awake.
    /// </summary>
    public event Action? OnWakeUpNotificationReceived;

    internal override CommandClassCategory Category => CommandClassCategory.Management;

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

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetIntervalAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(WakeUpCommand.IntervalCapabilitiesGet).GetValueOrDefault())
        {
            _ = await GetIntervalCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Notify the device that there is no more information to send and it may return to sleep.
    /// </summary>
    public async Task SendNoMoreInformationAsync(CancellationToken cancellationToken)
    {
        var command = WakeUpNoMoreInformationCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((WakeUpCommand)frame.CommandId)
        {
            case WakeUpCommand.IntervalReport:
            {
                WakeUpInterval report = WakeUpIntervalReportCommand.Parse(frame, Logger);
                LastInterval = report;
                OnIntervalReportReceived?.Invoke(report);
                break;
            }
            case WakeUpCommand.Notification:
            {
                OnWakeUpNotificationReceived?.Invoke();
                break;
            }
            case WakeUpCommand.IntervalCapabilitiesReport:
            {
                IntervalCapabilities = WakeUpIntervalCapabilitiesReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    internal readonly struct WakeUpNoMoreInformationCommand : ICommand
    {
        public WakeUpNoMoreInformationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.NoMoreInformation;

        public CommandClassFrame Frame { get; }

        public static WakeUpNoMoreInformationCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WakeUpNoMoreInformationCommand(frame);
        }
    }

    internal readonly struct WakeUpNotificationCommand : ICommand
    {
        public WakeUpNotificationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WakeUp;

        public static byte CommandId => (byte)WakeUpCommand.Notification;

        public CommandClassFrame Frame { get; }
    }
}
