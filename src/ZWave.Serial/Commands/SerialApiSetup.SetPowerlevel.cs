namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to set the TX powerlevel for the Z-Wave API.
    /// </summary>
    /// <param name="normalPowerDeciDbm">The normal transmit powerlevel in deci-dBm (signed 8-bit).</param>
    /// <param name="measured0dBmDeciDbm">The measured 0 dBm powerlevel in deci-dBm (signed 8-bit).</param>
    public static SerialApiSetupRequest SetPowerlevel(sbyte normalPowerDeciDbm, sbyte measured0dBmDeciDbm)
    {
        ReadOnlySpan<byte> subcommandParameters = [unchecked((byte)normalPowerDeciDbm), unchecked((byte)measured0dBmDeciDbm)];
        return Create(SerialApiSetupSubcommand.SetPowerlevel, subcommandParameters);
    }
}

/// <summary>
/// Response to a SetPowerlevel sub-command.
/// </summary>
public readonly struct SerialApiSetupSetPowerlevelResponse : ICommand<SerialApiSetupSetPowerlevelResponse>
{
    public SerialApiSetupSetPowerlevelResponse(DataFrame frame)
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

    public static SerialApiSetupSetPowerlevelResponse Create(DataFrame frame) => new SerialApiSetupSetPowerlevelResponse(frame);
}
