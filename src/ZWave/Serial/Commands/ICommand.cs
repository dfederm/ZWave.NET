namespace ZWave.Serial.Commands;

public interface ICommand<TCommand> where TCommand : struct, ICommand<TCommand>
{
    public static abstract DataFrameType Type { get; }

    public static abstract CommandId CommandId { get; }

    public static abstract TCommand Create(DataFrame frame);

    public DataFrame Frame { get; }
}