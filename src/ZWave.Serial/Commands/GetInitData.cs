namespace ZWave.Serial.Commands;

[Flags]
public enum GetInitDataCapabilities : byte
{
    /// <summary>
    /// The Z-Wave module is an end node.
    /// </summary>
    EndNode = 1 << 0,

    /// <summary>
    /// The Z-Wave module supports timer functions.
    /// </summary>
    TimerFunctions = 1 << 1,

    /// <summary>
    /// The Z-Wave module has the Primary Controller role in the current network.
    /// </summary>
    /// <remarks>
    /// The spec is very unclear on this value, so copying zwave-js in its interpretation.
    /// </remarks>
    SecondaryController = 1 << 2,

    /// <summary>
    /// The Z-Wave module has SIS functionality enabled.
    /// </summary>
    SisFunctionality = 1 << 3,
}

public readonly struct GetInitDataRequest : ICommand<GetInitDataRequest>
{
    public GetInitDataRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetInitData;

    public DataFrame Frame { get; }

    public static GetInitDataRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetInitDataRequest(frame);
    }

    public static GetInitDataRequest Create(DataFrame frame) => new GetInitDataRequest(frame);
}

public readonly struct GetInitDataResponse : ICommand<GetInitDataResponse>
{
    public GetInitDataResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetInitData;

    public DataFrame Frame { get; }

    /// <summary>
    /// The Z-Wave API version that the Z-Wave Module is currently running.
    /// </summary>
    public byte ApiVersion => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The capabilities of the Z-Wave API running on the Z-Wave Module.
    /// </summary>
    public GetInitDataCapabilities ApiCapabilities => (GetInitDataCapabilities)Frame.CommandParameters.Span[1];

    /// <summary>
    /// List ids for nodes present in the current network.
    /// </summary>
    public HashSet<byte> NodeIds
    {
        get
        {
            byte nodeListLength = Frame.CommandParameters.Span[2];
            var nodeIds = new HashSet<byte>(nodeListLength * 8);

            var bitMask = Frame.CommandParameters.Span.Slice(3, nodeListLength);
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        // As per the spec, bit 0 corresponds to NodeID 1, so we need to add 1.
                        byte nodeId = (byte)((byteNum << 3) + bitNum + 1);
                        nodeIds.Add(nodeId);
                    }
                }
            }

            return nodeIds;
        }
    }

    /// <summary>
    /// The chip type of the Z-Wave Module.
    /// </summary>
    public byte ChipType => Frame.CommandParameters.Span[^2];

    /// <summary>
    /// The chip version of the Z-Wave Module.
    /// </summary>
    public byte ChipVersion => Frame.CommandParameters.Span[^1];

    public static GetInitDataResponse Create(DataFrame frame) => new GetInitDataResponse(frame);
}
