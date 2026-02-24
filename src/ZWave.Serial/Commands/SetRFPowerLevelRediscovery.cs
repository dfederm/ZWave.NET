namespace ZWave.Serial.Commands;

/// <summary>
/// Set the power level locally in the node when finding neighbors.
/// </summary>
public readonly struct SetRFPowerLevelRediscoveryRequest : ICommand<SetRFPowerLevelRediscoveryRequest>
{
    public SetRFPowerLevelRediscoveryRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetRFPowerLevelRediscovery;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the RF power level for neighbor discovery.
    /// </summary>
    /// <param name="powerLevel">The power level to set.</param>
    public static SetRFPowerLevelRediscoveryRequest Create(byte powerLevel)
    {
        ReadOnlySpan<byte> commandParameters = [powerLevel];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetRFPowerLevelRediscoveryRequest(frame);
    }

    public static SetRFPowerLevelRediscoveryRequest Create(DataFrame frame, CommandParsingContext context) => new SetRFPowerLevelRediscoveryRequest(frame);
}

public readonly struct SetRFPowerLevelRediscoveryResponse : ICommand<SetRFPowerLevelRediscoveryResponse>
{
    public SetRFPowerLevelRediscoveryResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetRFPowerLevelRediscovery;

    public DataFrame Frame { get; }

    /// <summary>
    /// The actual power level set.
    /// </summary>
    public byte PowerLevel => Frame.CommandParameters.Span[0];

    public static SetRFPowerLevelRediscoveryResponse Create(DataFrame frame, CommandParsingContext context) => new SetRFPowerLevelRediscoveryResponse(frame);
}
