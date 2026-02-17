namespace ZWave.Serial.Commands;

/// <summary>
/// Set the DCDC Configuration.
/// </summary>
public readonly struct SetDcdcConfigRequest : ICommand<SetDcdcConfigRequest>
{
    public SetDcdcConfigRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetDcdcConfig;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the DCDC configuration.
    /// </summary>
    /// <param name="config">The DCDC configuration value.</param>
    public static SetDcdcConfigRequest Create(byte config)
    {
        ReadOnlySpan<byte> commandParameters = [config];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetDcdcConfigRequest(frame);
    }

    public static SetDcdcConfigRequest Create(DataFrame frame) => new SetDcdcConfigRequest(frame);
}

public readonly struct SetDcdcConfigResponse : ICommand<SetDcdcConfigResponse>
{
    public SetDcdcConfigResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetDcdcConfig;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the configuration was successfully set.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static SetDcdcConfigResponse Create(DataFrame frame) => new SetDcdcConfigResponse(frame);
}
