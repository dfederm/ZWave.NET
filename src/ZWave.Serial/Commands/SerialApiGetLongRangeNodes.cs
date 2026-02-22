namespace ZWave.Serial.Commands;

/// <summary>
/// Obtain the list of Long Range nodes.
/// </summary>
public readonly struct SerialApiGetLongRangeNodesRequest : ICommand<SerialApiGetLongRangeNodesRequest>
{
    public SerialApiGetLongRangeNodesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiGetLongRangeNodes;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to get Long Range nodes.
    /// </summary>
    /// <param name="segmentOffset">The segment offset to start from.</param>
    public static SerialApiGetLongRangeNodesRequest Create(byte segmentOffset)
    {
        ReadOnlySpan<byte> commandParameters = [segmentOffset];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SerialApiGetLongRangeNodesRequest(frame);
    }

    public static SerialApiGetLongRangeNodesRequest Create(DataFrame frame) => new SerialApiGetLongRangeNodesRequest(frame);
}

public readonly struct SerialApiGetLongRangeNodesResponse : ICommand<SerialApiGetLongRangeNodesResponse>
{
    public SerialApiGetLongRangeNodesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiGetLongRangeNodes;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether there are more nodes to retrieve.
    /// </summary>
    public bool MoreNodes => Frame.CommandParameters.Span[0] != 0;

    /// <summary>
    /// The bitmask byte offset for the current segment (16-bit per spec).
    /// Per the Z-Wave Host API Specification, supported values correspond to byte offsets 0, 128, 256, 384.
    /// </summary>
    public ushort BitmaskOffset => Frame.CommandParameters.Span[1..3].ToUInt16BE();

    /// <summary>
    /// The length of the node list bitmask.
    /// </summary>
    public byte NodeListLength => Frame.CommandParameters.Span[3];

    /// <summary>
    /// The set of Long Range node IDs parsed from the bitmask.
    /// Per the Z-Wave Host API Specification, LR node IDs start at BASE=256.
    /// Node ID = BASE + (BITMASK_OFFSET + J) * 8 + N, where J is the byte index and N is the bit index.
    /// </summary>
    public HashSet<ushort> NodeIds
    {
        get
        {
            byte nodeListLength = NodeListLength;
            ushort bitmaskOffset = BitmaskOffset;
            ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(4, nodeListLength);

            // LR nodes start at 256. The bitmask offset is in bytes, so multiply by 8 to get the node offset.
            ushort baseNodeId = (ushort)(256 + bitmaskOffset * 8);
            return CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId);
        }
    }

    public static SerialApiGetLongRangeNodesResponse Create(DataFrame frame) => new SerialApiGetLongRangeNodesResponse(frame);
}
