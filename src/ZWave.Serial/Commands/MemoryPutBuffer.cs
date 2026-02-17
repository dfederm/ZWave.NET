namespace ZWave.Serial.Commands;

/// <summary>
/// Copy a number of bytes from a RAM buffer to the application area of the NVM.
/// </summary>
public readonly struct MemoryPutBufferRequest : ICommand<MemoryPutBufferRequest>
{
    public MemoryPutBufferRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryPutBuffer;

    public DataFrame Frame { get; }

    public static MemoryPutBufferRequest Create(ushort offset, ReadOnlySpan<byte> data)
    {
        Span<byte> commandParameters = stackalloc byte[4 + data.Length];
        offset.WriteBytesBE(commandParameters);
        ((ushort)data.Length).WriteBytesBE(commandParameters[2..]);
        data.CopyTo(commandParameters[4..]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new MemoryPutBufferRequest(frame);
    }

    public static MemoryPutBufferRequest Create(DataFrame frame) => new MemoryPutBufferRequest(frame);
}

public readonly struct MemoryPutBufferResponse : ICommand<MemoryPutBufferResponse>
{
    public MemoryPutBufferResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryPutBuffer;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the buffer was successfully written.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static MemoryPutBufferResponse Create(DataFrame frame) => new MemoryPutBufferResponse(frame);
}
