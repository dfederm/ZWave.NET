namespace ZWave.Serial.Commands;

/// <summary>
/// Request the Z-Wave Hardware, Protocol, and Host API manufacturer info.
/// </summary>
public readonly struct GetManufacturerInfoRequest : ICommand<GetManufacturerInfoRequest>
{
    public GetManufacturerInfoRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetManufacturerInfo;

    public DataFrame Frame { get; }

    public static GetManufacturerInfoRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId);
        return new GetManufacturerInfoRequest(frame);
    }

    public static GetManufacturerInfoRequest Create(DataFrame frame) => new GetManufacturerInfoRequest(frame);
}

public readonly struct GetManufacturerInfoResponse : ICommand<GetManufacturerInfoResponse>
{
    public GetManufacturerInfoResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetManufacturerInfo;

    public DataFrame Frame { get; }

    /// <summary>
    /// Gets the Z-Wave Hardware Manufacturer ID (16-bit, big-endian).
    /// </summary>
    public ushort HardwareManufacturerId => Frame.CommandParameters.Span[0..2].ToUInt16BE();

    /// <summary>
    /// Gets the Z-Wave Protocol Manufacturer ID (16-bit, big-endian).
    /// </summary>
    public ushort ProtocolManufacturerId => Frame.CommandParameters.Span[2..4].ToUInt16BE();

    /// <summary>
    /// Gets the Z-Wave Host API Manufacturer ID (16-bit, big-endian).
    /// </summary>
    public ushort HostApiManufacturerId => Frame.CommandParameters.Span[4..6].ToUInt16BE();

    /// <summary>
    /// Gets the manufacturer-specific chip info data.
    /// </summary>
    public ReadOnlyMemory<byte> ChipInfo
    {
        get
        {
            byte length = Frame.CommandParameters.Span[6];
            return length > 0
                ? Frame.CommandParameters.Slice(7, length)
                : ReadOnlyMemory<byte>.Empty;
        }
    }

    public static GetManufacturerInfoResponse Create(DataFrame frame) => new GetManufacturerInfoResponse(frame);
}
