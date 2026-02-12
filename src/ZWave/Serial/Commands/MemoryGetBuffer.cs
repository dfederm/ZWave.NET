namespace ZWave.Serial.Commands;

/// <summary>
/// Read a number of bytes from the NVM allocated for the application.
/// </summary>
public readonly struct MemoryGetBufferRequest : ICommand<MemoryGetBufferRequest>
{
    public MemoryGetBufferRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryGetBuffer;

    public DataFrame Frame { get; }

    public static MemoryGetBufferRequest Create(ushort offset, byte length)
    {
        Span<byte> commandParameters = stackalloc byte[3];
        offset.WriteBytesBE(commandParameters);
        commandParameters[2] = length;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new MemoryGetBufferRequest(frame);
    }

    public static MemoryGetBufferRequest Create(DataFrame frame) => new MemoryGetBufferRequest(frame);
}

public readonly struct MemoryGetBufferResponse : ICommand<MemoryGetBufferResponse>
{
    public MemoryGetBufferResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryGetBuffer;

    public DataFrame Frame { get; }

    /// <summary>
    /// The data read from NVM.
    /// </summary>
    public ReadOnlyMemory<byte> Data => Frame.CommandParameters;

    public static MemoryGetBufferResponse Create(DataFrame frame) => new MemoryGetBufferResponse(frame);
}
