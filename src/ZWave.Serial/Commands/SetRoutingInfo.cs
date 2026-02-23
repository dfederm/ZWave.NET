namespace ZWave.Serial.Commands;

/// <summary>
/// Overwrite the current neighbor information for a given node ID in the protocol locally.
/// </summary>
public readonly struct SetRoutingInfoRequest : ICommand<SetRoutingInfoRequest>
{
    public SetRoutingInfoRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetRoutingInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the routing info for a node.
    /// </summary>
    /// <param name="nodeId">The node ID to update.</param>
    /// <param name="neighborMask">The 29-byte bitmask of neighbors (232 bits for 232 nodes).</param>
    /// <remarks>
    /// This command writes the neighbor bitmask which is structurally limited to classic Z-Wave
    /// nodes (1–232). The NodeID field is always 8 bits.
    /// </remarks>
    public static SetRoutingInfoRequest Create(ushort nodeId, ReadOnlySpan<byte> neighborMask)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nodeId, (ushort)0xFF);
        Span<byte> commandParameters = stackalloc byte[1 + neighborMask.Length];
        commandParameters[0] = (byte)nodeId;
        neighborMask.CopyTo(commandParameters[1..]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetRoutingInfoRequest(frame);
    }

    public static SetRoutingInfoRequest Create(DataFrame frame, CommandParsingContext context) => new SetRoutingInfoRequest(frame);
}

public readonly struct SetRoutingInfoResponse : ICommand<SetRoutingInfoResponse>
{
    public SetRoutingInfoResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetRoutingInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// The return value from the operation.
    /// </summary>
    public byte RetVal => Frame.CommandParameters.Span[0];

    public static SetRoutingInfoResponse Create(DataFrame frame, CommandParsingContext context) => new SetRoutingInfoResponse(frame);
}
