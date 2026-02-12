namespace ZWave.Serial.Commands;

/// <summary>
/// Checks if a node is a Virtual Slave node.
/// </summary>
public readonly struct IsVirtualNodeRequest : ICommand<IsVirtualNodeRequest>
{
    public IsVirtualNodeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.IsVirtualNode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to check if a node is virtual.
    /// </summary>
    /// <param name="nodeId">The node ID to check.</param>
    public static IsVirtualNodeRequest Create(byte nodeId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new IsVirtualNodeRequest(frame);
    }

    public static IsVirtualNodeRequest Create(DataFrame frame) => new IsVirtualNodeRequest(frame);
}

public readonly struct IsVirtualNodeResponse : ICommand<IsVirtualNodeResponse>
{
    public IsVirtualNodeResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.IsVirtualNode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the node is a Virtual Slave node.
    /// </summary>
    public bool IsVirtual => Frame.CommandParameters.Span[0] != 0;

    public static IsVirtualNodeResponse Create(DataFrame frame) => new IsVirtualNodeResponse(frame);
}
