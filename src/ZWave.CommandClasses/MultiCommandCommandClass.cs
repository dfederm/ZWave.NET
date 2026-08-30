using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Multi Command Command Class commands (version 1).
/// </summary>
public enum MultiCommandCommand : byte
{
    /// <summary>
    /// Encapsulate multiple commands in a single frame.
    /// </summary>
    CommandEncapsulation = 0x01,
}

/// <summary>
/// Implements the Multi Command Command Class (version 1).
/// </summary>
/// <remarks>
/// Per the Transport-Encapsulation spec (SDS13783) §3.4, the Multi Command CC bundles multiple
/// command class commands into a single frame. The receiving node MUST process all encapsulated
/// commands in the order they are transmitted. Multi Command is the innermost encapsulation layer.
/// </remarks>
[CommandClass(CommandClassId.MultiCommand)]
public sealed partial class MultiCommandCommandClass : CommandClass<MultiCommandCommand>
{
    internal MultiCommandCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(MultiCommandCommand command)
        => command switch
        {
            MultiCommandCommand.CommandEncapsulation => true,
            _ => false,
        };

    /// <summary>
    /// Per spec §2, Multi Command is a Transport-Encapsulation CC.
    /// </summary>
    internal override CommandClassCategory Category => CommandClassCategory.Transport;

    /// <summary>
    /// Per spec §3.4, there is no mandatory node interview for this Command Class.
    /// </summary>
    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        // The Driver de-encapsulates Multi Command frames upstream (in the receive path) into their
        // constituent commands, so a Multi Command frame is not delivered to this instance as an
        // unsolicited report.
    }
}
