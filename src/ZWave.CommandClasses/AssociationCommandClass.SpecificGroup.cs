using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class AssociationCommandClass
{
    /// <summary>
    /// Request the association group representing the most recently detected button.
    /// </summary>
    /// <remarks>
    /// This command is available in version 2 and later. It allows a portable controller
    /// to interactively create associations from a multi-button device to a destination
    /// that is out of direct range.
    /// A group value of 0 indicates the functionality is not supported or the most recent
    /// button event does not map to an association group.
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The association group identifier for the most recently detected button, or 0 if not supported.</returns>
    public async Task<byte> GetSpecificGroupAsync(CancellationToken cancellationToken)
    {
        AssociationSpecificGroupGetCommand command = AssociationSpecificGroupGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<AssociationSpecificGroupReportCommand>(cancellationToken).ConfigureAwait(false);
        byte group = AssociationSpecificGroupReportCommand.Parse(reportFrame, Logger);
        return group;
    }

    internal readonly struct AssociationSpecificGroupGetCommand : ICommand
    {
        public AssociationSpecificGroupGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.SpecificGroupGet;

        public CommandClassFrame Frame { get; }

        public static AssociationSpecificGroupGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new AssociationSpecificGroupGetCommand(frame);
        }
    }

    internal readonly struct AssociationSpecificGroupReportCommand : ICommand
    {
        public AssociationSpecificGroupReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Association;

        public static byte CommandId => (byte)AssociationCommand.SpecificGroupReport;

        public CommandClassFrame Frame { get; }

        public static AssociationSpecificGroupReportCommand Create(byte group)
        {
            ReadOnlySpan<byte> commandParameters = [group];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AssociationSpecificGroupReportCommand(frame);
        }

        public static byte Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Association Specific Group Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Specific Group Report frame is too short");
            }

            return frame.CommandParameters.Span[0];
        }
    }
}
