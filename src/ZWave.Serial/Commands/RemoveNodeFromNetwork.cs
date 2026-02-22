namespace ZWave.Serial.Commands;

public enum RemoveNodeMode : byte
{
    /// <summary>
    /// Remove any node.
    /// </summary>
    Any = 0x01,

    /// <summary>
    /// Remove controller node.
    /// </summary>
    [Obsolete($"Deprecated: Use {nameof(RemoveNodeMode)}.{nameof(Any)} instead.")]
    Controller = 0x02,

    /// <summary>
    /// Remove End Node.
    /// </summary>
    [Obsolete($"Deprecated: Use {nameof(RemoveNodeMode)}.{nameof(Any)} instead.")]
    EndNode = 0x03,

    /// <summary>
    /// Stop network exclusion
    /// </summary>
    StopNetworkExclusion = 0x05,
}

public enum RemoveNodeStatus : byte
{
    /// <summary>
    /// The Z-Wave Module has initiated Network exclusion and is ready to remove existing nodes.
    /// </summary>
    NetworkExclusionStarted = 0x01,

    /// <summary>
    /// A node requesting exclusion has been found and the node removal operation is initiated.
    /// </summary>
    NodeFound = 0x02,

    /// <summary>
    /// The network exclusion is ongoing with an End Node.
    /// </summary>
    ExclusionOngoingEndNode = 0x03,

    /// <summary>
    /// The network exclusion is ongoing with a Controller node.
    /// </summary>
    ExclusionOngoingController = 0x04,

    /// <summary>
    /// Node removal operation is completed.
    /// </summary>
    ExclusionCompleted = 0x06,

    /// <summary>
    /// Removal node operation is failed.
    /// </summary>
    ExclusionFailed = 0x07,

    /// <summary>
    /// The node exclusion operation cannot be performed because the Z-Wave API Module does not have the Primary
    /// Controller role and the SIS functionality is not available in the current network
    /// </summary>
    NotPrimary = 0x23,
}

public readonly struct RemoveNodeFromNetworkRequest : ICommand<RemoveNodeFromNetworkRequest>
{
    public RemoveNodeFromNetworkRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RemoveNodeFromNetwork;

    public DataFrame Frame { get; }

    public static RemoveNodeFromNetworkRequest Create(
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
        return new RemoveNodeFromNetworkRequest(frame);
    }

    public static RemoveNodeFromNetworkRequest Create(DataFrame frame) => new RemoveNodeFromNetworkRequest(frame);
}

public readonly struct RemoveNodeFromNetworkCallback : ICommand<RemoveNodeFromNetworkCallback>
{
    public RemoveNodeFromNetworkCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RemoveNodeFromNetwork;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public RemoveNodeStatus Status => (RemoveNodeStatus)Frame.CommandParameters.Span[1];

    public ushort NodeId => Frame.CommandParameters.Span[2];

    public static RemoveNodeFromNetworkCallback Create(DataFrame frame) => new RemoveNodeFromNetworkCallback(frame);
}
