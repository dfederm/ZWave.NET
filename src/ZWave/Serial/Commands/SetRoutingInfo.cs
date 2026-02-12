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
    /// <param name="speed">The speed setting for the node.</param>
    public static SetRoutingInfoRequest Create(byte nodeId, ReadOnlySpan<byte> neighborMask, byte speed)
    {
        Span<byte> commandParameters = stackalloc byte[1 + neighborMask.Length + 1];
        commandParameters[0] = nodeId;
        neighborMask.CopyTo(commandParameters[1..]);
        commandParameters[1 + neighborMask.Length] = speed;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetRoutingInfoRequest(frame);
    }

    public static SetRoutingInfoRequest Create(DataFrame frame) => new SetRoutingInfoRequest(frame);
}
