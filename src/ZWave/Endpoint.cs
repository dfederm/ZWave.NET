using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;

namespace ZWave;

/// <summary>
/// Represents a Multi Channel End Point of a Z-Wave node.
/// </summary>
/// <remarks>
/// An endpoint represents a functional sub-unit of a Z-Wave node. Endpoints 1–127 are
/// individual endpoints discovered via the Multi Channel Command Class. Each endpoint has
/// its own set of supported command classes.
/// </remarks>
public sealed class Endpoint : IEndpoint
{
    private readonly CommandClassCollection _commandClassCollection;

    internal Endpoint(ushort nodeId, byte endpointIndex, IDriver driver, ILogger logger)
    {
        if (endpointIndex == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endpointIndex), "Endpoint index 0 is reserved for the Root Device (the Node itself).");
        }

        NodeId = nodeId;
        EndpointIndex = endpointIndex;
        _commandClassCollection = new CommandClassCollection(driver, this, logger);
    }

    /// <inheritdoc />
    public ushort NodeId { get; }

    /// <inheritdoc />
    public byte EndpointIndex { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses => _commandClassCollection.CommandClasses;

    /// <inheritdoc />
    public CommandClass GetCommandClass(CommandClassId commandClassId)
        => _commandClassCollection.GetCommandClass(commandClassId);

    /// <summary>
    /// Gets a specific command class by its CLR type.
    /// </summary>
    public TCommandClass GetCommandClass<TCommandClass>()
        where TCommandClass : CommandClass
        => _commandClassCollection.GetCommandClass<TCommandClass>();

    /// <summary>
    /// Tries to get a specific command class by its command class ID.
    /// </summary>
    public bool TryGetCommandClass(CommandClassId commandClassId, [NotNullWhen(true)] out CommandClass? commandClass)
        => _commandClassCollection.TryGetCommandClass(commandClassId, out commandClass);

    /// <summary>
    /// Tries to get a specific command class by its CLR type.
    /// </summary>
    public bool TryGetCommandClass<TCommandClass>([NotNullWhen(true)] out TCommandClass? commandClass)
        where TCommandClass : CommandClass
        => _commandClassCollection.TryGetCommandClass(out commandClass);

    internal void AddCommandClasses(IReadOnlyList<CommandClassInfo> commandClassInfos)
        => _commandClassCollection.AddCommandClasses(commandClassInfos);

    internal void ProcessCommand(CommandClassFrame frame)
        => _commandClassCollection.ProcessCommand(frame);

    internal Task InterviewCommandClassesAsync(CancellationToken cancellationToken)
        => _commandClassCollection.InterviewCommandClassesAsync(cancellationToken);
}
