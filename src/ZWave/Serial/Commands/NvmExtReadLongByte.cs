namespace ZWave.Serial.Commands;

/// <summary>
/// Read a byte from external NVM at address offset.
/// </summary>
public readonly struct NvmExtReadLongByteRequest : ICommand<NvmExtReadLongByteRequest>
{
    public NvmExtReadLongByteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NvmExtReadLongByte;

    public DataFrame Frame { get; }

    /// <param name="offset">The 24-bit NVM address offset.</param>
    public static NvmExtReadLongByteRequest Create(uint offset)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            // 24-bit offset in big-endian
            (byte)(offset >> 16),
            (byte)(offset >> 8),
            (byte)offset,
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NvmExtReadLongByteRequest(frame);
    }

    public static NvmExtReadLongByteRequest Create(DataFrame frame) => new NvmExtReadLongByteRequest(frame);
}

public readonly struct NvmExtReadLongByteResponse : ICommand<NvmExtReadLongByteResponse>
{
    public NvmExtReadLongByteResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NvmExtReadLongByte;

    public DataFrame Frame { get; }

    /// <summary>
    /// The status of the NVM operation.
    /// </summary>
    public NvmStatus Status => (NvmStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The data byte read from NVM.
    /// </summary>
    public byte Data => Frame.CommandParameters.Span[0];

    public static NvmExtReadLongByteResponse Create(DataFrame frame) => new NvmExtReadLongByteResponse(frame);
}
