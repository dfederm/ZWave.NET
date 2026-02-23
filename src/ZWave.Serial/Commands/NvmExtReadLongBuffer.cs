namespace ZWave.Serial.Commands;

/// <summary>
/// Read a number of bytes from external NVM starting from address offset.
/// </summary>
public readonly struct NvmExtReadLongBufferRequest : ICommand<NvmExtReadLongBufferRequest>
{
    public NvmExtReadLongBufferRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NvmExtReadLongBuffer;

    public DataFrame Frame { get; }

    /// <param name="offset">The 24-bit NVM address offset.</param>
    /// <param name="length">The number of bytes to read.</param>
    public static NvmExtReadLongBufferRequest Create(uint offset, ushort length)
    {
        Span<byte> commandParameters = stackalloc byte[5];
        // 24-bit offset in big-endian
        commandParameters[0] = (byte)(offset >> 16);
        commandParameters[1] = (byte)(offset >> 8);
        commandParameters[2] = (byte)offset;
        length.WriteBytesBE(commandParameters[3..]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NvmExtReadLongBufferRequest(frame);
    }

    public static NvmExtReadLongBufferRequest Create(DataFrame frame, CommandParsingContext context) => new NvmExtReadLongBufferRequest(frame);
}

public readonly struct NvmExtReadLongBufferResponse : ICommand<NvmExtReadLongBufferResponse>
{
    public NvmExtReadLongBufferResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NvmExtReadLongBuffer;

    public DataFrame Frame { get; }

    /// <summary>
    /// The data read from NVM.
    /// </summary>
    public ReadOnlyMemory<byte> Data => Frame.CommandParameters;

    public static NvmExtReadLongBufferResponse Create(DataFrame frame, CommandParsingContext context) => new NvmExtReadLongBufferResponse(frame);
}
