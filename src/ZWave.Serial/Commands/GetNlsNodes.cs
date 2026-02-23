namespace ZWave.Serial.Commands;

/// <summary>
/// Request the list of Z-Wave NLS nodes.
/// </summary>
public readonly struct GetNlsNodesRequest : ICommand<GetNlsNodesRequest>
{
    public GetNlsNodesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetNlsNodes;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to get NLS nodes starting from a bitmask offset.
    /// </summary>
    /// <param name="startOffset">The NLS nodes list start offset, in units of 128 bytes.</param>
    public static GetNlsNodesRequest Create(byte startOffset)
    {
        ReadOnlySpan<byte> commandParameters = [startOffset];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetNlsNodesRequest(frame);
    }

    public static GetNlsNodesRequest Create(DataFrame frame, CommandParsingContext context) => new GetNlsNodesRequest(frame);
}

public readonly struct GetNlsNodesResponse : ICommand<GetNlsNodesResponse>
{
    public GetNlsNodesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetNlsNodes;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether there are more nodes to retrieve (bit 0 of first byte).
    /// </summary>
    public bool MoreNodes => (Frame.CommandParameters.Span[0] & 0x01) != 0;

    /// <summary>
    /// The NLS nodes list start offset, in units of 128 bytes.
    /// </summary>
    public byte StartOffset => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The NLS node list data.
    /// </summary>
    public ReadOnlyMemory<byte> NodeList => Frame.CommandParameters.Slice(3, Frame.CommandParameters.Span[2]);

    public static GetNlsNodesResponse Create(DataFrame frame, CommandParsingContext context) => new GetNlsNodesResponse(frame);
}
