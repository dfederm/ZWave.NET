using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class MultiChannelAssociationCommandClass
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
        MultiChannelAssociationSupportedGroupingsGetCommand command = MultiChannelAssociationSupportedGroupingsGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultiChannelAssociationSupportedGroupingsReportCommand>(cancellationToken).ConfigureAwait(false);
        byte groupings = MultiChannelAssociationSupportedGroupingsReportCommand.Parse(reportFrame, Logger);
        SupportedGroupings = groupings;
        return groupings;
    }

    internal readonly struct MultiChannelAssociationSupportedGroupingsGetCommand : ICommand
    {
        public MultiChannelAssociationSupportedGroupingsGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.SupportedGroupingsGet;

        public CommandClassFrame Frame { get; }

        public static MultiChannelAssociationSupportedGroupingsGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultiChannelAssociationSupportedGroupingsGetCommand(frame);
        }
    }

    internal readonly struct MultiChannelAssociationSupportedGroupingsReportCommand : ICommand
    {
        public MultiChannelAssociationSupportedGroupingsReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannelAssociation;

        public static byte CommandId => (byte)MultiChannelAssociationCommand.SupportedGroupingsReport;

        public CommandClassFrame Frame { get; }

        public static byte Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Multi Channel Association Supported Groupings Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Multi Channel Association Supported Groupings Report frame is too short");
            }

            return frame.CommandParameters.Span[0];
        }
    }
}
