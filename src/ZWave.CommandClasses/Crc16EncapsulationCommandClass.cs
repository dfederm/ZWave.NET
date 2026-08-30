using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// CRC-16 Encapsulation Command Class commands (version 1).
/// </summary>
public enum Crc16EncapsulationCommand : byte
{
    /// <summary>
    /// Encapsulate a command with a CRC-16 checksum.
    /// </summary>
    CommandEncapsulation = 0x01,
}

/// <summary>
/// Implements the CRC-16 Encapsulation Command Class (version 1).
/// </summary>
/// <remarks>
/// Per the Transport-Encapsulation spec (SDS13783) §3.1, the CRC-16 Encapsulation CC is
/// [DEPRECATED] but some device types are still required to support it, so it is implemented
/// for receive compatibility.
/// A CRC-16 frame is the outermost encapsulation layer and MUST NOT be encapsulated by any
/// other Command Class (spec §3.1.1.2).
/// </remarks>
[CommandClass(CommandClassId.Crc16Encapsulation)]
public sealed partial class Crc16EncapsulationCommandClass : CommandClass<Crc16EncapsulationCommand>
{
    internal Crc16EncapsulationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(Crc16EncapsulationCommand command)
        => command switch
        {
            Crc16EncapsulationCommand.CommandEncapsulation => true,
            _ => false,
        };

    /// <summary>
    /// Per spec §2, CRC-16 Encapsulation is a Transport-Encapsulation CC.
    /// </summary>
    internal override CommandClassCategory Category => CommandClassCategory.Transport;

    /// <summary>
    /// Per spec §3.1, there is no mandatory node interview for this Command Class.
    /// </summary>
    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        // The Driver de-encapsulates CRC-16 frames upstream (in the receive path), so a
        // Command Encapsulation frame is not delivered to this instance as an unsolicited report.
    }
}
