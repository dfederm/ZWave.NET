using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands for the Barrier Operator Command Class.
/// </summary>
public enum BarrierOperatorCommand : byte
{
    /// <summary>
    /// Initiate an unattended change in state of the barrier.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current state of a barrier operator device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the status of the barrier operator device.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Query a device for available signaling subsystems which may be controlled via Z-Wave.
    /// </summary>
    SignalSupportedGet = 0x04,

    /// <summary>
    /// Report the signaling subsystems supported by the device.
    /// </summary>
    SignalSupportedReport = 0x05,

    /// <summary>
    /// Turn on or off an event signaling subsystem that is supported by the device.
    /// </summary>
    EventSignalSet = 0x06,

    /// <summary>
    /// Request the state of a signaling subsystem.
    /// </summary>
    EventSignalingGet = 0x07,

    /// <summary>
    /// Indicate the state of a notification subsystem of a Barrier Device.
    /// </summary>
    EventSignalingReport = 0x08,
}

/// <summary>
/// Represents the interpreted state of a barrier operator device.
/// </summary>
public enum BarrierOperatorState : byte
{
    /// <summary>
    /// The barrier is in the Closed position.
    /// </summary>
    Closed = 0x00,

    /// <summary>
    /// The barrier is closing. The current position is unknown.
    /// </summary>
    Closing = 0xFC,

    /// <summary>
    /// The barrier is stopped. The current position may or may not be known.
    /// </summary>
    Stopped = 0xFD,

    /// <summary>
    /// The barrier is opening. The current position is unknown.
    /// </summary>
    Opening = 0xFE,

    /// <summary>
    /// The barrier is in the Open position.
    /// </summary>
    Open = 0xFF,
}

/// <summary>
/// Represents the type of a signaling subsystem on a barrier operator device.
/// </summary>
public enum BarrierOperatorSignalingSubsystemType : byte
{
    /// <summary>
    /// The Barrier Device has an Audible Notification subsystem (e.g. Siren).
    /// </summary>
    AudibleNotification = 0x01,

    /// <summary>
    /// The Barrier Device has a Visual Notification subsystem (e.g. Flashing Light).
    /// </summary>
    VisualNotification = 0x02,
}

/// <summary>
/// The Barrier Operator Command Class is used to control and query the status of motorized barriers.
/// </summary>
[CommandClass(CommandClassId.BarrierOperator)]
public sealed partial class BarrierOperatorCommandClass : CommandClass<BarrierOperatorCommand>
{
    internal BarrierOperatorCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(BarrierOperatorCommand command)
        => command switch
        {
            BarrierOperatorCommand.Set => true,
            BarrierOperatorCommand.Get => true,
            BarrierOperatorCommand.SignalSupportedGet => true,
            BarrierOperatorCommand.EventSignalSet => true,
            BarrierOperatorCommand.EventSignalingGet => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<BarrierOperatorSignalingSubsystemType> supportedSubsystems =
            await GetSupportedSignalingSubsystemsAsync(cancellationToken).ConfigureAwait(false);

        foreach (BarrierOperatorSignalingSubsystemType subsystemType in supportedSubsystems)
        {
            _ = await GetEventSignalAsync(subsystemType, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((BarrierOperatorCommand)frame.CommandId)
        {
            case BarrierOperatorCommand.Report:
            {
                BarrierOperatorReport report = BarrierOperatorReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnBarrierOperatorReportReceived?.Invoke(report);
                break;
            }
            case BarrierOperatorCommand.EventSignalingReport:
            {
                BarrierOperatorEventSignalReport report = EventSignalingReportCommand.Parse(frame, Logger);
                _eventSignals[report.SubsystemType] = report;
                OnEventSignalReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
