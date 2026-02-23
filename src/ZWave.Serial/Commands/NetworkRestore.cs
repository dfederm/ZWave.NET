namespace ZWave.Serial.Commands;

/// <summary>
/// Network restore sub-commands.
/// </summary>
public enum NetworkRestoreSubCommand : byte
{
    Prepare = 0x00,
    RestoreHomeIdAndNodeId = 0x01,
    RestoreDevice = 0x02,
    RestoreNeighbours = 0x03,
    RestoreRoutingEntries = 0x04,
    Finalize = 0xFF,
}

/// <summary>
/// Status of a network restore operation.
/// </summary>
public enum NetworkRestoreStatus : byte
{
    OK = 0x00,
    Error = 0x01,
    LongRangeNotSupported = 0x02,
    SubCommandNotSupported = 0x04,
}

/// <summary>
/// Write the firmware data of the Z-Wave API Module in an implementation-independent way.
/// </summary>
public readonly partial struct NetworkRestoreRequest : ICommand<NetworkRestoreRequest>
{
    public NetworkRestoreRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NetworkRestore;

    public DataFrame Frame { get; }

    private static NetworkRestoreRequest Create(NetworkRestoreSubCommand subCommand, ReadOnlySpan<byte> subCommandParameters)
    {
        Span<byte> commandParameters = stackalloc byte[subCommandParameters.Length + 1];
        commandParameters[0] = (byte)subCommand;
        subCommandParameters.CopyTo(commandParameters[1..]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NetworkRestoreRequest(frame);
    }

    /// <summary>
    /// Create a request to prepare the network restore operation.
    /// </summary>
    public static NetworkRestoreRequest Prepare()
        => Create(NetworkRestoreSubCommand.Prepare, []);

    /// <summary>
    /// Create a request to restore the Home ID and controller Node ID.
    /// </summary>
    /// <param name="homeId">The Home ID to restore (big-endian).</param>
    /// <param name="controllerNodeId">The controller Node ID (always 1 byte per spec).</param>
    public static NetworkRestoreRequest RestoreHomeIdAndNodeId(uint homeId, byte controllerNodeId)
    {
        Span<byte> subCommandParameters = stackalloc byte[5];
        homeId.WriteBytesBE(subCommandParameters[..4]);
        subCommandParameters[4] = controllerNodeId;
        return Create(NetworkRestoreSubCommand.RestoreHomeIdAndNodeId, subCommandParameters);
    }

    /// <summary>
    /// Create a request to restore a device.
    /// </summary>
    /// <param name="nodeId">The node ID of the device.</param>
    /// <param name="protocolSpecific">Protocol-specific data (5 bytes).</param>
    public static NetworkRestoreRequest RestoreDevice(ushort nodeId, ReadOnlySpan<byte> protocolSpecific)
    {
        Span<byte> subCommandParameters = stackalloc byte[1 + protocolSpecific.Length];
        subCommandParameters[0] = (byte)nodeId; // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
        protocolSpecific.CopyTo(subCommandParameters[1..]);
        return Create(NetworkRestoreSubCommand.RestoreDevice, subCommandParameters);
    }

    /// <summary>
    /// Create a request to restore the neighbours for a node.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <param name="routingTableLine">The routing table line (29 bytes = 232 bits).</param>
    public static NetworkRestoreRequest RestoreNeighbours(ushort nodeId, ReadOnlySpan<byte> routingTableLine)
    {
        Span<byte> subCommandParameters = stackalloc byte[1 + routingTableLine.Length];
        subCommandParameters[0] = (byte)nodeId; // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
        routingTableLine.CopyTo(subCommandParameters[1..]);
        return Create(NetworkRestoreSubCommand.RestoreNeighbours, subCommandParameters);
    }

    /// <summary>
    /// Create a request to restore routing entries for a node.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <param name="routingEntries">The routing entries to restore.</param>
    public static NetworkRestoreRequest RestoreRoutingEntries(ushort nodeId, ReadOnlySpan<RoutingTableEntry> routingEntries)
    {
        Span<byte> subCommandParameters = stackalloc byte[2 + (routingEntries.Length * RoutingTableEntry.Size)];
        subCommandParameters[0] = (byte)nodeId; // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
        subCommandParameters[1] = (byte)routingEntries.Length;
        for (int i = 0; i < routingEntries.Length; i++)
        {
            routingEntries[i].WriteTo(subCommandParameters[(2 + (i * RoutingTableEntry.Size))..]);
        }

        return Create(NetworkRestoreSubCommand.RestoreRoutingEntries, subCommandParameters);
    }

    /// <summary>
    /// Create a request to finalize the network restore operation.
    /// </summary>
    public static NetworkRestoreRequest Finalize()
        => Create(NetworkRestoreSubCommand.Finalize, []);

    public static NetworkRestoreRequest Create(DataFrame frame) => new NetworkRestoreRequest(frame);
}

/// <summary>
/// Response to a <see cref="NetworkRestoreRequest"/> command.
/// </summary>
public readonly struct NetworkRestoreResponse : ICommand<NetworkRestoreResponse>
{
    public NetworkRestoreResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NetworkRestore;

    public DataFrame Frame { get; }

    /// <summary>
    /// The sub-command this response corresponds to.
    /// </summary>
    public NetworkRestoreSubCommand SubCommand => (NetworkRestoreSubCommand)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the network restore operation.
    /// </summary>
    public NetworkRestoreStatus Status => (NetworkRestoreStatus)Frame.CommandParameters.Span[1];

    public static NetworkRestoreResponse Create(DataFrame frame) => new NetworkRestoreResponse(frame);
}
