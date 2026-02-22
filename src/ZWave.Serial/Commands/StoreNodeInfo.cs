namespace ZWave.Serial.Commands;

/// <summary>
/// Restore protocol node information from a backup.
/// </summary>
public readonly struct StoreNodeInfoRequest : ICommand<StoreNodeInfoRequest>
{
    public StoreNodeInfoRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.StoreNodeInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to store node information.
    /// </summary>
    /// <param name="nodeId">The node ID to store information for.</param>
    /// <param name="nodeInfo">The node information data.</param>
    public static StoreNodeInfoRequest Create(ushort nodeId, ReadOnlySpan<byte> nodeInfo)
    {
        Span<byte> commandParameters = stackalloc byte[1 + nodeInfo.Length];
        commandParameters[0] = (byte)nodeId;
        nodeInfo.CopyTo(commandParameters[1..]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new StoreNodeInfoRequest(frame);
    }

    public static StoreNodeInfoRequest Create(DataFrame frame) => new StoreNodeInfoRequest(frame);
}

public readonly struct StoreNodeInfoResponse : ICommand<StoreNodeInfoResponse>
{
    public StoreNodeInfoResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.StoreNodeInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// The status of the store operation.
    /// </summary>
    public byte Status => Frame.CommandParameters.Span[0];

    public static StoreNodeInfoResponse Create(DataFrame frame) => new StoreNodeInfoResponse(frame);
}
