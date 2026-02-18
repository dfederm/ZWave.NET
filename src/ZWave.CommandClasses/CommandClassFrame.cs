namespace ZWave.CommandClasses;

/// <summary>
/// Represents a command class frame containing a command class ID, command ID, and optional parameters.
/// </summary>
public readonly struct CommandClassFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandClassFrame"/> struct from raw payload bytes.
    /// </summary>
    public CommandClassFrame(ReadOnlyMemory<byte> data)
    {
        if (data.Span.Length < 2)
        {
            throw new ArgumentException("Command class frames must be at least 2 bytes long", nameof(data));
        }

        Data = data;
    }

    /// <summary>
    /// Gets the raw frame data.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets the command class identifier.
    /// </summary>
    public CommandClassId CommandClassId => (CommandClassId)Data.Span[0];

    /// <summary>
    /// Gets the command identifier within the command class.
    /// </summary>
    public byte CommandId => Data.Span[1];

    /// <summary>
    /// Gets the command parameters, excluding the command class ID and command ID.
    /// </summary>
    public ReadOnlyMemory<byte> CommandParameters => Data[2..];

    /// <summary>
    /// Creates a new command class frame with no command parameters.
    /// </summary>
    public static CommandClassFrame Create(CommandClassId commandClassId, byte commandId)
        => Create(commandClassId, commandId, []);

    /// <summary>
    /// Creates a new command class frame with the specified command parameters.
    /// </summary>
    public static CommandClassFrame Create(CommandClassId commandClassId, byte commandId, ReadOnlySpan<byte> commandParameters)
    {
        byte[] data = new byte[2 + commandParameters.Length];
        data[0] = (byte)commandClassId;
        data[1] = commandId;
        commandParameters.CopyTo(data.AsSpan()[2..]);
        return new CommandClassFrame(data);
    }
}
