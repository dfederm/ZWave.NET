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
    /// Extended (2-byte) command classes (MSB 0xF1..0xFF) are skipped because they cannot be
    /// represented by <see cref="CommandClassId"/> (a byte); a truncated extended class (a 0xF1..0xFF
    /// byte with no following byte) throws <see cref="ZWaveException"/>.
    /// </remarks>
    public static IReadOnlyList<CommandClassInfo> ParseList(ReadOnlySpan<byte> commandClassBytes)
    {
        List<CommandClassInfo> commandClassInfos = new List<CommandClassInfo>(commandClassBytes.Length);
        bool isSupported = true;
        bool isControlled = false;
        for (int i = 0; i < commandClassBytes.Length; i++)
        {
            byte commandClassByte = commandClassBytes[i];

            // Extended (2-byte) command classes (MSB 0xF1..0xFF) cannot be represented by
            // CommandClassId (byte); skip the pair to keep the remaining list aligned. 0xF0 is a
            // reserved value, not part of the extended range (spec §3.2 Command Class format).
            // TODO: Handle extended (2-byte) command classes properly instead of skipping them.
            if (commandClassByte >= 0xF1)
            {
                if (i + 1 >= commandClassBytes.Length)
                {
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Command class list is truncated");
                }

                i++;
                continue;
            }

            CommandClassId commandClassId = (CommandClassId)commandClassByte;
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