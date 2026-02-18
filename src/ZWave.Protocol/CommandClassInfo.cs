namespace ZWave;

public record struct CommandClassInfo(
    CommandClassId CommandClass,
    bool IsSupported,
    bool IsControlled);