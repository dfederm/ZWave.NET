namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get information about a supported RF region.
    /// </summary>
    /// <param name="region">The RF region to query.</param>
    public static SerialApiSetupRequest GetRegionInfo(RfRegion region)
    {
        ReadOnlySpan<byte> subcommandParameters = [(byte)region];
        return Create(SerialApiSetupSubcommand.GetRegionInfo, subcommandParameters);
    }
}

/// <summary>
/// Response to a GetRegionInfo sub-command.
/// </summary>
public readonly struct SerialApiSetupGetRegionInfoResponse : ICommand<SerialApiSetupGetRegionInfoResponse>
{
    public SerialApiSetupGetRegionInfoResponse(DataFrame frame)
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
    /// Gets the RF region this response describes.
    /// </summary>
    /// <remarks>
    /// If the region is not known by the Z-Wave module, this will be <see cref="RfRegion.Undefined"/>.
    /// </remarks>
    public RfRegion Region => (RfRegion)Frame.CommandParameters.Span[1];

    /// <summary>
    /// Gets a value indicating whether Z-Wave is supported in this region.
    /// </summary>
    public bool SupportsZWave => (Frame.CommandParameters.Span[2] & 0b0000_0001) != 0;

    /// <summary>
    /// Gets a value indicating whether Z-Wave Long Range is supported in this region.
    /// </summary>
    public bool SupportsZWaveLongRange => (Frame.CommandParameters.Span[2] & 0b0000_0010) != 0;

    /// <summary>
    /// Gets the RF region that this region is a superset of, if applicable.
    /// </summary>
    /// <remarks>
    /// Some Long Range regions are supersets of corresponding legacy regions.
    /// </remarks>
    public RfRegion IncludesRegion => (RfRegion)Frame.CommandParameters.Span[3];

    public static SerialApiSetupGetRegionInfoResponse Create(DataFrame frame) => new SerialApiSetupGetRegionInfoResponse(frame);
}
