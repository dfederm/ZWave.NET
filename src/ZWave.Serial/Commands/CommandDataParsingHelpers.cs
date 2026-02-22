namespace ZWave.Serial.Commands;

internal static class CommandDataParsingHelpers
{
    public static IReadOnlyList<CommandClassInfo> ParseCommandClasses(ReadOnlySpan<byte> allCommandClasses)
    {
        var commandClassInfos = new List<CommandClassInfo>(allCommandClasses.Length);
        bool isSupported = true;
        bool isControlled = false;
        for (int i = 0; i < allCommandClasses.Length; i++)
        {
            var commandClassId = (CommandClassId)allCommandClasses[i];
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

    /// <summary>
    /// Parses a node ID bitmask into a set of node IDs.
    /// Each bit in the bitmask represents a node: bit N in byte J corresponds to
    /// node ID <paramref name="baseNodeId"/> + J * 8 + N.
    /// </summary>
    /// <param name="bitMask">The bitmask bytes to parse.</param>
    /// <param name="baseNodeId">
    /// The node ID corresponding to bit 0 of byte 0.
    /// For classic Z-Wave nodes this is 1 (bit 0 = NodeID 1).
    /// For Long Range nodes this is 256 + BITMASK_OFFSET * 8.
    /// </param>
    public static HashSet<ushort> ParseNodeBitmask(ReadOnlySpan<byte> bitMask, ushort baseNodeId)
    {
        HashSet<ushort> nodeIds = new HashSet<ushort>(bitMask.Length * 8);
        for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
        {
            for (int bitNum = 0; bitNum < 8; bitNum++)
            {
                if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                {
                    ushort nodeId = (ushort)(baseNodeId + byteNum * 8 + bitNum);
                    nodeIds.Add(nodeId);
                }
            }
        }

        return nodeIds;
    }
}
