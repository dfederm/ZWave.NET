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
    public HashSet<ushort> NodeIds
    {
        get
        {
            ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
            return CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);
        }
    }

    public static GetVirtualNodesResponse Create(DataFrame frame) => new GetVirtualNodesResponse(frame);
}
