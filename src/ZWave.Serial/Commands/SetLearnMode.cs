namespace ZWave.Serial.Commands;

/// <summary>
/// The learn mode to set.
/// </summary>
public enum LearnMode : byte
{
    /// <summary>
    /// Disable learn mode.
    /// </summary>
    Disable = 0x00,

    /// <summary>
    /// Enable classic inclusion mode.
    /// </summary>
    ClassicInclusion = 0x01,

    /// <summary>
    /// Enable network-wide inclusion mode.
    /// </summary>
    NetworkWideInclusion = 0x02,

    /// <summary>
    /// Enable network-wide exclusion mode.
    /// </summary>
    NetworkWideExclusion = 0x03,
}

/// <summary>
/// The status of the learn mode operation.
/// </summary>
public enum LearnModeStatus : byte
{
    /// <summary>
    /// The learn mode operation has started.
    /// </summary>
    Started = 0x01,

    /// <summary>
    /// The learn mode operation has completed successfully.
    /// </summary>
    Done = 0x06,

    /// <summary>
    /// The learn mode operation has failed.
    /// </summary>
    Failed = 0x07,
}

/// <summary>
/// Enable or disable home and node ID's learn mode.
/// </summary>
public readonly struct SetLearnModeRequest : IRequestWithCallback<SetLearnModeRequest>
{
    public SetLearnModeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetLearnMode;

    public static bool ExpectsResponseStatus => false;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[1];

    public static SetLearnModeRequest Create(
        LearnMode mode,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)mode, sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetLearnModeRequest(frame);
    }

    public static SetLearnModeRequest Create(DataFrame frame) => new SetLearnModeRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SetLearnModeRequest"/> command.
/// </summary>
public readonly struct SetLearnModeCallback : ICommand<SetLearnModeCallback>
{
    public SetLearnModeCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetLearnMode;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the learn mode operation.
    /// </summary>
    public LearnModeStatus Status => (LearnModeStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The node ID assigned during the learn mode operation.
    /// </summary>
    public ushort AssignedNodeId => Frame.CommandParameters.Span[2];

    public static SetLearnModeCallback Create(DataFrame frame) => new SetLearnModeCallback(frame);
}
