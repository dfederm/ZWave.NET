namespace ZWave.Serial.Commands;

/// <summary>
/// Write one byte to the application area of the NVM.
/// </summary>
public readonly struct MemoryPutByteRequest : ICommand<MemoryPutByteRequest>
{
    public MemoryPutByteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryPutByte;

    public DataFrame Frame { get; }

    public static MemoryPutByteRequest Create(ushort offset, byte value)
    {
        Span<byte> commandParameters = stackalloc byte[3];
        offset.WriteBytesBE(commandParameters);
        commandParameters[2] = value;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new MemoryPutByteRequest(frame);
    }

    public static MemoryPutByteRequest Create(DataFrame frame) => new MemoryPutByteRequest(frame);
}

public readonly struct MemoryPutByteResponse : ICommand<MemoryPutByteResponse>
{
    public MemoryPutByteResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryPutByte;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the byte was successfully written.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static MemoryPutByteResponse Create(DataFrame frame) => new MemoryPutByteResponse(frame);
}
