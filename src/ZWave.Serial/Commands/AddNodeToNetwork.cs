using static ZWave.Serial.Commands.CommandDataParsingHelpers;

namespace ZWave.Serial.Commands;

public enum AddNodeMode : byte
{
    /// <summary>
    /// Add any node.
    /// </summary>
    Any = 0x01,

    /// <summary>
    /// Add controller node.
    /// </summary>
    [Obsolete($"Deprecated: Use {nameof(AddNodeMode)}.{nameof(Any)} instead.")]
    Controller = 0x02,

    /// <summary>
    /// Add End Node.
    /// </summary>
    [Obsolete($"Deprecated: Use {nameof(AddNodeMode)}.{nameof(Any)} instead.")]
    EndNode = 0x03,

    /// <summary>
    /// Add existing node.
    /// </summary>
    [Obsolete($"Deprecated: Use {nameof(AddNodeMode)}.{nameof(Any)} instead.")]
    Existing = 0x04,

    /// <summary>
    /// Stop network inclusion.
    /// </summary>
    StopNetworkInclusion = 0x05,

    /// <summary>
    /// Stop controller replication.
    /// </summary>
    StopControllerReplication = 0x06,

    /// <summary>
    /// SmartStart Include Node.
    /// </summary>
    SmartStartInclude = 0x08,

    /// <summary>
    /// Start SmartStart.
    /// </summary>
    StartSmartStart = 0x09,
}

public enum AddNodeStatus : byte
{
    /// <summary>
    /// The Z-Wave Module has initiated Network inclusion and is ready to include new nodes.
    /// </summary>
    NetworkInclusionStarted = 0x01,

    /// <summary>
    /// A node requesting inclusion has been found and the network inclusion is initiated.
    /// </summary>
    NodeFound = 0x02,

    /// <summary>
    /// The network inclusion is ongoing with an End Node.
    /// </summary>
    InclusionOngoingEndNode = 0x03,

    /// <summary>
    /// The network inclusion is ongoing with a Controller node.
    /// </summary>
    InclusionOngoingController = 0x04,

    /// <summary>
    /// The network inclusion is completed.
    /// </summary>
    InclusionProtocolCompleted = 0x05,

    /// <summary>
    /// The network inclusion is completed.
    /// </summary>
    InclusionCompleted = 0x06,
}

public readonly struct AddNodeToNetworkRequest : ICommand<AddNodeToNetworkRequest>
{
    private const byte HighPower = 0b1000_0000;
    private const byte NetworkWide = 0b0100_0000;
    private const byte LongRange = 0b0010_0000;

    public AddNodeToNetworkRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AddNodeToNetwork;

    public DataFrame Frame { get; }

    public static AddNodeToNetworkRequest Create(
        bool isHighPower,
        bool isNetworkWide,
        bool isLongRange,
        AddNodeMode addMode,
        byte sessionId)
    {
        if (addMode == AddNodeMode.SmartStartInclude)
        {
            throw new ArgumentException($"To use {nameof(AddNodeMode.SmartStartInclude)}, call {nameof(CreateSmartStartInclude)} instead", nameof(addMode));
        }

        if (addMode == AddNodeMode.StartSmartStart)
        {
            throw new ArgumentException($"To use {nameof(AddNodeMode.StartSmartStart)}, call {nameof(CreateStartSmartStart)} instead", nameof(addMode));
        }

        Span<byte> commandParameters = stackalloc byte[2];

        if (isHighPower)
        {
            commandParameters[0] |= HighPower;
        }

        if (isNetworkWide)
        {
            commandParameters[0] |= NetworkWide;
        }

        if (isLongRange)
        {
            commandParameters[0] |= LongRange;
        }

        commandParameters[0] |= (byte)addMode;

        commandParameters[1] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AddNodeToNetworkRequest(frame);
    }

    public static AddNodeToNetworkRequest CreateSmartStartInclude(
        bool isHighPower,
        bool isNetworkWide,
        bool isLongRange,
        byte sessionId,
        uint nwiHomeId,
        uint authHomeId)
    {
        Span<byte> commandParameters = stackalloc byte[10];

        if (isHighPower)
        {
            commandParameters[0] |= HighPower;
        }

        if (isNetworkWide)
        {
            commandParameters[0] |= NetworkWide;
        }

        if (isLongRange)
        {
            commandParameters[0] |= LongRange;
        }

        commandParameters[0] |= (byte)AddNodeMode.SmartStartInclude;

        commandParameters[1] = sessionId;

        nwiHomeId.WriteBytesBE(commandParameters[2..6]);
        authHomeId.WriteBytesBE(commandParameters[6..10]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AddNodeToNetworkRequest(frame);
    }

    public static AddNodeToNetworkRequest CreateStartSmartStart()
    {
        ReadOnlySpan<byte> commandParameters =
        [
            NetworkWide | (byte)AddNodeMode.SmartStartInclude,
            0, // No callback
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AddNodeToNetworkRequest(frame);
    }

    public static AddNodeToNetworkRequest Create(DataFrame frame) => new AddNodeToNetworkRequest(frame);
}

public readonly struct AddNodeToNetworkCallback : ICommand<AddNodeToNetworkCallback>
{
    public AddNodeToNetworkCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AddNodeToNetwork;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public AddNodeStatus Status => (AddNodeStatus)Frame.CommandParameters.Span[1];

    public byte AssignedNodeId => Frame.CommandParameters.Span[2];

    public byte BasicDeviceClass => Frame.CommandParameters.Span[4];

    public byte GenericDeviceClass => Frame.CommandParameters.Span[5];

    public byte SpecificDeviceClass => Frame.CommandParameters.Span[6];

    /// <summary>
    /// The list of non-secure implemented Command Classes by the remote node.
    /// </summary>
    public IReadOnlyList<CommandClassInfo> CommandClasses
    {
        get
        {
            byte length = Frame.CommandParameters.Span[3];
            ReadOnlySpan<byte> allCommandClasses = Frame.CommandParameters.Span.Slice(7, length);
            return ParseCommandClasses(allCommandClasses);
        }
    }

    public static AddNodeToNetworkCallback Create(DataFrame frame) => new AddNodeToNetworkCallback(frame);
}
