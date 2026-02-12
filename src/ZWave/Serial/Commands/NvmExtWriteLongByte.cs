namespace ZWave.Serial.Commands;

/// <summary>
/// Write a byte to external NVM at address offset.
/// </summary>
public readonly struct NvmExtWriteLongByteRequest : ICommand<NvmExtWriteLongByteRequest>
{
    public NvmExtWriteLongByteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NvmExtWriteLongByte;

    public DataFrame Frame { get; }

    /// <param name="offset">The 24-bit NVM address offset.</param>
    /// <param name="data">The byte to write.</param>
    public static NvmExtWriteLongByteRequest Create(uint offset, byte data)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            // 24-bit offset in big-endian
            (byte)(offset >> 16),
            (byte)(offset >> 8),
            (byte)offset,
            data,
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NvmExtWriteLongByteRequest(frame);
    }

    public static NvmExtWriteLongByteRequest Create(DataFrame frame) => new NvmExtWriteLongByteRequest(frame);
}

public readonly struct NvmExtWriteLongByteResponse : ICommand<NvmExtWriteLongByteResponse>
{
    public NvmExtWriteLongByteResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NvmExtWriteLongByte;

    public DataFrame Frame { get; }

    /// <summary>
    /// The status of the NVM operation.
    /// </summary>
    public NvmStatus Status => (NvmStatus)Frame.CommandParameters.Span[0];

    public static NvmExtWriteLongByteResponse Create(DataFrame frame) => new NvmExtWriteLongByteResponse(frame);
}
