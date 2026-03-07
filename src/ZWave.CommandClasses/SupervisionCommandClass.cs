using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Supervision Command Class commands (version 2).
/// </summary>
public enum SupervisionCommand : byte
{
    /// <summary>
    /// Initiate the execution of a command and request immediate and future status.
    /// </summary>
    Get = 0x01,

    /// <summary>
    /// Advertise the status of one or more command processes.
    /// </summary>
    Report = 0x02,
}

/// <summary>
/// Status identifiers for the Supervision Report Command (spec Table 4.35).
/// </summary>
public enum SupervisionStatus : byte
{
    /// <summary>
    /// The command is not supported by the receiver.
    /// </summary>
    NoSupport = 0x00,

    /// <summary>
    /// The command was accepted and processing has started.
    /// A non-zero Duration value is advertised.
    /// </summary>
    Working = 0x01,

    /// <summary>
    /// The command was accepted but processing failed.
    /// </summary>
    Fail = 0x02,

    /// <summary>
    /// The requested command has been completed successfully.
    /// </summary>
    Success = 0xFF,
}

/// <summary>
/// Implements the Supervision Command Class (version 2).
/// </summary>
/// <remarks>
/// The Supervision CC provides application-level delivery confirmation for Set-type
/// and unsolicited Report commands. Per spec §4.2.8, a Supervision Get wraps a command
/// and the receiver responds with a Supervision Report indicating the operation status.
///
/// Per spec §4.1.3.5, the encapsulation order is:
/// payload → Multi Command → Supervision → Multi Channel → Security/CRC-16/Transport Service
/// </remarks>
[CommandClass(CommandClassId.Supervision)]
public sealed partial class SupervisionCommandClass : CommandClass<SupervisionCommand>
{
    internal SupervisionCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(SupervisionCommand command)
        => command switch
        {
            SupervisionCommand.Get => true,
            SupervisionCommand.Report => true,
            _ => false,
        };

    /// <summary>
    /// Per spec §6.4.5, Supervision is a Transport CC.
    /// </summary>
    internal override CommandClassCategory Category => CommandClassCategory.Transport;

    /// <summary>
    /// Per spec §6.4.5.1: "There is no mandatory node interview for a node controlling this Command Class."
    /// </summary>
    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((SupervisionCommand)frame.CommandId)
        {
            case SupervisionCommand.Report:
            {
                SupervisionReport report = SupervisionReportCommand.Parse(frame, Logger);
                OnSupervisionReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
