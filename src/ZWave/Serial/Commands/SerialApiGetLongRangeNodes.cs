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
    /// The node ID offset for the current segment.
    /// </summary>
    public byte NodeIdOffset => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The length of the node list bitmask.
    /// </summary>
    public byte NodeListLength => Frame.CommandParameters.Span[2];

    /// <summary>
    /// The set of Long Range node IDs parsed from the bitmask.
    /// </summary>
    public HashSet<byte> NodeIds
    {
        get
        {
            byte nodeListLength = NodeListLength;
            byte nodeIdOffset = NodeIdOffset;
            var bitMask = Frame.CommandParameters.Span.Slice(3, nodeListLength);
            var nodeIds = new HashSet<byte>(nodeListLength * 8);

            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        byte nodeId = (byte)((byteNum << 3) + bitNum + 1 + nodeIdOffset);
                        nodeIds.Add(nodeId);
                    }
                }
            }

            return nodeIds;
        }
    }

    public static SerialApiGetLongRangeNodesResponse Create(DataFrame frame) => new SerialApiGetLongRangeNodesResponse(frame);
}
