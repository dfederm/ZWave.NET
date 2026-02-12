namespace ZWave.Serial.Commands;

[Flags]
public enum ControllerCapabilities : byte
{
    /// <summary>
    /// The Z-Wave Module has the secondary controller role
    /// </summary>
    SecondaryController = 1 << 0,

    /// <summary>
    /// The module has been included on another network and did not start the current network
    /// </summary>
    OtherNetwork = 1 << 1,

    /// <summary>
    /// A SIS is present in the current network.
    /// </summary>
    SisIsPresent = 1 << 2,

    /// <summary>
    /// Before the SIS was added, the controller was the primary
    /// </summary>
    /// <remarks>
    /// As per spec, bit 3 is to be ignored
    /// </remarks>
    WasRealPrimary = 1 << 3,

    /// <summary>
    /// The module provides the SUC functionality in the current network
    /// </summary>
    SucEnabled = 1 << 4,

    /// <summary>
    /// The module is the only node in the network.
    /// </summary>
    NoNodesIncluded = 1 << 5,
}

public readonly struct GetControllerCapabilitiesRequest : ICommand<GetControllerCapabilitiesRequest>
{
    public GetControllerCapabilitiesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetControllerCapabilities;

    public DataFrame Frame { get; }

    public static GetControllerCapabilitiesRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetControllerCapabilitiesRequest(frame);
    }

    public static GetControllerCapabilitiesRequest Create(DataFrame frame) => new GetControllerCapabilitiesRequest(frame);
}

public readonly struct GetControllerCapabilitiesResponse : ICommand<GetControllerCapabilitiesResponse>
{
    public GetControllerCapabilitiesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetControllerCapabilities;

    public DataFrame Frame { get; }

    public ControllerCapabilities Capabilities => (ControllerCapabilities)Frame.CommandParameters.Span[0];

    public static GetControllerCapabilitiesResponse Create(DataFrame frame) => new GetControllerCapabilitiesResponse(frame);
}
