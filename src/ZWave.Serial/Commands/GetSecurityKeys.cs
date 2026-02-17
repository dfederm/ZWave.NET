namespace ZWave.Serial.Commands;

/// <summary>
/// Returns a bitmask of security keys the node possesses.
/// </summary>
public readonly struct GetSecurityKeysRequest : ICommand<GetSecurityKeysRequest>
{
    public GetSecurityKeysRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetSecurityKeys;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to get the security keys for a node.
    /// </summary>
    /// <param name="nodeId">The node ID to query.</param>
    public static GetSecurityKeysRequest Create(byte nodeId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetSecurityKeysRequest(frame);
    }

    public static GetSecurityKeysRequest Create(DataFrame frame) => new GetSecurityKeysRequest(frame);
}

public readonly struct GetSecurityKeysResponse : ICommand<GetSecurityKeysResponse>
{
    public GetSecurityKeysResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetSecurityKeys;

    public DataFrame Frame { get; }

    /// <summary>
    /// The bitmask of security keys the node possesses.
    /// </summary>
    public byte SecurityKeys => Frame.CommandParameters.Span[0];

    public static GetSecurityKeysResponse Create(DataFrame frame) => new GetSecurityKeysResponse(frame);
}
