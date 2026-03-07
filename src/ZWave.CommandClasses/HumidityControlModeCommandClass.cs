using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The humidity control mode.
/// </summary>
public enum HumidityControlMode : byte
{
    /// <summary>
    /// The humidity control system is off.
    /// </summary>
    Off = 0x00,

    /// <summary>
    /// The system will attempt to raise humidity to the humidifier setpoint.
    /// </summary>
    Humidify = 0x01,

    /// <summary>
    /// The system will attempt to lower humidity to the de-humidifier setpoint.
    /// </summary>
    Dehumidify = 0x02,

    /// <summary>
    /// The system will automatically switch between humidifying and de-humidifying
    /// in order to satisfy the humidify and de-humidify setpoints.
    /// </summary>
    Auto = 0x03,
}

/// <summary>
/// Commands for the Humidity Control Mode Command Class.
/// </summary>
public enum HumidityControlModeCommand : byte
{
    /// <summary>
    /// Set the humidity control mode in the device.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current humidity control mode from the device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the current humidity control mode from the device.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported humidity control modes from the device.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Report the supported humidity control modes from the device.
    /// </summary>
    SupportedReport = 0x05,
}

/// <summary>
/// Implements the Humidity Control Mode Command Class (V1-2).
/// </summary>
[CommandClass(CommandClassId.HumidityControlMode)]
public sealed partial class HumidityControlModeCommandClass : CommandClass<HumidityControlModeCommand>
{
    internal HumidityControlModeCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(HumidityControlModeCommand command)
        => command switch
        {
            HumidityControlModeCommand.Set => true,
            HumidityControlModeCommand.Get => true,
            HumidityControlModeCommand.SupportedGet => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetSupportedModesAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((HumidityControlModeCommand)frame.CommandId)
        {
            case HumidityControlModeCommand.Report:
            {
                HumidityControlModeReport report = HumidityControlModeReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnModeReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
