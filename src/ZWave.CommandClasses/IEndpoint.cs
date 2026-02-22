namespace ZWave.CommandClasses;

/// <summary>
/// Provides endpoint-level operations needed by command classes.
/// </summary>
/// <remarks>
/// An endpoint represents a functional sub-unit of a Z-Wave node. Endpoint 0 is the
/// "Root Device" (the node itself). Endpoints 1–127 are individual endpoints discovered
/// via the Multi Channel Command Class. Each endpoint has its own set of supported
/// command classes.
/// </remarks>
public interface IEndpoint
{
    /// <summary>
    /// Gets the node ID of the node this endpoint belongs to.
    /// </summary>
    ushort NodeId { get; }

    /// <summary>
    /// Gets the endpoint index. Zero represents the root device (the node itself).
    /// </summary>
    byte EndpointIndex { get; }

    /// <summary>
    /// Gets the command classes supported by this endpoint.
    /// </summary>
    IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses { get; }

    /// <summary>
    /// Gets a specific command class by ID.
    /// </summary>
    CommandClass GetCommandClass(CommandClassId commandClassId);
}
