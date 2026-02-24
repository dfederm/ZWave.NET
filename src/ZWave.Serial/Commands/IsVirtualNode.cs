namespace ZWave.Serial.Commands;

/// <summary>
/// Checks if a node is a Virtual Node.
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
    /// <remarks>
    /// Per Z-Wave Host API Specification, this field MUST be encoded using 8 bits regardless
    /// of the configured NodeID base Type.
    /// </remarks>
    public static IsVirtualNodeRequest Create(ushort nodeId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nodeId, (ushort)0xFF);
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new IsVirtualNodeRequest(frame);
    }

    public static IsVirtualNodeRequest Create(DataFrame frame, CommandParsingContext context) => new IsVirtualNodeRequest(frame);
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
    /// Indicates whether the node is a Virtual Node.
    /// </summary>
    public bool IsVirtual => Frame.CommandParameters.Span[0] != 0;

    public static IsVirtualNodeResponse Create(DataFrame frame, CommandParsingContext context) => new IsVirtualNodeResponse(frame);
}
