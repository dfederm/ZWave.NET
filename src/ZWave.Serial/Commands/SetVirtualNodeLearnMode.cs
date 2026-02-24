namespace ZWave.Serial.Commands;

/// <summary>
/// The Virtual Node learn mode to set.
/// </summary>
public enum VirtualNodeLearnMode : byte
{
    /// <summary>
    /// Disable Virtual Node learn mode.
    /// </summary>
    Disable = 0x00,

    /// <summary>
    /// Enable Virtual Node learn mode.
    /// </summary>
    Enable = 0x01,

    /// <summary>
    /// Add a virtual node.
    /// </summary>
    Add = 0x02,

    /// <summary>
    /// Remove a virtual node.
    /// </summary>
    Remove = 0x03,
}

/// <summary>
/// The status of the Virtual Node learn mode operation.
/// </summary>
public enum VirtualNodeLearnModeStatus : byte
{
    /// <summary>
    /// The Virtual Node learn mode operation has completed successfully.
    /// </summary>
    Done = 0x06,

    /// <summary>
    /// The Virtual Node learn mode operation has failed.
    /// </summary>
    Failed = 0x07,
}

/// <summary>
/// Enable or disable "Virtual Node Learn Mode" to allow controllers to add or remove Virtual Nodes.
/// </summary>
public readonly struct SetVirtualNodeLearnModeRequest : IRequestWithCallback<SetVirtualNodeLearnModeRequest>
{
    public SetVirtualNodeLearnModeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetVirtualNodeLearnMode;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[2];

    /// <remarks>
    /// Per Z-Wave Host API Specification, the NodeID field MUST be encoded using 8 bits regardless
    /// of the configured NodeID base Type.
    /// </remarks>
    public static SetVirtualNodeLearnModeRequest Create(
        ushort nodeId,
        VirtualNodeLearnMode mode,
        byte sessionId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nodeId, (ushort)0xFF);
        ReadOnlySpan<byte> commandParameters = [(byte)nodeId, (byte)mode, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetVirtualNodeLearnModeRequest(frame);
    }

    public static SetVirtualNodeLearnModeRequest Create(DataFrame frame, CommandParsingContext context) => new SetVirtualNodeLearnModeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SetVirtualNodeLearnModeRequest"/> command.
/// </summary>
public readonly struct SetVirtualNodeLearnModeCallback : ICommand<SetVirtualNodeLearnModeCallback>
{
    public SetVirtualNodeLearnModeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetVirtualNodeLearnMode;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the Virtual Node learn mode operation.
    /// </summary>
    public VirtualNodeLearnModeStatus Status => (VirtualNodeLearnModeStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The original node ID.
    /// </summary>
    public ushort OrgNodeId => Frame.CommandParameters.Span[2];

    /// <summary>
    /// The new node ID.
    /// </summary>
    public ushort NewNodeId => Frame.CommandParameters.Span[3];

    public static SetVirtualNodeLearnModeCallback Create(DataFrame frame, CommandParsingContext context) => new SetVirtualNodeLearnModeCallback(frame);
}
