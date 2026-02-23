namespace ZWave.Serial.Commands;

/// <summary>
/// Get the state of network layer security (NLS) on a node.
/// </summary>
public readonly struct GetNodeNlsStateRequest : ICommand<GetNodeNlsStateRequest>
{
    public GetNodeNlsStateRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetNodeNlsState;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to get the NLS state for a node.
    /// </summary>
    /// <param name="nodeId">The node ID to query.</param>
    public static GetNodeNlsStateRequest Create(ushort nodeId, NodeIdType nodeIdType)
    {
        int nodeIdSize = nodeIdType.NodeIdSize();
        Span<byte> commandParameters = stackalloc byte[nodeIdSize];
        nodeIdType.WriteNodeId(commandParameters, 0, nodeId);
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetNodeNlsStateRequest(frame);
    }

    public static GetNodeNlsStateRequest Create(DataFrame frame, CommandParsingContext context) => new GetNodeNlsStateRequest(frame);
}

public readonly struct GetNodeNlsStateResponse : ICommand<GetNodeNlsStateResponse>
{
    public GetNodeNlsStateResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetNodeNlsState;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether NLS is supported on the node.
    /// </summary>
    /// <remarks>
    /// 0x00 = NLS not supported, 0x01 = NLS supported. All other values are reserved.
    /// </remarks>
    public bool IsNlsSupported => (Frame.CommandParameters.Span[0] & 0x01) != 0;

    /// <summary>
    /// Indicates whether NLS is enabled on the node.
    /// </summary>
    /// <remarks>
    /// 0x00 = NLS disabled, 0x01 = NLS enabled. All other values are reserved.
    /// When queried with the controller's own node ID, this field is set to 1 if
    /// one or more nodes have NLS enabled on the network.
    /// </remarks>
    public bool IsNlsEnabled => (Frame.CommandParameters.Span[1] & 0x01) != 0;

    public static GetNodeNlsStateResponse Create(DataFrame frame, CommandParsingContext context) => new GetNodeNlsStateResponse(frame);
}
