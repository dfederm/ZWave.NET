namespace ZWave.Serial.Commands;

/// <summary>
/// Copy a number of bytes from a RAM buffer to the application area of the NVM.
/// </summary>
public readonly struct MemoryPutBufferRequest : IRequestWithCallback<MemoryPutBufferRequest>
{
    public MemoryPutBufferRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryPutBuffer;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[^1];

    /// <summary>
    /// Create a request to write a buffer to NVM.
    /// </summary>
    /// <param name="offset">Address offset into host application NVM memory array.</param>
    /// <param name="data">The data to write.</param>
    /// <param name="sessionId">The session ID for correlating the callback.</param>
    public static MemoryPutBufferRequest Create(ushort offset, ReadOnlySpan<byte> data, byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[4 + data.Length + 1];
        offset.WriteBytesBE(commandParameters);
        ((ushort)data.Length).WriteBytesBE(commandParameters[2..]);
        data.CopyTo(commandParameters[4..]);
        commandParameters[^1] = sessionId;

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new MemoryPutBufferRequest(frame);
    }

    public static MemoryPutBufferRequest Create(DataFrame frame, CommandParsingContext context) => new MemoryPutBufferRequest(frame);
}

/// <summary>
/// Response to a <see cref="MemoryPutBufferRequest"/> command.
/// </summary>
/// <remarks>
/// retVal meanings: 0 = error, 1 = OK (NVM no change), &gt;= 2 = OK (NVM data bytes written + 1).
/// </remarks>
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
    /// The raw return value. 0 = error, 1 = OK (NVM no change), &gt;= 2 = OK (NVM data bytes written + 1).
    /// </summary>
    public byte ReturnValue => Frame.CommandParameters.Span[0];

    /// <summary>
    /// Indicates whether the buffer was successfully written.
    /// </summary>
    public bool Success => ReturnValue != 0;

    public static MemoryPutBufferResponse Create(DataFrame frame, CommandParsingContext context) => new MemoryPutBufferResponse(frame);
}

/// <summary>
/// Callback for the <see cref="MemoryPutBufferRequest"/> command.
/// </summary>
public readonly struct MemoryPutBufferCallback : ICommand<MemoryPutBufferCallback>
{
    public MemoryPutBufferCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryPutBuffer;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    public static MemoryPutBufferCallback Create(DataFrame frame, CommandParsingContext context) => new MemoryPutBufferCallback(frame);
}
