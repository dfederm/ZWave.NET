using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class AssociationCommandClass
{
    /// <summary>
    /// Gets the number of association groups supported by this node, or null if not yet queried.
    /// </summary>
    public byte? SupportedGroupings { get; private set; }

    /// <summary>
    /// Request the number of association groups that this node supports.
    /// </summary>
    public async Task<byte> GetSupportedGroupingsAsync(CancellationToken cancellationToken)
    {
        AssociationSupportedGroupingsGetCommand command = AssociationSupportedGroupingsGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<AssociationSupportedGroupingsReportCommand>(cancellationToken).ConfigureAwait(false);
        byte groupings = AssociationSupportedGroupingsReportCommand.Parse(reportFrame, Logger);
        SupportedGroupings = groupings;
        return groupings;
    }

    internal readonly struct AssociationSupportedGroupingsGetCommand : ICommand
    {
        public AssociationSupportedGroupingsGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.SupportedGroupingsGet;

        public CommandClassFrame Frame { get; }

        public static AssociationSupportedGroupingsGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new AssociationSupportedGroupingsGetCommand(frame);
        }
    }

    internal readonly struct AssociationSupportedGroupingsReportCommand : ICommand
    {
        public AssociationSupportedGroupingsReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.SupportedGroupingsReport;

        public CommandClassFrame Frame { get; }

        public static AssociationSupportedGroupingsReportCommand Create(byte supportedGroupings)
        {
            ReadOnlySpan<byte> commandParameters = [supportedGroupings];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationSupportedGroupingsReportCommand(frame);
        }

        public static byte Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Association Supported Groupings Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Supported Groupings Report frame is too short");
            }

            return frame.CommandParameters.Span[0];
        }
    }
}
