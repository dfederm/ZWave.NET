namespace ZWave.Serial.Commands;

/// <summary>
/// Set the SoC in a specified power down mode.
/// </summary>
public readonly struct SetSleepModeRequest : ICommand<SetSleepModeRequest>
{
    public SetSleepModeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSleepMode;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the sleep mode.
    /// </summary>
    /// <param name="mode">The sleep mode to set. See INS12350 for mode definitions.</param>
    /// <param name="intEnable">The interrupt enable bits.</param>
    public static SetSleepModeRequest Create(byte mode, byte intEnable)
    {
        ReadOnlySpan<byte> commandParameters = [mode, intEnable];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetSleepModeRequest(frame);
    }

    public static SetSleepModeRequest Create(DataFrame frame) => new SetSleepModeRequest(frame);
}
