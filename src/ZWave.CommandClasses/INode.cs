namespace ZWave.CommandClasses;

/// <summary>
/// Provides the node operations needed by command classes.
/// </summary>
public interface INode
{
    /// <summary>
    /// Gets the node ID.
    /// </summary>
    byte Id { get; }

    /// <summary>
    /// Gets the frequent listening mode of the node.
    /// </summary>
    FrequentListeningMode FrequentListeningMode { get; }

    /// <summary>
    /// Gets the command classes supported by this node.
    /// </summary>
    IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses { get; }

    /// <summary>
    /// Gets a specific command class by ID.
    /// </summary>
    CommandClass GetCommandClass(CommandClassId commandClassId);
}
