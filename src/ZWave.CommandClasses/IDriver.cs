namespace ZWave.CommandClasses;

/// <summary>
/// Provides the driver operations needed by command classes.
/// </summary>
public interface IDriver
{
    /// <summary>
    /// Sends a command class command to a specific node and endpoint.
    /// </summary>
    Task SendCommandAsync<TCommand>(
        TCommand command,
        ushort nodeId,
        byte endpointIndex,
        CancellationToken cancellationToken)
        where TCommand : struct, ICommand;

    /// <summary>
    /// Gets a node by its ID, or null if the node is not found.
    /// </summary>
    INode? GetNode(ushort nodeId);
}
