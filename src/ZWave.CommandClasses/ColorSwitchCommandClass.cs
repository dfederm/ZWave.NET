using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies a color component of a color switch device.
/// </summary>
public enum ColorSwitchColorComponent : byte
{
    /// <summary>
    /// Warm white color component.
    /// </summary>
    WarmWhite = 0x00,

    /// <summary>
    /// Cold white color component.
    /// </summary>
    ColdWhite = 0x01,

    /// <summary>
    /// Red color component.
    /// </summary>
    Red = 0x02,

    /// <summary>
    /// Green color component.
    /// </summary>
    Green = 0x03,

    /// <summary>
    /// Blue color component.
    /// </summary>
    Blue = 0x04,

    /// <summary>
    /// Amber color component (for 6-channel color mixing).
    /// </summary>
    Amber = 0x05,

    /// <summary>
    /// Cyan color component (for 6-channel color mixing).
    /// </summary>
    Cyan = 0x06,

    /// <summary>
    /// Purple color component (for 6-channel color mixing).
    /// </summary>
    Purple = 0x07,

    /// <summary>
    /// Indexed color. This value is obsoleted per the specification.
    /// </summary>
    Index = 0x08,
}

/// <summary>
/// The direction of a color switch level change.
/// </summary>
public enum ColorSwitchChangeDirection : byte
{
    /// <summary>
    /// The level change is increasing.
    /// </summary>
    Up = 0x00,

    /// <summary>
    /// The level change is decreasing.
    /// </summary>
    Down = 0x01,
}

/// <summary>
/// Defines the commands for the Color Switch Command Class.
/// </summary>
public enum ColorSwitchCommand : byte
{
    /// <summary>
    /// Request the supported color components of a device.
    /// </summary>
    SupportedGet = 0x01,

    /// <summary>
    /// Report the supported color components of a device.
    /// </summary>
    SupportedReport = 0x02,

    /// <summary>
    /// Request the status of a specified color component.
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
/// Controls color-capable devices by manipulating individual color components.
/// </summary>
[CommandClass(CommandClassId.ColorSwitch)]
public sealed partial class ColorSwitchCommandClass : CommandClass<ColorSwitchCommand>
{
    internal ColorSwitchCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

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

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        IReadOnlySet<ColorSwitchColorComponent> supportedColorComponents = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);

        foreach (ColorSwitchColorComponent colorComponent in supportedColorComponents)
        {
            _ = await GetAsync(colorComponent, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ColorSwitchCommand)frame.CommandId)
        {
            case ColorSwitchCommand.SupportedReport:
            {
                IReadOnlySet<ColorSwitchColorComponent> supportedComponents = ColorSwitchSupportedReportCommand.Parse(frame, Logger);
                ApplySupportedComponents(supportedComponents);
                break;
            }
            case ColorSwitchCommand.Report:
            {
                ColorSwitchReport report = ColorSwitchReportCommand.Parse(frame, Logger);
                _colorComponents[report.ColorComponent] = report;

                OnColorSwitchReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
