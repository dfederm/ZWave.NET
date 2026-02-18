namespace ZWave.Serial.Commands;

/// <summary>
/// Defines a Serial API command that can be created from and represented as a data frame.
/// </summary>
public interface ICommand<TCommand> where TCommand : struct, ICommand<TCommand>
{
    /// <summary>
    /// Gets the data frame type (REQ or RES) for this command.
    /// </summary>
    public static abstract DataFrameType Type { get; }

    /// <summary>
    /// Gets the Serial API command identifier.
    /// </summary>
    public static abstract CommandId CommandId { get; }

    /// <summary>
    /// Creates an instance of this command from a data frame.
    /// </summary>
    public static abstract TCommand Create(DataFrame frame);

    /// <summary>
    /// Gets the data frame representation of this command.
    /// </summary>
    public DataFrame Frame { get; }
}