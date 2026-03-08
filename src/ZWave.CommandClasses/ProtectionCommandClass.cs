using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// The state of local (physical) protection.
/// </summary>
public enum LocalProtectionState : byte
{
    /// <summary>
    /// The device is not protected and may be operated normally via its user interface.
    /// </summary>
    Unprotected = 0x00,

    /// <summary>
    /// The device requires a complicated action sequence to operate via its user interface.
    /// </summary>
    ProtectionBySequence = 0x01,

    /// <summary>
    /// The device cannot be operated via its user interface.
    /// </summary>
    NoOperationPossible = 0x02,
}

/// <summary>
/// The state of RF (wireless) protection.
/// </summary>
public enum RfProtectionState : byte
{
    /// <summary>
    /// The device accepts and responds to all RF commands.
    /// </summary>
    Unprotected = 0x00,

    /// <summary>
    /// The device ignores runtime RF commands but still responds to status requests.
    /// </summary>
    NoRfControl = 0x01,

    /// <summary>
    /// The device does not respond to any RF commands at all.
    /// </summary>
    NoRfResponse = 0x02,
}

/// <summary>
/// Defines the commands for the Protection Command Class.
/// </summary>
public enum ProtectionCommand : byte
{
    /// <summary>
    /// Set the protection state of a device.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the protection state of a device.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the protection state of a device.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the protection capabilities of a device.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Report the protection capabilities of a device.
    /// </summary>
    SupportedReport = 0x05,

    /// <summary>
    /// Set the exclusive control node for a device.
    /// </summary>
    ExclusiveControlSet = 0x06,

    /// <summary>
    /// Request the exclusive control node for a device.
    /// </summary>
    ExclusiveControlGet = 0x07,

    /// <summary>
    /// Report the exclusive control node for a device.
    /// </summary>
    ExclusiveControlReport = 0x08,

    /// <summary>
    /// Set the RF protection timeout for a device.
    /// </summary>
    TimeoutSet = 0x09,

    /// <summary>
    /// Request the RF protection timeout for a device.
    /// </summary>
    TimeoutGet = 0x0A,

    /// <summary>
    /// Report the RF protection timeout for a device.
    /// </summary>
    TimeoutReport = 0x0B,
}

/// <summary>
/// Prevents unintentional control of a device by disabling its local user interface
/// and/or RF command acceptance.
/// </summary>
/// <remarks>
/// Control via Z-Wave is always possible independently of the local protection state.
/// This Command Class is intended for convenience applications and SHOULD NOT be used
/// for safety-critical applications.
/// </remarks>
[CommandClass(CommandClassId.Protection)]
public sealed partial class ProtectionCommandClass : CommandClass<ProtectionCommand>
{
    internal ProtectionCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(ProtectionCommand command)
        => command switch
        {
            ProtectionCommand.Set => true,
            ProtectionCommand.Get => true,
            ProtectionCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.ExclusiveControlSet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.ExclusiveControlGet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.TimeoutSet => Version.HasValue ? Version >= 2 : null,
            ProtectionCommand.TimeoutGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(ProtectionCommand.SupportedGet).GetValueOrDefault())
        {
            _ = await GetSupportedAsync(cancellationToken).ConfigureAwait(false);
        }

        _ = await GetAsync(cancellationToken).ConfigureAwait(false);

        if (SupportedReport?.SupportsExclusiveControl == true)
        {
            _ = await GetExclusiveControlAsync(cancellationToken).ConfigureAwait(false);
        }

        if (SupportedReport?.SupportsTimeout == true)
        {
            _ = await GetTimeoutAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ProtectionCommand)frame.CommandId)
        {
            case ProtectionCommand.Report:
            {
                ProtectionReport report = ProtectionReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnProtectionReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
