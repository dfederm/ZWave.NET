using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies a window covering parameter.
/// </summary>
/// <remarks>
/// Even parameter IDs represent movement-only control (position unknown).
/// Odd parameter IDs represent position control (0x00–0x63).
/// If a device supports a position parameter (odd), it MUST NOT support the corresponding movement parameter (even).
/// </remarks>
public enum WindowCoveringParameterId : byte
{
    /// <summary>
    /// Outbound left edge, right/left movement (position unknown).
    /// </summary>
    OutboundLeftMovement = 0,

    /// <summary>
    /// Outbound left edge, right/left position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    OutboundLeftPosition = 1,

    /// <summary>
    /// Outbound right edge, right/left movement (position unknown).
    /// </summary>
    OutboundRightMovement = 2,

    /// <summary>
    /// Outbound right edge, right/left position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    OutboundRightPosition = 3,

    /// <summary>
    /// Inbound left edge, right/left movement (position unknown).
    /// </summary>
    InboundLeftMovement = 4,

    /// <summary>
    /// Inbound left edge, right/left position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    InboundLeftPosition = 5,

    /// <summary>
    /// Inbound right edge, right/left movement (position unknown).
    /// </summary>
    InboundRightMovement = 6,

    /// <summary>
    /// Inbound right edge, right/left position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    InboundRightPosition = 7,

    /// <summary>
    /// Inbound edges controlled horizontally as one, right/left movement (position unknown).
    /// </summary>
    InboundRightLeftMovement = 8,

    /// <summary>
    /// Inbound edges controlled horizontally as one, right/left position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    InboundRightLeftPosition = 9,

    /// <summary>
    /// Vertical slats angle, right/left movement (position unknown).
    /// </summary>
    VerticalSlatsAngleMovement = 10,

    /// <summary>
    /// Vertical slats angle, right/left position (0x00 = Closed right, 0x32 = Open, 0x63 = Closed left).
    /// </summary>
    VerticalSlatsAnglePosition = 11,

    /// <summary>
    /// Outbound bottom edge, up/down movement (position unknown).
    /// </summary>
    OutboundBottomMovement = 12,

    /// <summary>
    /// Outbound bottom edge, up/down position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    OutboundBottomPosition = 13,

    /// <summary>
    /// Outbound top edge, up/down movement (position unknown).
    /// </summary>
    OutboundTopMovement = 14,

    /// <summary>
    /// Outbound top edge, up/down position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    OutboundTopPosition = 15,

    /// <summary>
    /// Inbound bottom edge, up/down movement (position unknown).
    /// </summary>
    InboundBottomMovement = 16,

    /// <summary>
    /// Inbound bottom edge, up/down position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    InboundBottomPosition = 17,

    /// <summary>
    /// Inbound top edge, up/down movement (position unknown).
    /// </summary>
    InboundTopMovement = 18,

    /// <summary>
    /// Inbound top edge, up/down position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    InboundTopPosition = 19,

    /// <summary>
    /// Inbound edges controlled vertically as one, up/down movement (position unknown).
    /// </summary>
    InboundTopBottomMovement = 20,

    /// <summary>
    /// Inbound edges controlled vertically as one, up/down position (0x00 = Closed, 0x63 = Open).
    /// </summary>
    InboundTopBottomPosition = 21,

    /// <summary>
    /// Horizontal slats angle, up/down movement (position unknown).
    /// </summary>
    HorizontalSlatsAngleMovement = 22,

    /// <summary>
    /// Horizontal slats angle, up/down position (0x00 = Closed up, 0x32 = Open, 0x63 = Closed down).
    /// </summary>
    HorizontalSlatsAnglePosition = 23,
}

/// <summary>
/// The direction of a window covering level change.
/// </summary>
public enum WindowCoveringChangeDirection : byte
{
    /// <summary>
    /// The level change is increasing (opening).
    /// </summary>
    Up = 0x00,

    /// <summary>
    /// The level change is decreasing (closing).
    /// </summary>
    Down = 0x01,
}

/// <summary>
/// Defines the commands for the Window Covering Command Class.
/// </summary>
public enum WindowCoveringCommand : byte
{
    /// <summary>
    /// Request the supported properties of a device.
    /// </summary>
    SupportedGet = 0x01,

    /// <summary>
    /// Report the supported properties of a device.
    /// </summary>
    SupportedReport = 0x02,

    /// <summary>
    /// Request the status of a specified covering parameter.
    /// </summary>
    Get = 0x03,

    /// <summary>
    /// Report the status of a covering parameter.
    /// </summary>
    Report = 0x04,

    /// <summary>
    /// Set the value of one or more covering parameters.
    /// </summary>
    Set = 0x05,

    /// <summary>
    /// Initiate a transition of one parameter to a new level.
    /// </summary>
    StartLevelChange = 0x06,

    /// <summary>
    /// Stop an ongoing transition.
    /// </summary>
    StopLevelChange = 0x07,
}

/// <summary>
/// Controls window covering devices by manipulating covering parameters such as position and slats angle.
/// </summary>
[CommandClass(CommandClassId.WindowCovering)]
public sealed partial class WindowCoveringCommandClass : CommandClass<WindowCoveringCommand>
{
    internal WindowCoveringCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(WindowCoveringCommand command)
        => command switch
        {
            WindowCoveringCommand.SupportedGet => true,
            WindowCoveringCommand.Get => true,
            WindowCoveringCommand.Set => true,
            WindowCoveringCommand.StartLevelChange => true,
            WindowCoveringCommand.StopLevelChange => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        IReadOnlySet<WindowCoveringParameterId> supportedParameters = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

        foreach (WindowCoveringParameterId parameterId in supportedParameters)
        {
            _ = await GetAsync(parameterId, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((WindowCoveringCommand)frame.CommandId)
        {
            case WindowCoveringCommand.SupportedReport:
            {
                IReadOnlySet<WindowCoveringParameterId> supportedParameters = WindowCoveringSupportedReportCommand.Parse(frame, Logger);
                ApplySupportedParameters(supportedParameters);
                break;
            }
            case WindowCoveringCommand.Report:
            {
                WindowCoveringReport report = WindowCoveringReportCommand.Parse(frame, Logger);
                _parameterValues[report.ParameterId] = report;

                OnWindowCoveringReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
