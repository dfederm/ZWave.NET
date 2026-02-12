namespace ZWave.Serial.Commands;

/// <summary>
/// Set the power level locally in the node when finding neighbors.
/// </summary>
public readonly struct RFPowerlevelRediscoverySetRequest : ICommand<RFPowerlevelRediscoverySetRequest>
{
    public RFPowerlevelRediscoverySetRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RFPowerlevelRediscoverySet;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the RF power level for neighbor discovery.
    /// </summary>
    /// <param name="powerLevel">The power level to set.</param>
    public static RFPowerlevelRediscoverySetRequest Create(byte powerLevel)
    {
        ReadOnlySpan<byte> commandParameters = [powerLevel];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RFPowerlevelRediscoverySetRequest(frame);
    }

    public static RFPowerlevelRediscoverySetRequest Create(DataFrame frame) => new RFPowerlevelRediscoverySetRequest(frame);
}

public readonly struct RFPowerlevelRediscoverySetResponse : ICommand<RFPowerlevelRediscoverySetResponse>
{
    public RFPowerlevelRediscoverySetResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.RFPowerlevelRediscoverySet;

    public DataFrame Frame { get; }

    /// <summary>
    /// The actual power level set.
    /// </summary>
    public byte PowerLevel => Frame.CommandParameters.Span[0];

    public static RFPowerlevelRediscoverySetResponse Create(DataFrame frame) => new RFPowerlevelRediscoverySetResponse(frame);
}
