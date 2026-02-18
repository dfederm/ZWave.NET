namespace ZWave.CommandClasses;

public interface ICommand
{
    public static abstract CommandClassId CommandClassId { get; }

    public static abstract byte CommandId { get; }

    public CommandClassFrame Frame { get; }
}
