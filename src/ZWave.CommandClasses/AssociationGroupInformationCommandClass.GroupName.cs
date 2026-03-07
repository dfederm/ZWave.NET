using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class AssociationGroupInformationCommandClass
{
    /// <summary>
    /// Gets the cached group names, keyed by grouping identifier.
    /// </summary>
    public IReadOnlyDictionary<byte, string> GroupNames => _groupNames;

    private readonly Dictionary<byte, string> _groupNames = [];

    /// <summary>
    /// Request the name of an association group.
    /// </summary>
    /// <param name="groupingIdentifier">The association group identifier (1-255).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The UTF-8 encoded name of the association group.</returns>
    public async Task<string> GetGroupNameAsync(byte groupingIdentifier, CancellationToken cancellationToken)
    {
        var command = GroupNameGetCommand.Create(groupingIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<GroupNameReportCommand>(
            frame => frame.CommandParameters.Length >= 1 && frame.CommandParameters.Span[0] == groupingIdentifier,
            cancellationToken).ConfigureAwait(false);
        (byte _, string name) = GroupNameReportCommand.Parse(reportFrame, Logger);
        _groupNames[groupingIdentifier] = name;
        return name;
    }

    internal readonly struct GroupNameGetCommand : ICommand
    {
        public GroupNameGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.GroupNameGet;

        public CommandClassFrame Frame { get; }

        public static GroupNameGetCommand Create(byte groupingIdentifier)
        {
            ReadOnlySpan<byte> commandParameters = [groupingIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new GroupNameGetCommand(frame);
        }
    }

    internal readonly struct GroupNameReportCommand : ICommand
    {
        public GroupNameReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.GroupNameReport;

        public CommandClassFrame Frame { get; }

        public static GroupNameReportCommand Create(byte groupingIdentifier, string name)
        {
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            Span<byte> commandParameters = stackalloc byte[2 + nameBytes.Length];
            commandParameters[0] = groupingIdentifier;
            commandParameters[1] = (byte)nameBytes.Length;
            nameBytes.CopyTo(commandParameters[2..]);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new GroupNameReportCommand(frame);
        }

        /// <summary>
        /// Parse an Association Group Name Report frame.
        /// </summary>
        /// <returns>The grouping identifier and the UTF-8 encoded group name.</returns>
        public static (byte GroupingIdentifier, string Name) Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: GroupingIdentifier (1) + NameLength (1) = 2 bytes
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning(
                    "Association Group Name Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Group Name Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte groupingIdentifier = span[0];
            byte nameLength = span[1];

            if (frame.CommandParameters.Length < 2 + nameLength)
            {
                logger.LogWarning(
                    "Association Group Name Report frame is too short for declared name length ({DeclaredLength} bytes, but only {Available} available)",
                    nameLength,
                    frame.CommandParameters.Length - 2);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Group Name Report frame is too short for declared name length");
            }

            string name = nameLength > 0
                ? Encoding.UTF8.GetString(span.Slice(2, nameLength))
                : string.Empty;

            return (groupingIdentifier, name);
        }
    }
}
