using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The direction of a multilevel switch level change.
/// </summary>
public enum MultilevelSwitchChangeDirection : byte
{
    /// <summary>
    /// The level change is increasing.
    /// </summary>
    Up = 0x00,

    /// <summary>
    /// The level change is decreasing.
    /// </summary>
    Down = 0x01,

    // 0x02 is Reserved

    /// <summary>
    /// No primary switch level change (V3+). Maintains the current level for the Primary Switch Type.
    /// </summary>
    NoUpDownMotion = 0x03,
}

/// <summary>
/// Identifies the type of a multilevel switch per Table 2.425 of the Z-Wave Application Specification.
/// </summary>
public enum MultilevelSwitchType : byte
{
    /// <summary>
    /// Undefined / Not supported. Only valid as a Secondary Switch Type. Obsoleted for Primary Switch Type.
    /// </summary>
    NotSupported = 0x00,

    /// <summary>
    /// Off (0x00) / On (0xFF).
    /// </summary>
    OffOn = 0x01,

    /// <summary>
    /// Down (0x00) / Up (0xFF).
    /// </summary>
    DownUp = 0x02,

    /// <summary>
    /// Close (0x00) / Open (0xFF).
    /// </summary>
    CloseOpen = 0x03,

    /// <summary>
    /// Counter-Clockwise (0x00) / Clockwise (0xFF).
    /// </summary>
    CounterClockwiseClockwise = 0x04,

    /// <summary>
    /// Left (0x00) / Right (0xFF).
    /// </summary>
    LeftRight = 0x05,

    /// <summary>
    /// Reverse (0x00) / Forward (0xFF).
    /// </summary>
    ReverseForward = 0x06,

    /// <summary>
    /// Pull (0x00) / Push (0xFF).
    /// </summary>
    PullPush = 0x07,
}

/// <summary>
/// Defines the commands for the Multilevel Switch Command Class.
/// </summary>
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
    /// Advertise the status of a multilevel device.
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
    /// Request the supported Switch Types of a supporting device (V3+).
    /// </summary>
    SupportedGet = 0x06,

    /// <summary>
    /// Advertise the supported Switch Types implemented by a supporting device (V3+).
    /// </summary>
    SupportedReport = 0x07,
}

/// <summary>
/// Controls devices with multilevel capability (e.g. dimmers, motor controllers).
/// </summary>
/// <remarks>
/// The Secondary Switch Type functionality is deprecated per the specification and is not implemented.
/// </remarks>
[CommandClass(CommandClassId.MultilevelSwitch)]
public sealed partial class MultilevelSwitchCommandClass : CommandClass<MultilevelSwitchCommand>
{
    internal MultilevelSwitchCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

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

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((MultilevelSwitchCommand)frame.CommandId)
        {
            case MultilevelSwitchCommand.Report:
            {
                MultilevelSwitchReport report = MultilevelSwitchReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnMultilevelSwitchReportReceived?.Invoke(report);
                break;
            }
            case MultilevelSwitchCommand.SupportedReport:
            {
                SwitchType = MultilevelSwitchSupportedReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }
}
