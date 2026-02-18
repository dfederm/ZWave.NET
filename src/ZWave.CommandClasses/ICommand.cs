namespace ZWave.CommandClasses;

/// <summary>
/// Defines a command within a command class.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the command class identifier for this command.
    /// </summary>
    public static abstract CommandClassId CommandClassId { get; }

    /// <summary>
    /// Gets the command identifier within the command class.
    /// </summary>
    public static abstract byte CommandId { get; }

    /// <summary>
    /// Gets the command class frame representation of this command.
    /// </summary>
    public CommandClassFrame Frame { get; }
}
