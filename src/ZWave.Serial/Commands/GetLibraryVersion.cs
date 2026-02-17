using System.Text;

namespace ZWave.Serial.Commands;

public enum LibraryType : byte
{
    /// <summary>
    /// This library is intended for main home controllers, that are typically Primary controllers in a network
    /// </summary>
    StaticController = 0x01,

    /// <summary>
    /// This library is intended for small portable controllers, that are typically secondary controllers or
    /// inclusion controllers in a network
    /// </summary>
    PortableController = 0x02,

    /// <summary>
    /// This library is intended for end nodes.
    /// </summary>
    Enhanced232EndNode = 0x03,

    /// <summary>
    /// This library is intended for end nodes with more limited capabilities than the Enhanced 232 End Node Library.
    /// </summary>
    EndNode = 0x04,

    /// <summary>
    /// This library is intended for controllers nodes used for setup and monitoring of existing networks.
    /// </summary>
    Installer = 0x05,

    /// <summary>
    /// This library is intended for end nodes with routing capabilities.
    /// </summary>
    RoutingEndNode = 0x06,

    /// <summary>
    /// This library is intended for controller nodes that are able to allocate more than 1 NodeID to themselves and use
    /// them for transmitting/receiving frames
    /// </summary>
    BridgeController = 0x07,
}

public readonly struct GetLibraryVersionRequest : ICommand<GetLibraryVersionRequest>
{
    public GetLibraryVersionRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetLibraryVersion;

    public DataFrame Frame { get; }

    public static GetLibraryVersionRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetLibraryVersionRequest(frame);
    }

    public static GetLibraryVersionRequest Create(DataFrame frame) => new GetLibraryVersionRequest(frame);
}

public readonly struct GetLibraryVersionResponse : ICommand<GetLibraryVersionResponse>
{
    public GetLibraryVersionResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetLibraryVersion;

    public DataFrame Frame { get; }

    /// <summary>
    /// The Z-Wave API library version that runs on the Z-Wave Module
    /// </summary>
    public string LibraryVersion
    {
        get
        {
            // This is a null-terminated ASCII string
            ReadOnlySpan<byte> bytes = Frame.CommandParameters.Span[0..12];

            var nullIndex = bytes.IndexOf((byte)0);
            if (nullIndex != -1)
            {
                bytes = bytes[..nullIndex];
            }

            return Encoding.ASCII.GetString(bytes);
        }
    }

    /// <summary>
    /// The library type that runs on the Z-Wave Module.
    /// </summary>
    public LibraryType LibraryType => (LibraryType)Frame.CommandParameters.Span[12];

    public static GetLibraryVersionResponse Create(DataFrame frame) => new GetLibraryVersionResponse(frame);
}
