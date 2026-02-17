namespace ZWave.Serial.Commands;

/// <summary>
/// Get the current power level used in RF transmitting.
/// </summary>
public readonly struct RFPowerLevelGetRequest : ICommand<RFPowerLevelGetRequest>
{
    public RFPowerLevelGetRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RFPowerLevelGet;

    public DataFrame Frame { get; }

    public static RFPowerLevelGetRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new RFPowerLevelGetRequest(frame);
    }

    public static RFPowerLevelGetRequest Create(DataFrame frame) => new RFPowerLevelGetRequest(frame);
}

public readonly struct RFPowerLevelGetResponse : ICommand<RFPowerLevelGetResponse>
{
    public RFPowerLevelGetResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.RFPowerLevelGet;

    public DataFrame Frame { get; }

    /// <summary>
    /// The current RF power level.
    /// </summary>
    public byte PowerLevel => Frame.CommandParameters.Span[0];

    public static RFPowerLevelGetResponse Create(DataFrame frame) => new RFPowerLevelGetResponse(frame);
}
