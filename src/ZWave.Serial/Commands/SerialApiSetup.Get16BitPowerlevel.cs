namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the TX powerlevel as 16-bit values.
    /// </summary>
    public static SerialApiSetupRequest Get16BitPowerlevel()
        => Create(SerialApiSetupSubcommand.Get16BitPowerlevel, []);
}

/// <summary>
/// Response to a Get16BitPowerlevel sub-command.
/// </summary>
public readonly struct SerialApiSetupGet16BitPowerlevelResponse : ICommand<SerialApiSetupGet16BitPowerlevelResponse>
{
    public SerialApiSetupGet16BitPowerlevelResponse(DataFrame frame)
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
    /// Gets the normal transmit powerlevel in deci-dBm (signed 16-bit).
    /// </summary>
    public short NormalPowerlevelDeciDbm => Frame.CommandParameters.Span[1..3].ToInt16BE();

    /// <summary>
    /// Gets the measured 0 dBm powerlevel in deci-dBm (signed 16-bit).
    /// </summary>
    public short Measured0dBmPowerlevelDeciDbm => Frame.CommandParameters.Span[3..5].ToInt16BE();

    public static SerialApiSetupGet16BitPowerlevelResponse Create(DataFrame frame) => new SerialApiSetupGet16BitPowerlevelResponse(frame);
}
