namespace ZWave.Serial.Commands;

/// <summary>
/// Request a buffer containing available Virtual Slave nodes in the Z-Wave network.
/// </summary>
public readonly struct GetVirtualNodesRequest : ICommand<GetVirtualNodesRequest>
{
    public GetVirtualNodesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetVirtualNodes;

    public DataFrame Frame { get; }

    public static GetVirtualNodesRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetVirtualNodesRequest(frame);
    }

    public static GetVirtualNodesRequest Create(DataFrame frame) => new GetVirtualNodesRequest(frame);
}

public readonly struct GetVirtualNodesResponse : ICommand<GetVirtualNodesResponse>
{
    public GetVirtualNodesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetVirtualNodes;

    public DataFrame Frame { get; }

    /// <summary>
    /// The bitmask of available Virtual Slave nodes.
    /// </summary>
    public ReadOnlyMemory<byte> NodeBitmask => Frame.CommandParameters;

    /// <summary>
    /// The set of Virtual Slave node IDs parsed from the bitmask.
    /// </summary>
    public HashSet<byte> NodeIds
    {
        get
        {
            ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
            var nodeIds = new HashSet<byte>(bitMask.Length * 8);

            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        byte nodeId = (byte)((byteNum << 3) + bitNum + 1);
                        nodeIds.Add(nodeId);
                    }
                }
            }

            return nodeIds;
        }
    }

    public static GetVirtualNodesResponse Create(DataFrame frame) => new GetVirtualNodesResponse(frame);
}
