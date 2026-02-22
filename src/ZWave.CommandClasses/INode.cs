namespace ZWave.CommandClasses;

/// <summary>
/// Provides node-level operations beyond what <see cref="IEndpoint"/> exposes.
/// </summary>
/// <remarks>
/// A node IS endpoint 0. <see cref="INode"/> extends <see cref="IEndpoint"/> with
/// node-level properties that are not per-endpoint (e.g. frequent listening mode).
/// </remarks>
public interface INode : IEndpoint
{
    /// <summary>
    /// Gets the frequent listening mode of the node.
    /// </summary>
    FrequentListeningMode FrequentListeningMode { get; }
}
