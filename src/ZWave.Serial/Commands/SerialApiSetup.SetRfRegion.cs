namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to set the RF region.
    /// </summary>
    /// <param name="region">The RF region to configure.</param>
    /// <remarks>
    /// The RF Region will only be in use by the Z-Wave API Module after it is restarted.
    /// A host application SHOULD issue a Soft Reset Command after configuring the RF Region.
    /// </remarks>
    public static SerialApiSetupRequest SetRfRegion(RfRegion region)
    {
        ReadOnlySpan<byte> subcommandParameters = [(byte)region];
        return Create(SerialApiSetupSubcommand.SetRFRegion, subcommandParameters);
    }
}

/// <summary>
/// Response to a SetRFRegion sub-command.
/// </summary>
public readonly struct SerialApiSetupSetRfRegionResponse : ICommand<SerialApiSetupSetRfRegionResponse>
{
    public SerialApiSetupSetRfRegionResponse(DataFrame frame)
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

    public static SerialApiSetupSetRfRegionResponse Create(DataFrame frame, CommandParsingContext context) => new SerialApiSetupSetRfRegionResponse(frame);
}
