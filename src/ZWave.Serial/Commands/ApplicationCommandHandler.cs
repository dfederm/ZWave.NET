namespace ZWave.Serial.Commands;

/// <summary>
/// This command is used by a Z-Wave module to notify a host application that a Z-Wave frame has been received
/// </summary>
public readonly struct ApplicationCommandHandler : ICommand<ApplicationCommandHandler>
{
    private readonly NodeIdType _nodeIdType;

    public ApplicationCommandHandler(DataFrame frame, NodeIdType nodeIdType)
    {
        Frame = frame;
        _nodeIdType = nodeIdType;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationCommandHandler;

    public DataFrame Frame { get; }

    public ReceivedStatus ReceivedStatus => (ReceivedStatus)Frame.CommandParameters.Span[0];

    public ushort NodeId => _nodeIdType.ReadNodeId(Frame.CommandParameters.Span, 1);

    private byte PayloadLength => Frame.CommandParameters.Span[1 + _nodeIdType.NodeIdSize()];

    public ReadOnlyMemory<byte> Payload => Frame.CommandParameters.Slice(2 + _nodeIdType.NodeIdSize(), PayloadLength);

    public RssiMeasurement ReceivedRssi => Frame.CommandParameters.Span[2 + _nodeIdType.NodeIdSize() + PayloadLength];

    public static ApplicationCommandHandler Create(DataFrame frame, CommandParsingContext context) => new ApplicationCommandHandler(frame, context.NodeIdType);
}
