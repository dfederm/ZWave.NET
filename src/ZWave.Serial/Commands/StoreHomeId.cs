namespace ZWave.Serial.Commands;

/// <summary>
/// Restore HomeID and NodeID information from a backup.
/// </summary>
/// <remarks>
/// This is a fire-and-forget command with no response per INS13954.
/// The restored values will not take effect before the Z-Wave module has been reset.
/// </remarks>
public readonly struct StoreHomeIdRequest : ICommand<StoreHomeIdRequest>
{
    public StoreHomeIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.StoreHomeId;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to store the Home ID and Node ID.
    /// </summary>
    /// <param name="homeId">The Home ID to store (4 bytes, big-endian).</param>
    /// <param name="nodeId">The Node ID to store.</param>
    public static StoreHomeIdRequest Create(uint homeId, ushort nodeId, NodeIdType nodeIdType)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[4 + nodeIdSize];
        homeId.WriteBytesBE(commandParameters);
        nodeIdType.WriteNodeId(commandParameters, 4, nodeId);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new StoreHomeIdRequest(frame);
    }

    public static StoreHomeIdRequest Create(DataFrame frame, CommandParsingContext context) => new StoreHomeIdRequest(frame);
}
