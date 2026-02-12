namespace ZWave.Serial.Commands;

/// <summary>
/// Power down the RF when not in use.
/// </summary>
public readonly struct SetRFReceiveModeRequest : ICommand<SetRFReceiveModeRequest>
{
    public SetRFReceiveModeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetRFReceiveMode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the RF receive mode.
    /// </summary>
    /// <param name="enabled">True to enable RF receive, false to disable.</param>
    public static SetRFReceiveModeRequest Create(bool enabled)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)(enabled ? 1 : 0)];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetRFReceiveModeRequest(frame);
    }

    public static SetRFReceiveModeRequest Create(DataFrame frame) => new SetRFReceiveModeRequest(frame);
}

public readonly struct SetRFReceiveModeResponse : ICommand<SetRFReceiveModeResponse>
{
    public SetRFReceiveModeResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetRFReceiveMode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the command was accepted.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static SetRFReceiveModeResponse Create(DataFrame frame) => new SetRFReceiveModeResponse(frame);
}
