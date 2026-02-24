namespace ZWave.Serial.Commands;

/// <summary>
/// This command is used by a Z-Wave module to notify a host application that a Z-Wave frame has been received
/// to the Bridge Controller or an existing virtual node.
/// </summary>
public readonly struct ApplicationCommandHandlerBridge : ICommand<ApplicationCommandHandlerBridge>
{
    private readonly NodeIdType _nodeIdType;

    public ApplicationCommandHandlerBridge(DataFrame frame, NodeIdType nodeIdType)
    {
        Frame = frame;
        _nodeIdType = nodeIdType;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationCommandHandlerBridge;

    public DataFrame Frame { get; }

    /// <summary>
    /// The received status flags.
    /// </summary>
    public ReceivedStatus ReceivedStatus => (ReceivedStatus)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The destination node ID.
    /// </summary>
    public ushort DestinationNodeId => _nodeIdType.ReadNodeId(Frame.CommandParameters.Span, 1);

    /// <summary>
    /// The source node ID.
    /// </summary>
    public ushort SourceNodeId => _nodeIdType.ReadNodeId(Frame.CommandParameters.Span, 1 + _nodeIdType.NodeIdSize());

    private byte PayloadLength => Frame.CommandParameters.Span[1 + 2 * _nodeIdType.NodeIdSize()];

    /// <summary>
    /// The payload data.
    /// </summary>
    public ReadOnlyMemory<byte> Payload => Frame.CommandParameters.Slice(2 + 2 * _nodeIdType.NodeIdSize(), PayloadLength);

    /// <summary>
    /// The RSSI measurement of the received frame.
    /// </summary>
    public RssiMeasurement ReceivedRssi => Frame.CommandParameters.Span[2 + 2 * _nodeIdType.NodeIdSize() + PayloadLength];

    public static ApplicationCommandHandlerBridge Create(DataFrame frame, CommandParsingContext context) => new ApplicationCommandHandlerBridge(frame, context.NodeIdType);
}
