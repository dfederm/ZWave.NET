namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the maximum Long Range payload size.
    /// </summary>
    public static SerialApiSetupRequest GetLongRangeMaxPayloadSize()
        => Create(SerialApiSetupSubcommand.GetLongRangeMaxPayloadSize, []);
}

/// <summary>
/// Response to a GetLongRangeMaxPayloadSize sub-command.
/// </summary>
public readonly struct SerialApiSetupGetLongRangeMaxPayloadSizeResponse : ICommand<SerialApiSetupGetLongRangeMaxPayloadSizeResponse>
{
    public SerialApiSetupGetLongRangeMaxPayloadSizeResponse(DataFrame frame)
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
    /// Gets the maximum payload size for Long Range frames, in bytes.
    /// </summary>
    public byte MaxPayloadSize => Frame.CommandParameters.Span[1];

    public static SerialApiSetupGetLongRangeMaxPayloadSizeResponse Create(DataFrame frame, CommandParsingContext context) => new SerialApiSetupGetLongRangeMaxPayloadSizeResponse(frame);
}
