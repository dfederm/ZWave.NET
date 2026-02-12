namespace ZWave.Serial.Commands;

/// <summary>
/// Set the Controller back to the factory default state.
/// </summary>
public readonly struct SetDefaultRequest : IRequestWithCallback<SetDefaultRequest>
{
    public SetDefaultRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetDefault;

    public static bool ExpectsResponseStatus => false;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public static SetDefaultRequest Create(byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetDefaultRequest(frame);
    }

    public static SetDefaultRequest Create(DataFrame frame) => new SetDefaultRequest(frame);
}

/// <summary>
/// Callback for the <see cref="SetDefaultRequest"/> command.
/// </summary>
public readonly struct SetDefaultCallback : ICommand<SetDefaultCallback>
{
    public SetDefaultCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetDefault;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    public static SetDefaultCallback Create(DataFrame frame) => new SetDefaultCallback(frame);
}
