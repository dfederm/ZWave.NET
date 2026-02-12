namespace ZWave.Serial.Commands;

public readonly struct GetSerialApiCapabilitiesRequest : ICommand<GetSerialApiCapabilitiesRequest>
{
    public GetSerialApiCapabilitiesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetSerialApiCapabilities;

    public DataFrame Frame { get; }

    public static GetSerialApiCapabilitiesRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetSerialApiCapabilitiesRequest(frame);
    }

    public static GetSerialApiCapabilitiesRequest Create(DataFrame frame) => new GetSerialApiCapabilitiesRequest(frame);
}

public readonly struct GetSerialApiCapabilitiesResponse : ICommand<GetSerialApiCapabilitiesResponse>
{
    public GetSerialApiCapabilitiesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetSerialApiCapabilities;

    public DataFrame Frame { get; }

    /// <summary>
    /// Gets the Serial API application Version number.
    /// </summary>
    public byte SerialApiVersion => Frame.CommandParameters.Span[0];

    /// <summary>
    /// Gets the Serial API application Revision number.
    /// </summary>
    public byte SerialApiRevision => Frame.CommandParameters.Span[1];

    /// <summary>
    /// Gets the Serial API application manufacturer_id.
    /// </summary>
    public ushort ManufacturerId => Frame.CommandParameters.Span[2..4].ToUInt16BE();

    /// <summary>
    /// Gets the Serial API application manufacturer product type
    /// </summary>
    public ushort ManufacturerProductType => Frame.CommandParameters.Span[4..6].ToUInt16BE();

    /// <summary>
    /// Gets the Serial API application manufacturer product ID
    /// </summary>
    public ushort ManufacturerProductId => Frame.CommandParameters.Span[6..8].ToUInt16BE();

    /// <summary>
    /// Gets the supported commands
    /// </summary>
    public HashSet<CommandId> SupportedCommandIds
    {
        get
        {
            // From the spec the remaining values are:
            //   a bitmask where every supported Serial API function ID has a corresponding bit in the bitmask
            //   set to ‘1’. All unsupported Serial API function IDs have their corresponding bit set to ‘0’.
            //   The first byte in bitmask corresponds to FuncIDs 1-8, where bit 0 corresponds to FuncID 1 and
            //   bit 7 corresponds to FuncID 8. The second byte in bitmask corresponds to FuncIDs 9-16, and so on.
            ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span[8..];

            // This length is pretty over-estimated, but CommandId is only a byte so this is at most 256, and we'll trim later.
            var supportedCommandIds = new HashSet<CommandId>(bitMask.Length * 8);

            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        // As per spec quote above, bit 0 corresponds to FuncID 1, so we need to add 1.
                        CommandId commandId = (CommandId)((byteNum << 3) + bitNum + 1);
                        supportedCommandIds.Add(commandId);
                    }
                }
            }

            // Avoid holding onto more memory than we actually need
            supportedCommandIds.TrimExcess();

            return supportedCommandIds;
        }
    }

    public static GetSerialApiCapabilitiesResponse Create(DataFrame frame) => new GetSerialApiCapabilitiesResponse(frame);
}
