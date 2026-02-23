namespace ZWave.Serial.Commands;

/// <summary>
/// Enable the state of network layer security (NLS) of an included node.
/// NLS state cannot be disabled after it has been enabled.
/// </summary>
public readonly struct EnableNodeNlsRequest : ICommand<EnableNodeNlsRequest>
{
    public EnableNodeNlsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.EnableNodeNls;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to enable NLS for a node.
    /// </summary>
    /// <param name="nodeId">The node ID to enable NLS for.</param>
    public static EnableNodeNlsRequest Create(ushort nodeId, NodeIdType nodeIdType)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize];
        nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new EnableNodeNlsRequest(frame);
    }

    public static EnableNodeNlsRequest Create(DataFrame frame, CommandParsingContext context) => new EnableNodeNlsRequest(frame);
}

public readonly struct EnableNodeNlsResponse : ICommand<EnableNodeNlsResponse>
{
    public EnableNodeNlsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.EnableNodeNls;

    public DataFrame Frame { get; }

    /// <summary>
    /// The command status (0x01 = success, 0x00 = failure).
    /// </summary>
    public byte CommandStatus => Frame.CommandParameters.Span[0];

    public static EnableNodeNlsResponse Create(DataFrame frame, CommandParsingContext context) => new EnableNodeNlsResponse(frame);
}
