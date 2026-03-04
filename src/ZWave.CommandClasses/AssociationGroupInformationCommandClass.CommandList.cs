using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a command (Command Class + Command ID pair) that an association group sends.
/// </summary>
/// <remarks>
/// The command class identifier is a <see cref="ushort"/> because extended command classes
/// use a 2-byte encoding (first byte 0xF1-0xFF followed by a second byte). Normal command
/// classes (0x20-0xEE) fit in a single byte.
/// </remarks>
public readonly record struct AssociationGroupCommand(
    /// <summary>
    /// The command class identifier. Normal CCs are in the range 0x20-0xEE (single byte).
    /// Extended CCs are in the range 0xF100-0xFFFF (two bytes).
    /// </summary>
    ushort CommandClassId,

    /// <summary>
    /// The command identifier within the command class.
    /// </summary>
    byte CommandId);

public sealed partial class AssociationGroupInformationCommandClass
{
    /// <summary>
    /// Gets the cached command lists, keyed by grouping identifier.
    /// </summary>
    public IReadOnlyDictionary<byte, IReadOnlyList<AssociationGroupCommand>> CommandLists => _commandLists;

    private readonly Dictionary<byte, IReadOnlyList<AssociationGroupCommand>> _commandLists = [];

    /// <summary>
    /// Request the commands that are sent via a given association group.
    /// </summary>
    /// <param name="groupingIdentifier">The association group identifier (1-255).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The list of commands sent via this association group.</returns>
    public async Task<IReadOnlyList<AssociationGroupCommand>> GetCommandListAsync(
        byte groupingIdentifier,
        CancellationToken cancellationToken)
    {
        var command = CommandListGetCommand.Create(groupingIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<CommandListReportCommand>(
            frame => frame.CommandParameters.Length >= 1 && frame.CommandParameters.Span[0] == groupingIdentifier,
            cancellationToken).ConfigureAwait(false);
        (byte _, IReadOnlyList<AssociationGroupCommand> commands) = CommandListReportCommand.Parse(reportFrame, Logger);
        _commandLists[groupingIdentifier] = commands;
        return commands;
    }

    internal readonly struct CommandListGetCommand : ICommand
    {
        public CommandListGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.CommandListGet;

        public CommandClassFrame Frame { get; }

        public static CommandListGetCommand Create(byte groupingIdentifier)
        {
            // Byte 0: [Allow Cache (1 bit)] [Reserved (7 bits)]
            // Per spec CC:0059.01.05.12.001: A requesting node SHOULD allow caching.
            byte flags = 0b1000_0000;

            ReadOnlySpan<byte> commandParameters = [flags, groupingIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new CommandListGetCommand(frame);
        }
    }

    internal readonly struct CommandListReportCommand : ICommand
    {
        public CommandListReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.AssociationGroupInformation;

        public static byte CommandId => (byte)AssociationGroupInformationCommand.CommandListReport;

        public CommandClassFrame Frame { get; }

        public static CommandListReportCommand Create(
            byte groupingIdentifier,
            IReadOnlyList<AssociationGroupCommand> commands)
        {
            // Calculate list length: each command is 2 bytes (normal CC) or 3 bytes (extended CC)
            int listLength = 0;
            for (int i = 0; i < commands.Count; i++)
            {
                listLength += commands[i].CommandClassId >= 0xF100 ? 3 : 2;
            }

            Span<byte> commandParameters = stackalloc byte[2 + listLength];
            commandParameters[0] = groupingIdentifier;
            commandParameters[1] = (byte)listLength;

            int offset = 2;
            for (int i = 0; i < commands.Count; i++)
            {
                AssociationGroupCommand cmd = commands[i];
                if (cmd.CommandClassId >= 0xF100)
                {
                    commandParameters[offset++] = (byte)(cmd.CommandClassId >> 8);
                    commandParameters[offset++] = (byte)(cmd.CommandClassId & 0xFF);
                }
                else
                {
                    commandParameters[offset++] = (byte)cmd.CommandClassId;
                }

                commandParameters[offset++] = cmd.CommandId;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new CommandListReportCommand(frame);
        }

        /// <summary>
        /// Parse an Association Group Command List Report frame.
        /// </summary>
        /// <returns>The grouping identifier and the list of commands.</returns>
        public static (byte GroupingIdentifier, IReadOnlyList<AssociationGroupCommand> Commands) Parse(
            CommandClassFrame frame,
            ILogger logger)
        {
            // Minimum: GroupingIdentifier (1) + ListLength (1) = 2 bytes
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning(
                    "Association Group Command List Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Group Command List Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte groupingIdentifier = span[0];
            byte listLength = span[1];

            if (frame.CommandParameters.Length < 2 + listLength)
            {
                logger.LogWarning(
                    "Association Group Command List Report frame is too short for declared list length ({DeclaredLength} bytes, but only {Available} available)",
                    listLength,
                    frame.CommandParameters.Length - 2);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Association Group Command List Report frame is too short for declared list length");
            }

            List<AssociationGroupCommand> commands = [];
            int offset = 2;
            int endOffset = 2 + listLength;

            while (offset < endOffset)
            {
                byte ccByte = span[offset];

                ushort ccId;
                if (ccByte >= 0xF1)
                {
                    // Extended command class: 2-byte CC ID
                    if (offset + 2 >= endOffset)
                    {
                        logger.LogWarning(
                            "Association Group Command List Report has truncated extended command class entry at offset {Offset}",
                            offset);
                        break;
                    }

                    ccId = (ushort)((ccByte << 8) | span[offset + 1]);
                    offset += 2;
                }
                else
                {
                    // Normal command class: 1-byte CC ID
                    ccId = ccByte;
                    offset += 1;
                }

                if (offset >= endOffset)
                {
                    logger.LogWarning(
                        "Association Group Command List Report has truncated command entry (missing command ID) at offset {Offset}",
                        offset);
                    break;
                }

                byte commandId = span[offset];
                offset += 1;

                commands.Add(new AssociationGroupCommand(ccId, commandId));
            }

            return (groupingIdentifier, commands);
        }
    }
}
