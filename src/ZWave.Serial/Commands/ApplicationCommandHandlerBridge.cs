namespace ZWave.Serial.Commands;

/// <summary>
/// This command is used by a Z-Wave module to notify a host application that a Z-Wave frame has been received
/// to the Bridge Controller or an existing virtual slave node.
/// </summary>
public readonly struct ApplicationCommandHandlerBridge : ICommand<ApplicationCommandHandlerBridge>
{
    public ApplicationCommandHandlerBridge(DataFrame frame)
    {
        Frame = frame;
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
    public ushort DestinationNodeId => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The source node ID.
    /// </summary>
    public ushort SourceNodeId => Frame.CommandParameters.Span[2];

    private byte PayloadLength => Frame.CommandParameters.Span[3];

    /// <summary>
    /// The payload data.
    /// </summary>
    public ReadOnlyMemory<byte> Payload => Frame.CommandParameters.Slice(4, PayloadLength);

    /// <summary>
    /// The RSSI measurement of the received frame.
    /// </summary>
    public RssiMeasurement ReceivedRssi => Frame.CommandParameters.Span[4 + PayloadLength];

    public static ApplicationCommandHandlerBridge Create(DataFrame frame) => new ApplicationCommandHandlerBridge(frame);
}
