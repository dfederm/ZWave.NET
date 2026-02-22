namespace ZWave.Serial.Commands;

/// <summary>
/// This command is used by a Z-Wave module to notify a host application that a Z-Wave frame has been received
/// </summary>
public readonly struct ApplicationCommandHandler : ICommand<ApplicationCommandHandler>
{
    public ApplicationCommandHandler(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationCommandHandler;

    public DataFrame Frame { get; }

    public ReceivedStatus ReceivedStatus => (ReceivedStatus)Frame.CommandParameters.Span[0];

    public ushort NodeId => Frame.CommandParameters.Span[1];

    private byte PayloadLength => Frame.CommandParameters.Span[2];

    public ReadOnlyMemory<byte> Payload => Frame.CommandParameters.Slice(3, PayloadLength);

    public RssiMeasurement ReceivedRssi => Frame.CommandParameters.Span[3 + PayloadLength];

    public static ApplicationCommandHandler Create(DataFrame frame) => new ApplicationCommandHandler(frame);
}
