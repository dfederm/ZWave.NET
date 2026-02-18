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
    bool IsControlled);