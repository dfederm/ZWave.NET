namespace ZWave.Serial.Commands;

/// <summary>
/// Get the status of the radio debug interface.
/// </summary>
public readonly struct RadioDebugStatusRequest : ICommand<RadioDebugStatusRequest>
{
    public RadioDebugStatusRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RadioDebugStatus;

    public DataFrame Frame { get; }

    public static RadioDebugStatusRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId);
        return new RadioDebugStatusRequest(frame);
    }

    public static RadioDebugStatusRequest Create(DataFrame frame) => new RadioDebugStatusRequest(frame);
}

public readonly struct RadioDebugStatusResponse : ICommand<RadioDebugStatusResponse>
{
    public RadioDebugStatusResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.RadioDebugStatus;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the debug interface is enabled.
    /// </summary>
    public bool IsEnabled => Frame.CommandParameters.Span[0] != 0;

    /// <summary>
    /// The debug interface protocol. Only present in V2 responses.
    /// If IsEnabled is false, this field is not relevant.
    /// </summary>
    public DebugInterfaceProtocol? DebugInterfaceProtocol
        => Frame.CommandParameters.Length >= 3
            ? (DebugInterfaceProtocol)Frame.CommandParameters.Span[1..3].ToUInt16BE()
            : null;

    public static RadioDebugStatusResponse Create(DataFrame frame) => new RadioDebugStatusResponse(frame);
}
