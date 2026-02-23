namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// The minimum allowed Long Range powerlevel in deci-dBm.
    /// </summary>
    /// <remarks>
    /// Defined in the Z-Wave Host API Specification, section 4.3.16.3.
    /// </remarks>
    public const short MinLongRangePowerlevelDeciDbm = -60;

    /// <summary>
    /// The maximum allowed Long Range powerlevel in deci-dBm (20 dBm board).
    /// </summary>
    /// <remarks>
    /// Defined in the Z-Wave Host API Specification, section 4.3.16.3.
    /// The valid range is -60 to 140 for a 14 dBm board and -60 to 200 for a 20 dBm board.
    /// </remarks>
    public const short MaxLongRangePowerlevelDeciDbm = 200;

    /// <summary>
    /// Create a request to set the maximum Long Range transmit powerlevel.
    /// </summary>
    /// <param name="maxPowerlevelDeciDbm">The maximum LR powerlevel in deci-dBm. Valid range is -60 to 200.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPowerlevelDeciDbm"/> is outside the valid range.</exception>
    public static SerialApiSetupRequest SetMaxLongRangePowerlevel(short maxPowerlevelDeciDbm)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPowerlevelDeciDbm, MinLongRangePowerlevelDeciDbm);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxPowerlevelDeciDbm, MaxLongRangePowerlevelDeciDbm);

        Span<byte> subcommandParameters = stackalloc byte[2];
        maxPowerlevelDeciDbm.WriteBytesBE(subcommandParameters);
        return Create(SerialApiSetupSubcommand.SetMaxLongRangePowerlevel, subcommandParameters);
    }
}

/// <summary>
/// Response to a SetMaxLongRangePowerlevel sub-command.
/// </summary>
public readonly struct SerialApiSetupSetMaxLongRangePowerlevelResponse : ICommand<SerialApiSetupSetMaxLongRangePowerlevelResponse>
{
    public SerialApiSetupSetMaxLongRangePowerlevelResponse(DataFrame frame)
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

    public static SerialApiSetupSetMaxLongRangePowerlevelResponse Create(DataFrame frame, CommandParsingContext context) => new SerialApiSetupSetMaxLongRangePowerlevelResponse(frame);
}
