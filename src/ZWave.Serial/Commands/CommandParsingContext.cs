namespace ZWave.Serial.Commands;

/// <summary>
/// Provides protocol-level context needed when parsing Serial API command frames.
/// </summary>
/// <remarks>
/// This is passed to <see cref="ICommand{TCommand}.Create(DataFrame, CommandParsingContext)"/> to provide
/// session-level state (like the configured NodeID encoding) without coupling it to the wire-format
/// <see cref="DataFrame"/>.
/// </remarks>
/// <param name="NodeIdType">The NodeID base type used for encoding NodeID fields.</param>
public readonly record struct CommandParsingContext(NodeIdType NodeIdType);
