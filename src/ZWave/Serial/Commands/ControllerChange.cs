namespace ZWave.Serial.Commands;

/// <summary>
/// The mode for the controller change operation.
/// </summary>
public enum ControllerChangeMode : byte
{
    /// <summary>
    /// Start the controller change process.
    /// </summary>
    Start = 0x02,

    /// <summary>
    /// Stop the controller change process.
    /// </summary>
    Stop = 0x05,

    /// <summary>
    /// Start the controller change process using network-wide inclusion.
    /// </summary>
    StartNetworkWide = 0x42,
}

/// <summary>
/// The status of the controller change operation.
/// </summary>
public enum ControllerChangeStatus : byte
{
    /// <summary>
    /// A node requesting inclusion has been found.
    /// </summary>
    NodeFound = 0x02,

    /// <summary>
    /// The inclusion is ongoing.
    /// </summary>
    InclusionOngoing = 0x03,

    /// <summary>
    /// The inclusion has completed successfully.
    /// </summary>
    InclusionCompleted = 0x05,

    /// <summary>
    /// The inclusion has failed.
    /// </summary>
    InclusionFailed = 0x07,
}

/// <summary>
/// Add a controller to the Z-Wave network and transfer the role as primary controller to it.
/// </summary>
public readonly struct ControllerChangeRequest : ICommand<ControllerChangeRequest>
{
    public ControllerChangeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ControllerChange;

    public DataFrame Frame { get; }

    public static ControllerChangeRequest Create(
        ControllerChangeMode mode,
        bool isHighPower,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)((byte)mode | (isHighPower ? 0x80 : 0)), sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ControllerChangeRequest(frame);
    }

    public static ControllerChangeRequest Create(DataFrame frame) => new ControllerChangeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="ControllerChangeRequest"/> command.
/// </summary>
public readonly struct ControllerChangeCallback : ICommand<ControllerChangeCallback>
{
    public ControllerChangeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ControllerChange;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the controller change operation.
    /// </summary>
    public ControllerChangeStatus Status => (ControllerChangeStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The node ID assigned during the controller change operation.
    /// </summary>
    public byte AssignedNodeId => Frame.CommandParameters.Span[2];

    public static ControllerChangeCallback Create(DataFrame frame) => new ControllerChangeCallback(frame);
}
