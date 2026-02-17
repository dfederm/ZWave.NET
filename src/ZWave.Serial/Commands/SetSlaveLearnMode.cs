namespace ZWave.Serial.Commands;

/// <summary>
/// The slave learn mode to set.
/// </summary>
public enum SlaveLearnMode : byte
{
    /// <summary>
    /// Disable slave learn mode.
    /// </summary>
    Disable = 0x00,

    /// <summary>
    /// Enable slave learn mode.
    /// </summary>
    Enable = 0x01,

    /// <summary>
    /// Add a virtual slave node.
    /// </summary>
    Add = 0x02,

    /// <summary>
    /// Remove a virtual slave node.
    /// </summary>
    Remove = 0x03,
}

/// <summary>
/// The status of the slave learn mode operation.
/// </summary>
public enum SlaveLearnModeStatus : byte
{
    /// <summary>
    /// The slave learn mode operation has completed successfully.
    /// </summary>
    Done = 0x06,

    /// <summary>
    /// The slave learn mode operation has failed.
    /// </summary>
    Failed = 0x07,
}

/// <summary>
/// Enable or disable "Slave Learn Mode" to allow controllers to add or remove Virtual Slave Nodes.
/// </summary>
public readonly struct SetSlaveLearnModeRequest : IRequestWithCallback<SetSlaveLearnModeRequest>
{
    public SetSlaveLearnModeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSlaveLearnMode;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[2];

    public static SetSlaveLearnModeRequest Create(
        byte nodeId,
        SlaveLearnMode mode,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [nodeId, (byte)mode, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetSlaveLearnModeRequest(frame);
    }

    public static SetSlaveLearnModeRequest Create(DataFrame frame) => new SetSlaveLearnModeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SetSlaveLearnModeRequest"/> command.
/// </summary>
public readonly struct SetSlaveLearnModeCallback : ICommand<SetSlaveLearnModeCallback>
{
    public SetSlaveLearnModeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSlaveLearnMode;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the slave learn mode operation.
    /// </summary>
    public SlaveLearnModeStatus Status => (SlaveLearnModeStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The original node ID.
    /// </summary>
    public byte OrgNodeId => Frame.CommandParameters.Span[2];

    /// <summary>
    /// The new node ID.
    /// </summary>
    public byte NewNodeId => Frame.CommandParameters.Span[3];

    public static SetSlaveLearnModeCallback Create(DataFrame frame) => new SetSlaveLearnModeCallback(frame);
}
