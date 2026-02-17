namespace ZWave.Serial.Commands;

/// <summary>
/// Retrieves the current Network Statistics as collected by the Z-Wave protocol.
/// </summary>
public readonly struct GetNetworkStatsRequest : ICommand<GetNetworkStatsRequest>
{
    public GetNetworkStatsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetNetworkStats;

    public DataFrame Frame { get; }

    public static GetNetworkStatsRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetNetworkStatsRequest(frame);
    }

    public static GetNetworkStatsRequest Create(DataFrame frame) => new GetNetworkStatsRequest(frame);
}

public readonly struct GetNetworkStatsResponse : ICommand<GetNetworkStatsResponse>
{
    public GetNetworkStatsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetNetworkStats;

    public DataFrame Frame { get; }

    /// <summary>
    /// The raw network statistics data.
    /// </summary>
    public ReadOnlyMemory<byte> Data => Frame.CommandParameters;

    public static GetNetworkStatsResponse Create(DataFrame frame) => new GetNetworkStatsResponse(frame);
}
