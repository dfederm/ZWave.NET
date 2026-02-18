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
}
