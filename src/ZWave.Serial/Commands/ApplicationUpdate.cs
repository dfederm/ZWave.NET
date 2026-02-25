namespace ZWave.Serial.Commands;

/// <summary>
/// Indicates which event has triggered the transmission of thie ApplicationControllerUpdate command.
/// </summary>
public enum ApplicationUpdateEvent
{
    /// <summary>
    /// The SIS NodeID has been updated.
    /// </summary>
    SucId = 0x10,

    /// <summary>
    /// A node has been deleted from the network.
    /// </summary>
    DeleteDone = 0x20,

    /// <summary>
    /// A node has been deleted from the network.
    /// </summary>
    NewIdAssigned = 0x40,

    /// <summary>
    /// Another node in the network has requested the Z-Wave API Module to perform a neighbor discovery.
    /// </summary>
    RoutingPending = 0x80,

    /// <summary>
    /// The issued Request Node Information Command has not been acknowledged by the destination
    /// </summary>
    NodeInfoRequestFailed = 0x81,

    /// <summary>
    /// The issued Request Node Information Command has been acknowledged by the destination.
    /// </summary>
    NodeInfoRequestDone = 0x82,

    /// <summary>
    /// Another node sent a NOP Power Command to the Z-Wave API Module.
    /// The host application SHOULD NOT power down the Z-Wave API Module.
    /// </summary>
    NopPowerReceived = 0x83,

    /// <summary>
    /// A Node Information Frame has been received as unsolicited frame or in response to a Request Node Information Command
    /// </summary>
    NodeInfoReceived = 0x84,

    /// <summary>
    /// A SmartStart Prime Command has been received using the Z-Wave protocol.
    /// </summary>
    NodeInfoSmartStartHomeIdReceived = 0x85,

    /// <summary>
    /// A SmartStart Included Node Information Frame has been received (using either Z-Wave or Z-Wave Long Range protocol).
    /// </summary>
    IncludedNodeInfoReceived = 0x86,

    /// <summary>
    /// A SmartStart Prime Command has been received using the Z-Wave Long Range protocol.
    /// </summary>
    NodeInfoSmartStartHomeIdReceivedLongRange = 0x87,
}

public readonly struct ApplicationUpdateRequest : ICommand<ApplicationUpdateRequest>
{
    private readonly NodeIdType _nodeIdType;

    public ApplicationUpdateRequest(DataFrame frame, NodeIdType nodeIdType)
    {
        Frame = frame;
        _nodeIdType = nodeIdType;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationUpdate;

    public DataFrame Frame { get; }

    public ApplicationUpdateEvent Event => (ApplicationUpdateEvent)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The generic data frame format used with most values of <see cref="Event"/>.
    /// </summary>
    /// <remarks>
    /// This only applies with specific values for <see cref="Event"/>. Using this with the wrong
    /// event type at best lead to garbled data and at worst lead to out of range exceptions.
    /// </remarks>
    public ApplicationUpdateGeneric Generic => new ApplicationUpdateGeneric(Frame.CommandParameters[1..], _nodeIdType);

    /// <summary>
    /// The data frame format when the <see cref="Event"/> is <see cref="ApplicationUpdateEvent.NodeInfoSmartStartHomeIdReceived"/>
    /// or <see cref="ApplicationUpdateEvent.NodeInfoSmartStartHomeIdReceivedLongRange"/>
    /// </summary>
    /// <remarks>
    /// This only applies with specific values for <see cref="Event"/>. Using this with the wrong
    /// event type at best lead to garbled data and at worst lead to out of range exceptions.
    /// </remarks>
    public ApplicationUpdateSmartStartPrime? SmartStartPrime => Event == ApplicationUpdateEvent.NodeInfoSmartStartHomeIdReceived
        ? new ApplicationUpdateSmartStartPrime(Frame.CommandParameters[1..], _nodeIdType)
        : null;

    /// <summary>
    /// The data frame format when the <see cref="Event"/> is <see cref="ApplicationUpdateEvent.IncludedNodeInfoReceived"/>.
    /// </summary>
    /// <remarks>
    /// This only applies with specific values for <see cref="Event"/>. Using this with the wrong
    /// event type at best lead to garbled data and at worst lead to out of range exceptions.
    /// </remarks>
    public ApplicationUpdateSmartStartIncludedNodeInfo? SmartStartIncludedNodeInfo => Event == ApplicationUpdateEvent.IncludedNodeInfoReceived
        ? new ApplicationUpdateSmartStartIncludedNodeInfo(Frame.CommandParameters[1..], _nodeIdType)
        : null;

    public static ApplicationUpdateRequest Create(DataFrame frame, CommandParsingContext context) => new ApplicationUpdateRequest(frame, context.NodeIdType);
}

public readonly struct ApplicationUpdateGeneric
{
    private readonly ReadOnlyMemory<byte> _data;

    private readonly NodeIdType _nodeIdType;

    public ApplicationUpdateGeneric(ReadOnlyMemory<byte> data, NodeIdType nodeIdType)
    {
        _data = data;
        _nodeIdType = nodeIdType;
    }

    public ushort NodeId => _nodeIdType.ReadNodeId(_data.Span, 0);

    public byte BasicDeviceClass => _data.Span[_nodeIdType.NodeIdSize() + 1];

    public byte GenericDeviceClass => _data.Span[_nodeIdType.NodeIdSize() + 2];

    public byte SpecificDeviceClass => _data.Span[_nodeIdType.NodeIdSize() + 3];

    /// <summary>
    /// The list of non-secure implemented Command Classes by the remote node.
    /// </summary>
    public IReadOnlyList<CommandClassInfo> CommandClasses
    {
        get
        {
            byte length = _data.Span[_nodeIdType.NodeIdSize()];
            ReadOnlySpan<byte> allCommandClasses = _data.Span.Slice(_nodeIdType.NodeIdSize() + 4, length);
            return CommandClassInfo.ParseList(allCommandClasses);
        }
    }
}

public readonly struct ApplicationUpdateSmartStartPrime
{
    private readonly ReadOnlyMemory<byte> _data;

    private readonly NodeIdType _nodeIdType;

    public ApplicationUpdateSmartStartPrime(ReadOnlyMemory<byte> data, NodeIdType nodeIdType)
    {
        _data = data;
        _nodeIdType = nodeIdType;
    }

    public ushort NodeId => _nodeIdType.ReadNodeId(_data.Span, 0);

    public ReceivedStatus ReceivedStatus => (ReceivedStatus)_data.Span[_nodeIdType.NodeIdSize()];

    /// <summary>
    /// The NWI HomeID on which the SmartStart Prime Command was received.
    /// </summary>
    public uint HomeId => _data.Span[(_nodeIdType.NodeIdSize() + 1)..(_nodeIdType.NodeIdSize() + 5)].ToUInt32BE();

    public byte BasicDeviceClass => _data.Span[_nodeIdType.NodeIdSize() + 6];

    public byte GenericDeviceClass => _data.Span[_nodeIdType.NodeIdSize() + 7];

    public byte SpecificDeviceClass => _data.Span[_nodeIdType.NodeIdSize() + 8];

    /// <summary>
    /// The list of non-secure implemented Command Classes by the remote node.
    /// </summary>
    public IReadOnlyList<CommandClassInfo> CommandClasses
    {
        get
        {
            byte length = _data.Span[_nodeIdType.NodeIdSize() + 5];
            ReadOnlySpan<byte> allCommandClasses = _data.Span.Slice(_nodeIdType.NodeIdSize() + 9, length);
            return CommandClassInfo.ParseList(allCommandClasses);
        }
    }
}

public readonly struct ApplicationUpdateSmartStartIncludedNodeInfo
{
    private readonly ReadOnlyMemory<byte> _data;

    private readonly NodeIdType _nodeIdType;

    public ApplicationUpdateSmartStartIncludedNodeInfo(ReadOnlyMemory<byte> data, NodeIdType nodeIdType)
    {
        _data = data;
        _nodeIdType = nodeIdType;
    }

    public ushort NodeId => _nodeIdType.ReadNodeId(_data.Span, 0);

    // Byte at NodeIdType.NodeIdSize() is reserved

    public ReceivedStatus ReceivedStatus => (ReceivedStatus)_data.Span[_nodeIdType.NodeIdSize() + 1];

    /// <summary>
    /// The NWI HomeID for which the SmartStart Inclusion Node Information Frame was received
    /// </summary>
    public uint HomeId => _data.Span[(_nodeIdType.NodeIdSize() + 2)..(_nodeIdType.NodeIdSize() + 6)].ToUInt32BE();
}
