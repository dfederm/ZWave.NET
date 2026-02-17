namespace ZWave.Serial.Commands;

/// <summary>
/// Clears the current Network Statistics collected by the Z-Wave protocol.
/// </summary>
public readonly struct ClearNetworkStatsRequest : ICommand<ClearNetworkStatsRequest>
{
    public ClearNetworkStatsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ClearNetworkStats;

    public DataFrame Frame { get; }

    public static ClearNetworkStatsRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new ClearNetworkStatsRequest(frame);
    }

    public static ClearNetworkStatsRequest Create(DataFrame frame) => new ClearNetworkStatsRequest(frame);
}
