namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to set the TX powerlevel using 16-bit values.
    /// </summary>
    /// <param name="normalPowerlevelDeciDbm">The normal transmit powerlevel in deci-dBm (signed 16-bit).</param>
    /// <param name="measured0dBmPowerlevelDeciDbm">The measured 0 dBm powerlevel in deci-dBm (signed 16-bit).</param>
    public static SerialApiSetupRequest Set16BitPowerlevel(short normalPowerlevelDeciDbm, short measured0dBmPowerlevelDeciDbm)
    {
        Span<byte> subcommandParameters = stackalloc byte[4];
        normalPowerlevelDeciDbm.WriteBytesBE(subcommandParameters[..2]);
        measured0dBmPowerlevelDeciDbm.WriteBytesBE(subcommandParameters[2..]);
        return Create(SerialApiSetupSubcommand.Set16BitPowerlevel, subcommandParameters);
    }
}

/// <summary>
/// Response to a Set16BitPowerlevel sub-command.
/// </summary>
public readonly struct SerialApiSetupSet16BitPowerlevelResponse : ICommand<SerialApiSetupSet16BitPowerlevelResponse>
{
    public SerialApiSetupSet16BitPowerlevelResponse(DataFrame frame)
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
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[1] != 0;

    public static SerialApiSetupSet16BitPowerlevelResponse Create(DataFrame frame, CommandParsingContext context) => new SerialApiSetupSet16BitPowerlevelResponse(frame);
}
