namespace ZWave.Serial.Commands;

/// <summary>
/// Read one byte from the NVM allocated for the application.
/// </summary>
public readonly struct MemoryGetByteRequest : ICommand<MemoryGetByteRequest>
{
    public MemoryGetByteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryGetByte;

    public DataFrame Frame { get; }

    public static MemoryGetByteRequest Create(ushort offset)
    {
        Span<byte> commandParameters = stackalloc byte[2];
        offset.WriteBytesBE(commandParameters);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new MemoryGetByteRequest(frame);
    }

    public static MemoryGetByteRequest Create(DataFrame frame) => new MemoryGetByteRequest(frame);
}

public readonly struct MemoryGetByteResponse : ICommand<MemoryGetByteResponse>
{
    public MemoryGetByteResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryGetByte;

    public DataFrame Frame { get; }

    /// <summary>
    /// The value read from NVM.
    /// </summary>
    public byte Value => Frame.CommandParameters.Span[0];

    public static MemoryGetByteResponse Create(DataFrame frame) => new MemoryGetByteResponse(frame);
}
