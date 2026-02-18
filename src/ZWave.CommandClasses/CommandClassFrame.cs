namespace ZWave.CommandClasses;

public readonly struct CommandClassFrame
{
    public CommandClassFrame(ReadOnlyMemory<byte> data)
    {
        if (data.Span.Length < 2)
        {
            throw new ArgumentException("Command class frames must be at least 2 bytes long", nameof(data));
        }

        Data = data;
    }

    public ReadOnlyMemory<byte> Data { get; }

    public CommandClassId CommandClassId => (CommandClassId)Data.Span[0];

    public byte CommandId => Data.Span[1];

    public ReadOnlyMemory<byte> CommandParameters => Data[2..];

    public static CommandClassFrame Create(CommandClassId commandClassId, byte commandId)
        => Create(commandClassId, commandId, ReadOnlySpan<byte>.Empty);

    public static CommandClassFrame Create(CommandClassId commandClassId, byte commandId, ReadOnlySpan<byte> commandParameters)
    {
        byte[] data = new byte[2 + commandParameters.Length];
        data[0] = (byte)commandClassId;
        data[1] = commandId;
        commandParameters.CopyTo(data.AsSpan()[2..]);
        return new CommandClassFrame(data);
    }
}
