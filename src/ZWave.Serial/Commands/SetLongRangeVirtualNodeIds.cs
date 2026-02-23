namespace ZWave.Serial.Commands;

/// <summary>
/// Enable the use of Shadow NodeIDs in the Long Range capable controller.
/// </summary>
public readonly struct SetLongRangeVirtualNodeIdsRequest : ICommand<SetLongRangeVirtualNodeIdsRequest>
{
    public SetLongRangeVirtualNodeIdsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetLongRangeVirtualNodeIds;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set Long Range virtual node IDs.
    /// </summary>
    /// <param name="nodeIdBitmask">The node ID bitmask. Bits 0-3 enable node IDs 4002-4005.</param>
    public static SetLongRangeVirtualNodeIdsRequest Create(byte nodeIdBitmask)
    {
        ReadOnlySpan<byte> commandParameters = [nodeIdBitmask];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetLongRangeVirtualNodeIdsRequest(frame);
    }

    public static SetLongRangeVirtualNodeIdsRequest Create(DataFrame frame, CommandParsingContext context) => new SetLongRangeVirtualNodeIdsRequest(frame);
}
