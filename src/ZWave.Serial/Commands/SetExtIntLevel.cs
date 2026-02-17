namespace ZWave.Serial.Commands;

/// <summary>
/// Set the trigger level for external interrupts.
/// </summary>
public readonly struct SetExtIntLevelRequest : ICommand<SetExtIntLevelRequest>
{
    public SetExtIntLevelRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetExtIntLevel;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the external interrupt trigger level.
    /// </summary>
    /// <param name="pin">The pin number.</param>
    /// <param name="triggerLevel">The trigger level for the pin.</param>
    public static SetExtIntLevelRequest Create(byte pin, byte triggerLevel)
    {
        ReadOnlySpan<byte> commandParameters = [pin, triggerLevel];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetExtIntLevelRequest(frame);
    }

    public static SetExtIntLevelRequest Create(DataFrame frame) => new SetExtIntLevelRequest(frame);
}
