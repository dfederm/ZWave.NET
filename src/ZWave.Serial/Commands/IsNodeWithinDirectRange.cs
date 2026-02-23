namespace ZWave.Serial.Commands;

/// <summary>
/// Check if the supplied node ID is marked as being within direct range in any of the existing return routes.
/// </summary>
public readonly struct IsNodeWithinDirectRangeRequest : ICommand<IsNodeWithinDirectRangeRequest>
{
    public IsNodeWithinDirectRangeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.IsNodeWithinDirectRange;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to check if a node is within direct range.
    /// </summary>
    /// <param name="nodeId">The node ID to check.</param>
    /// <remarks>
    /// Per Z-Wave Host API Specification, this field MUST be encoded using 8 bits regardless
    /// of the configured NodeID base Type.
    /// </remarks>
    public static IsNodeWithinDirectRangeRequest Create(ushort nodeId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nodeId, (ushort)0xFF);
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new IsNodeWithinDirectRangeRequest(frame);
    }

    public static IsNodeWithinDirectRangeRequest Create(DataFrame frame, CommandParsingContext context) => new IsNodeWithinDirectRangeRequest(frame);
}

public readonly struct IsNodeWithinDirectRangeResponse : ICommand<IsNodeWithinDirectRangeResponse>
{
    public IsNodeWithinDirectRangeResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.IsNodeWithinDirectRange;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the node is within direct range.
    /// </summary>
    public bool IsWithinRange => Frame.CommandParameters.Span[0] != 0;

    public static IsNodeWithinDirectRangeResponse Create(DataFrame frame, CommandParsingContext context) => new IsNodeWithinDirectRangeResponse(frame);
}
