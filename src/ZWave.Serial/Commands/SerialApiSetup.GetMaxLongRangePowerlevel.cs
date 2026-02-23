namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the maximum Long Range transmit powerlevel.
    /// </summary>
    public static SerialApiSetupRequest GetMaxLongRangePowerlevel()
        => Create(SerialApiSetupSubcommand.GetMaxLongRangePowerlevel, []);
}

/// <summary>
/// Response to a GetMaxLongRangePowerlevel sub-command.
/// </summary>
public readonly struct SerialApiSetupGetMaxLongRangePowerlevelResponse : ICommand<SerialApiSetupGetMaxLongRangePowerlevelResponse>
{
    public SerialApiSetupGetMaxLongRangePowerlevelResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the sub-command was supported by the Z-Wave module.
    /// </summary>
    public bool WasSubcommandSupported => Frame.CommandParameters.Span[0] > 0;

    /// <summary>
    /// Gets the maximum Long Range powerlevel in deci-dBm (signed 16-bit, big-endian).
    /// </summary>
    public short MaxPowerlevelDeciDbm => Frame.CommandParameters.Span[1..3].ToInt16BE();

    public static SerialApiSetupGetMaxLongRangePowerlevelResponse Create(DataFrame frame, CommandParsingContext context) => new SerialApiSetupGetMaxLongRangePowerlevelResponse(frame);
}
