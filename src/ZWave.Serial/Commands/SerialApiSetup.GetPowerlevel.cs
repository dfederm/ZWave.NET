namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the TX powerlevel of the Z-Wave API.
    /// </summary>
    public static SerialApiSetupRequest GetPowerlevel()
        => Create(SerialApiSetupSubcommand.GetPowerlevel, []);
}

/// <summary>
/// Response to a GetPowerlevel sub-command.
/// </summary>
public readonly struct SerialApiSetupGetPowerlevelResponse : ICommand<SerialApiSetupGetPowerlevelResponse>
{
    public SerialApiSetupGetPowerlevelResponse(DataFrame frame)
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
    /// Gets the normal transmit powerlevel in deci-dBm (signed 8-bit).
    /// </summary>
    public sbyte NormalPowerDeciDbm => Frame.CommandParameters.Span[1].ToInt8();

    /// <summary>
    /// Gets the measured 0 dBm powerlevel in deci-dBm (signed 8-bit).
    /// </summary>
    public sbyte Measured0dBmDeciDbm => Frame.CommandParameters.Span[2].ToInt8();

    public static SerialApiSetupGetPowerlevelResponse Create(DataFrame frame, CommandParsingContext context) => new SerialApiSetupGetPowerlevelResponse(frame);
}
