namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the maximum payload size for Z-Wave frames.
    /// </summary>
    public static SerialApiSetupRequest GetMaxPayloadSize()
        => Create(SerialApiSetupSubcommand.GetMaxPayloadSize, []);
}

/// <summary>
/// Response to a GetMaxPayloadSize sub-command.
/// </summary>
public readonly struct SerialApiSetupGetMaxPayloadSizeResponse : ICommand<SerialApiSetupGetMaxPayloadSizeResponse>
{
    public SerialApiSetupGetMaxPayloadSizeResponse(DataFrame frame)
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
    /// Gets the maximum payload size for Z-Wave frames, in bytes.
    /// </summary>
    public byte MaxPayloadSize => Frame.CommandParameters.Span[1];

    public static SerialApiSetupGetMaxPayloadSizeResponse Create(DataFrame frame) => new SerialApiSetupGetMaxPayloadSizeResponse(frame);
}
