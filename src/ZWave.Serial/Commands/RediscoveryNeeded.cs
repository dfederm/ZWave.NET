namespace ZWave.Serial.Commands;

/// <summary>
/// Request a SUC/SIS controller to update the requesting nodes neighbors.
/// </summary>
public readonly struct RediscoveryNeededRequest : IRequestWithCallback<RediscoveryNeededRequest>
{
    public RediscoveryNeededRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RediscoveryNeeded;

    public static bool ExpectsResponseStatus => false;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static RediscoveryNeededRequest Create(
        ushort nodeId,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RediscoveryNeededRequest(frame);
    }

    public static RediscoveryNeededRequest Create(DataFrame frame) => new RediscoveryNeededRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RediscoveryNeededRequest"/> command.
/// </summary>
public readonly struct RediscoveryNeededCallback : ICommand<RediscoveryNeededCallback>
{
    public RediscoveryNeededCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RediscoveryNeeded;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the rediscovery request.
    /// </summary>
    public byte Status => Frame.CommandParameters.Span[1];

    public static RediscoveryNeededCallback Create(DataFrame frame) => new RediscoveryNeededCallback(frame);
}
