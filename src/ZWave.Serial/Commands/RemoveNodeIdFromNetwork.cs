namespace ZWave.Serial.Commands;

/// <summary>
/// Remove a specific node from a Z-Wave network.
/// </summary>
public readonly struct RemoveNodeIdFromNetworkRequest : ICommand<RemoveNodeIdFromNetworkRequest>
{
    public RemoveNodeIdFromNetworkRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RemoveNodeIdFromNetwork;

    public DataFrame Frame { get; }

    public static RemoveNodeIdFromNetworkRequest Create(
        bool isHighPower,
        bool isNetworkWide,
        RemoveNodeMode removeMode,
        byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[2];

        if (isHighPower)
        {
            commandParameters[0] |= 0b1000_0000;
        }

        if (isNetworkWide)
        {
            commandParameters[0] |= 0b0100_0000;
        }

        commandParameters[0] |= (byte)removeMode;

        commandParameters[1] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RemoveNodeIdFromNetworkRequest(frame);
    }

    public static RemoveNodeIdFromNetworkRequest Create(DataFrame frame) => new RemoveNodeIdFromNetworkRequest(frame);
}

/// <summary>
/// Callback for the <see cref="RemoveNodeIdFromNetworkRequest"/> command.
/// </summary>
public readonly struct RemoveNodeIdFromNetworkCallback : ICommand<RemoveNodeIdFromNetworkCallback>
{
    public RemoveNodeIdFromNetworkCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RemoveNodeIdFromNetwork;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the removal operation.
    /// </summary>
    public RemoveNodeStatus Status => (RemoveNodeStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The node ID of the removed node.
    /// </summary>
    public ushort NodeId => Frame.CommandParameters.Span[2];

    public static RemoveNodeIdFromNetworkCallback Create(DataFrame frame) => new RemoveNodeIdFromNetworkCallback(frame);
}
