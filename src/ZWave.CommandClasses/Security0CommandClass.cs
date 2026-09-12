using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Security 0 Command Class commands (version 1).
/// </summary>
public enum Security0Command : byte
{
    CommandsSupportedGet = 0x02,
    CommandsSupportedReport = 0x03,
    SchemeGet = 0x04,
    SchemeReport = 0x05,
    NetworkKeySet = 0x06,
    NetworkKeyVerify = 0x07,
    SchemeInherit = 0x08,
    NonceGet = 0x40,
    NonceReport = 0x80,
    CommandEncapsulation = 0x81,
    CommandEncapsulationNonceGet = 0xC1,
}

/// <summary>
/// Implements the Security 0 (S0) Command Class (version 1).
/// </summary>
/// <remarks>
/// Per the Transport-Encapsulation spec (SDS13783) §3.5, S0 is a Transport-Encapsulation CC that
/// provides command encapsulation using AES-128 (OFB) encryption and a CBC-based MAC. Phase 1
/// provides the encryption/nonce/key infrastructure and the command structs; the Driver
/// integration that inserts this layer into the encapsulation pipeline (spec §4.1.3.5) is a
/// follow-up.
/// </remarks>
[CommandClass(CommandClassId.Security0)]
public sealed partial class Security0CommandClass : CommandClass<Security0Command>
{
    // Spec §3.5.3.2 Table 4: the Supported Security Schemes byte for an S0 node is 0x00 —
    // bit 0 cleared indicates S0 support and all other bits are reserved (must be 0).
    private const byte SupportedSecuritySchemesS0 = 0b0000_0000;

    private readonly S0SecurityManager _manager;

    internal Security0CommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
        _manager = new S0SecurityManager(endpoint.NodeId);
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(Security0Command command)
        => command switch
        {
            Security0Command.CommandsSupportedGet => true,
            Security0Command.SchemeGet => true,
            Security0Command.NetworkKeySet => true,
            Security0Command.NetworkKeyVerify => true,
            Security0Command.SchemeInherit => true,
            Security0Command.NonceGet => true,
            _ => false,
        };

    /// <summary>
    /// Per spec §3.5, Security 0 is a Transport-Encapsulation CC.
    /// </summary>
    internal override CommandClassCategory Category => CommandClassCategory.Transport;

    /// <summary>
    /// Per spec §3.5, there is no mandatory node interview for this Command Class.
    /// </summary>
    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((Security0Command)frame.CommandId)
        {
            case Security0Command.NonceReport:
            {
                Security0Nonce nonce = NonceReportCommand.Parse(frame, Logger);
                OnNonceReportReceived?.Invoke(nonce);
                break;
            }
            case Security0Command.SchemeReport:
            {
                Security0SchemeReport report = SchemeReportCommand.Parse(frame, Logger);
                OnSchemeReportReceived?.Invoke(report);
                break;
            }
            case Security0Command.CommandsSupportedReport:
            {
                (Security0CommandsSupportedReport report, _) = CommandsSupportedReportCommand.Parse(frame, Logger);
                OnCommandsSupportedReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
