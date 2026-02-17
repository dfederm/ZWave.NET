namespace ZWave.CommandClasses;

public record struct CommandClassInfo(
    CommandClassId CommandClass,
    bool IsSupported,
    bool IsControlled);