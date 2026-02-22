namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the current RF region.
    /// </summary>
    public static SerialApiSetupRequest GetRfRegion()
        => Create(SerialApiSetupSubcommand.GetRFRegion, []);
}

/// <summary>
/// Response to a GetRFRegion sub-command.
/// </summary>
public readonly struct SerialApiSetupGetRfRegionResponse : ICommand<SerialApiSetupGetRfRegionResponse>
{
    public SerialApiSetupGetRfRegionResponse(DataFrame frame)
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
    /// Gets the current RF region.
    /// </summary>
    public RfRegion Region => (RfRegion)Frame.CommandParameters.Span[1];

    public static SerialApiSetupGetRfRegionResponse Create(DataFrame frame) => new SerialApiSetupGetRfRegionResponse(frame);
}
