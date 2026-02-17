namespace ZWave.Serial.Commands;

/// <summary>
/// Enable or disable promiscuous mode.
/// </summary>
public readonly struct SetPromiscuousModeRequest : ICommand<SetPromiscuousModeRequest>
{
    public SetPromiscuousModeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetPromiscuousMode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to enable or disable promiscuous mode.
    /// </summary>
    /// <param name="enable">True to enable promiscuous mode, false to disable.</param>
    public static SetPromiscuousModeRequest Create(bool enable)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)(enable ? 1 : 0)];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetPromiscuousModeRequest(frame);
    }

    public static SetPromiscuousModeRequest Create(DataFrame frame) => new SetPromiscuousModeRequest(frame);
}
