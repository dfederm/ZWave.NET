namespace ZWave.Serial.Commands;

/// <summary>
/// Write a number of bytes to external NVM starting from address offset.
/// </summary>
public readonly struct NvmExtWriteLongBufferRequest : ICommand<NvmExtWriteLongBufferRequest>
{
    public NvmExtWriteLongBufferRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NvmExtWriteLongBuffer;

    public DataFrame Frame { get; }

    /// <param name="offset">The 24-bit NVM address offset.</param>
    /// <param name="data">The data to write.</param>
    public static NvmExtWriteLongBufferRequest Create(uint offset, ReadOnlySpan<byte> data)
    {
        Span<byte> commandParameters = stackalloc byte[5 + data.Length];
        // 24-bit offset in big-endian
        commandParameters[0] = (byte)(offset >> 16);
        commandParameters[1] = (byte)(offset >> 8);
        commandParameters[2] = (byte)offset;
        ((ushort)data.Length).WriteBytesBE(commandParameters[3..]);
        data.CopyTo(commandParameters[5..]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NvmExtWriteLongBufferRequest(frame);
    }

    public static NvmExtWriteLongBufferRequest Create(DataFrame frame) => new NvmExtWriteLongBufferRequest(frame);
}

public readonly struct NvmExtWriteLongBufferResponse : ICommand<NvmExtWriteLongBufferResponse>
{
    public NvmExtWriteLongBufferResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NvmExtWriteLongBuffer;

    public DataFrame Frame { get; }

    /// <summary>
    /// The status of the NVM operation.
    /// </summary>
    public NvmStatus Status => (NvmStatus)Frame.CommandParameters.Span[0];

    public static NvmExtWriteLongBufferResponse Create(DataFrame frame) => new NvmExtWriteLongBufferResponse(frame);
}
