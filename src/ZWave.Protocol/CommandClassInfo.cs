namespace ZWave;

/// <summary>
/// Provides information about a command class supported or controlled by a node.
/// </summary>
public record struct CommandClassInfo(
    /// <summary>
    /// The command class identifier.
    /// </summary>
    CommandClassId CommandClass,
    /// <summary>
    /// Indicates whether the command class is supported by the node.
    /// </summary>
    bool IsSupported,
    /// <summary>
    /// Indicates whether the command class is controlled by the node.
    /// </summary>
    bool IsControlled)
{
    /// <summary>
    /// Parses a byte span of command class IDs into a list of <see cref="CommandClassInfo"/> records.
    /// </summary>
    /// <remarks>
    /// The byte span may contain the <see cref="CommandClassId.SupportControlMark"/> (0xEF) separator.
    /// IDs before the mark are considered "supported"; IDs after it are "controlled".
    /// If no mark is present, all IDs are treated as supported.
    /// </remarks>
    public static IReadOnlyList<CommandClassInfo> ParseList(ReadOnlySpan<byte> commandClassBytes)
    {
        List<CommandClassInfo> commandClassInfos = new List<CommandClassInfo>(commandClassBytes.Length);
        bool isSupported = true;
        bool isControlled = false;
        for (int i = 0; i < commandClassBytes.Length; i++)
        {
            CommandClassId commandClassId = (CommandClassId)commandClassBytes[i];
            if (commandClassId == CommandClassId.SupportControlMark)
            {
                isSupported = false;
                isControlled = true;
                continue;
            }

            commandClassInfos.Add(new CommandClassInfo(commandClassId, isSupported, isControlled));
        }

        return commandClassInfos;
    }
}