namespace ZWave.Serial.Commands;

/// <summary>
/// Get the list of supported radio debug protocols.
/// </summary>
public readonly struct RadioDebugGetProtocolListRequest : ICommand<RadioDebugGetProtocolListRequest>
{
    public RadioDebugGetProtocolListRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RadioDebugGetProtocolList;

    public DataFrame Frame { get; }

    public static RadioDebugGetProtocolListRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId);
        return new RadioDebugGetProtocolListRequest(frame);
    }

    public static RadioDebugGetProtocolListRequest Create(DataFrame frame) => new RadioDebugGetProtocolListRequest(frame);
}

public readonly struct RadioDebugGetProtocolListResponse : ICommand<RadioDebugGetProtocolListResponse>
{
    public RadioDebugGetProtocolListResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.RadioDebugGetProtocolList;

    public DataFrame Frame { get; }

    /// <summary>
    /// The version of Radio Debug commands (0xE6, 0xE7, 0xE8).
    /// </summary>
    public byte RadioDebugCommandsVersion => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The number of supported radio debug protocols.
    /// </summary>
    public int ProtocolCount => Frame.CommandParameters.Span[1];

    /// <summary>
    /// Gets the supported radio debug protocol at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the protocol.</param>
    public DebugInterfaceProtocol GetProtocol(int index)
    {
        if (index < 0 || index >= ProtocolCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int offset = 2 + (index * 2);
        return (DebugInterfaceProtocol)Frame.CommandParameters.Span[offset..(offset + 2)].ToUInt16BE();
    }

    public static RadioDebugGetProtocolListResponse Create(DataFrame frame) => new RadioDebugGetProtocolListResponse(frame);
}
