namespace ZWave.Serial.Commands;

/// <summary>
/// Get the DCDC Configuration.
/// </summary>
public readonly struct GetDcdcConfigRequest : ICommand<GetDcdcConfigRequest>
{
    public GetDcdcConfigRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetDcdcConfig;

    public DataFrame Frame { get; }

    public static GetDcdcConfigRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetDcdcConfigRequest(frame);
    }

    public static GetDcdcConfigRequest Create(DataFrame frame) => new GetDcdcConfigRequest(frame);
}

public readonly struct GetDcdcConfigResponse : ICommand<GetDcdcConfigResponse>
{
    public GetDcdcConfigResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetDcdcConfig;

    public DataFrame Frame { get; }

    /// <summary>
    /// The DCDC configuration value.
    /// </summary>
    public byte Config => Frame.CommandParameters.Span[0];

    public static GetDcdcConfigResponse Create(DataFrame frame) => new GetDcdcConfigResponse(frame);
}
