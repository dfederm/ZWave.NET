namespace ZWave;

/// <summary>
/// Identifies the type of a Z-Wave node.
/// </summary>
public enum NodeType
{
    /// <summary>
    /// The node type could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// The node is a controller.
    /// </summary>
    Controller,

    /// <summary>
    /// The node is an end node (slave device).
    /// </summary>
    EndNode,
}
