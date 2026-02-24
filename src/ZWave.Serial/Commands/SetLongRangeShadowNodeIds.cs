namespace ZWave.Serial.Commands;

/// <summary>
/// Enable the use of Shadow NodeIDs in the Long Range capable controller.
/// </summary>
public readonly struct SetLongRangeShadowNodeIdsRequest : ICommand<SetLongRangeShadowNodeIdsRequest>
{
    public SetLongRangeShadowNodeIdsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetLongRangeShadowNodeIds;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set Long Range virtual node IDs.
    /// </summary>
    /// <param name="nodeIdBitmask">The node ID bitmask. Bits 0-3 enable node IDs 4002-4005.</param>
    public static SetLongRangeShadowNodeIdsRequest Create(byte nodeIdBitmask)
    {
        ReadOnlySpan<byte> commandParameters = [nodeIdBitmask];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetLongRangeShadowNodeIdsRequest(frame);
    }

    public static SetLongRangeShadowNodeIdsRequest Create(DataFrame frame, CommandParsingContext context) => new SetLongRangeShadowNodeIdsRequest(frame);
}
