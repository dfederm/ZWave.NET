namespace ZWave.Serial.Commands;

/// <summary>
/// Request a buffer containing available Virtual Nodes in the Z-Wave network.
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

    public static GetVirtualNodesRequest Create(DataFrame frame, CommandParsingContext context) => new GetVirtualNodesRequest(frame);
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
    /// The bitmask of available Virtual Nodes.
    /// </summary>
    public ReadOnlyMemory<byte> NodeBitmask => Frame.CommandParameters;

    /// <summary>
    /// The set of Virtual Node IDs parsed from the bitmask.
    /// </summary>
    public IReadOnlySet<ushort> NodeIds
    {
        get
        {
            ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span;
            return CommandDataParsingHelpers.ParseNodeBitmask(bitMask, baseNodeId: 1);
        }
    }

    public static GetVirtualNodesResponse Create(DataFrame frame, CommandParsingContext context) => new GetVirtualNodesResponse(frame);
}
