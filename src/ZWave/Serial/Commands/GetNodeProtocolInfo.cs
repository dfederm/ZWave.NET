namespace ZWave.Serial.Commands;

public enum FrequentListeningMode
{
    None,

    Sensor1000ms,

    Sensor250ms,
}

public enum NodeType
{
    Unknown,

    Controller,

    EndNode,
}

public readonly struct GetNodeProtocolInfoRequest : ICommand<GetNodeProtocolInfoRequest>
{
    public GetNodeProtocolInfoRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetNodeProtocolInfo;

    public DataFrame Frame { get; }

    public static GetNodeProtocolInfoRequest Create(byte nodeId)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            nodeId, // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new GetNodeProtocolInfoRequest(frame);
    }

    public static GetNodeProtocolInfoRequest Create(DataFrame frame) => new GetNodeProtocolInfoRequest(frame);
}

public readonly struct GetNodeProtocolInfoResponse : ICommand<GetNodeProtocolInfoResponse>
{
    public GetNodeProtocolInfoResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetNodeProtocolInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicate that the node is always listening
    /// </summary>
    public bool IsListening => (Frame.CommandParameters.Span[0] & 0b10000000) != 0;

    /// <summary>
    /// Indicate that the node is able to route messages
    /// </summary>
    public bool IsRouting => (Frame.CommandParameters.Span[0] & 0b01000000) != 0;

    public IReadOnlyList<int> SupportedSpeeds
    {
        get
        {
            var supportedSpeeds = new List<int>(3);

            // Supported speed is bits 3-5 of byte 0
            if ((Frame.CommandParameters.Span[0] & 0b00010000) != 0)
            {
                supportedSpeeds.Add(40000);
            }

            if ((Frame.CommandParameters.Span[0] & 0b00001000) != 0)
            {
                supportedSpeeds.Add(9600);
            }

            // Speed extension is bits 0-2 of byte 2
            if ((Frame.CommandParameters.Span[2] & 0b00000001) != 0)
            {
                supportedSpeeds.Add(100000);
            }

            return supportedSpeeds;
        }
    }

    public byte ProtocolVersion => (byte)(Frame.CommandParameters.Span[0] & 0b00000111);

    /// <summary>
    /// Indicate that this node supports other command classes than the mandatory for the
    /// selected generic/specific device class and that a controlling node needs to look at
    /// the supported command classes to fully control this device
    /// </summary>
    public bool OptionalFunctionality => (Frame.CommandParameters.Span[1] & 0b10000000) != 0;

    public FrequentListeningMode FrequentListeningMode =>
        (Frame.CommandParameters.Span[1] & 0b01000000) != 0
            ? FrequentListeningMode.Sensor1000ms
            : (Frame.CommandParameters.Span[1] & 0b00100000) != 0
                ? FrequentListeningMode.Sensor250ms
                : FrequentListeningMode.None;

    public bool SupportsBeaming => (Frame.CommandParameters.Span[1] & 0b00010000) != 0;

    public NodeType NodeType => (Frame.CommandParameters.Span[1] & 0b00001000) != 0
        ? NodeType.EndNode
        : (Frame.CommandParameters.Span[1] & 0b00000010) != 0
            ? NodeType.Controller
            : NodeType.Unknown;

    public bool HasSpecificDeviceClass => (Frame.CommandParameters.Span[1] & 0b00000100) != 0;

    public bool SupportsSecurity => (Frame.CommandParameters.Span[1] & 0b00000001) != 0;

    public byte BasicDeviceClass => Frame.CommandParameters.Span[3];

    /// <summary>
    /// Identifies what Generic Device Class this node is part of and must be set by the application
    /// </summary>
    public byte GenericDeviceClass => Frame.CommandParameters.Span[4];

    /// <summary>
    /// Specifies what Specific Device Class this application is part of and must be set by the application
    /// </summary>
    public byte SpecificDeviceClass => HasSpecificDeviceClass ? Frame.CommandParameters.Span[5] : (byte)0;

    public static GetNodeProtocolInfoResponse Create(DataFrame frame) => new GetNodeProtocolInfoResponse(frame);
}
