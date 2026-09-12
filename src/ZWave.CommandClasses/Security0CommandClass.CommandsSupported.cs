using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Security 0 Commands Supported Report (spec §3.5.4.3).
/// </summary>
public readonly record struct Security0CommandsSupportedReport(
    /// <summary>
    /// The command classes this node supports and/or controls securely.
    /// </summary>
    IReadOnlyList<CommandClassInfo> CommandClasses);

public sealed partial class Security0CommandClass
{
    /// <summary>
    /// Event raised when a Commands Supported Report is received (solicited or unsolicited).
    /// </summary>
    public event Action<Security0CommandsSupportedReport>? OnCommandsSupportedReportReceived;

    /// <summary>
    /// Queries the command classes supported by this node.
    /// </summary>
    /// <remarks>
    /// Per spec §3.5.4, this command is only valid in the secure (encapsulated) context. The
    /// Driver's Security 0 pipeline (a follow-up) inserts the nonce exchange and encapsulation;
    /// this method issues the plain command and awaits the plain report. A node may respond with
    /// several frames; each frame's "reports to follow" count says how many further frames will
    /// follow, and this method accumulates them into a single report.
    /// </remarks>
    public async Task<Security0CommandsSupportedReport> GetCommandsSupportedAsync(CancellationToken cancellationToken)
    {
        CommandsSupportedGetCommand command = CommandsSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<CommandsSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        (Security0CommandsSupportedReport report, byte reportsToFollow) = CommandsSupportedReportCommand.Parse(reportFrame, Logger);
        List<CommandClassInfo> classes = new(report.CommandClasses);
        while (reportsToFollow > 0)
        {
            reportFrame = await AwaitNextReportAsync<CommandsSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
            (report, reportsToFollow) = CommandsSupportedReportCommand.Parse(reportFrame, Logger);
            classes.AddRange(report.CommandClasses);
        }

        Security0CommandsSupportedReport result = new(classes);
        OnCommandsSupportedReportReceived?.Invoke(result);
        return result;
    }

    /// <summary>
    /// Commands Supported Get command (spec §3.5.5.1). No parameters.
    /// </summary>
    internal readonly struct CommandsSupportedGetCommand : ICommand
    {
        public CommandsSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.CommandsSupportedGet;

        public CommandClassFrame Frame { get; }

        public static CommandsSupportedGetCommand Create()
            => new(CommandClassFrame.Create(CommandClassId, CommandId));
    }

    /// <summary>
    /// Commands Supported Report command (spec §3.5.4.3): a reports-to-follow byte, the supported
    /// command classes, the COMMAND_CLASS_MARK, then the controlled command classes.
    /// </summary>
    internal readonly struct CommandsSupportedReportCommand : ICommand
    {
        public CommandsSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.CommandsSupportedReport;

        public CommandClassFrame Frame { get; }

        public static CommandsSupportedReportCommand Create(IReadOnlyList<CommandClassId> supported, IReadOnlyList<CommandClassId> controlled)
        {
            List<byte> parameters = new();
            parameters.Add(0x00); // reports to follow (this is a single-frame report)
            foreach (CommandClassId commandClass in supported)
            {
                parameters.Add((byte)commandClass);
            }

            parameters.Add((byte)CommandClassId.SupportControlMark);
            foreach (CommandClassId commandClass in controlled)
            {
                parameters.Add((byte)commandClass);
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters.ToArray());
            return new CommandsSupportedReportCommand(frame);
        }

        public static (Security0CommandsSupportedReport Report, byte ReportsToFollow) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Commands Supported Report frame is malformed ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Commands Supported Report frame is malformed");
            }

            // parameters[0] is "reports to follow" (a continuation count), not a class count. A
            // single-frame report is 0; the remaining bytes are the supported/controlled class
            // list, which ParseList splits on the SupportControlMark. The count is returned
            // alongside the report (a chaining detail) rather than stored in it.
            byte reportsToFollow = frame.CommandParameters.Span[0];
            IReadOnlyList<CommandClassInfo> commandClasses = CommandClassInfo.ParseList(frame.CommandParameters.Span[1..]);

            return (new Security0CommandsSupportedReport(commandClasses), reportsToFollow);
        }
    }
}
