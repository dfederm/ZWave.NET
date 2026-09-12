using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Security Scheme Report (spec §3.5.3.3). Carries the Supported Security Schemes
/// byte, the same field as Scheme Get (S0 is represented by 0x00).
/// </summary>
public readonly record struct Security0SchemeReport(byte SupportedSecuritySchemes);

public sealed partial class Security0CommandClass
{
    /// <summary>
    /// Event raised when a Scheme Report is received (solicited or unsolicited).
    /// </summary>
    public event Action<Security0SchemeReport>? OnSchemeReportReceived;

    /// <summary>
    /// Queries the security schemes supported by this node.
    /// </summary>
    public async Task<Security0SchemeReport> GetSchemeAsync(CancellationToken cancellationToken)
    {
        SchemeGetCommand command = SchemeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<SchemeReportCommand>(cancellationToken).ConfigureAwait(false);
        Security0SchemeReport report = SchemeReportCommand.Parse(reportFrame, Logger);
        OnSchemeReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Scheme Get command (spec §3.5.3.2). Carries the Supported Security Schemes byte.
    /// </summary>
    internal readonly struct SchemeGetCommand : ICommand
    {
        public SchemeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.SchemeGet;

        public CommandClassFrame Frame { get; }

        public static SchemeGetCommand Create()
            => new(CommandClassFrame.Create(CommandClassId, CommandId, [SupportedSecuritySchemesS0]));
    }

    /// <summary>
    /// Scheme Report command (spec §3.5.3.3). Carries the Supported Security Schemes byte, the
    /// same field as Scheme Get; S0 is represented by 0x00.
    /// </summary>
    internal readonly struct SchemeReportCommand : ICommand
    {
        public SchemeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.SchemeReport;

        public CommandClassFrame Frame { get; }

        public static SchemeReportCommand Create()
            => new(CommandClassFrame.Create(CommandClassId, CommandId, [SupportedSecuritySchemesS0]));

        public static Security0SchemeReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length != 1)
            {
                logger.LogWarning("Scheme Report frame is malformed ({Length} bytes, expected 1)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Scheme Report frame is malformed");
            }

            return new Security0SchemeReport(frame.CommandParameters.Span[0]);
        }
    }
}
