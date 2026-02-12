namespace ZWave.Serial.Commands;

/// <summary>
/// Set the power level used for RF transmission.
/// </summary>
public readonly struct RFPowerLevelSetRequest : ICommand<RFPowerLevelSetRequest>
{
    public RFPowerLevelSetRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RFPowerLevelSet;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the RF power level.
    /// </summary>
    /// <param name="powerLevel">The power level to set (0x00 = normal, 0x01-0x09 = decreasing power).</param>
    public static RFPowerLevelSetRequest Create(byte powerLevel)
    {
        ReadOnlySpan<byte> commandParameters = [powerLevel];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RFPowerLevelSetRequest(frame);
    }

    public static RFPowerLevelSetRequest Create(DataFrame frame) => new RFPowerLevelSetRequest(frame);
}

public readonly struct RFPowerLevelSetResponse : ICommand<RFPowerLevelSetResponse>
{
    public RFPowerLevelSetResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.RFPowerLevelSet;

    public DataFrame Frame { get; }

    /// <summary>
    /// The actual power level set.
    /// </summary>
    public byte PowerLevel => Frame.CommandParameters.Span[0];

    public static RFPowerLevelSetResponse Create(DataFrame frame) => new RFPowerLevelSetResponse(frame);
}
