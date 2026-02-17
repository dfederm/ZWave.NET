namespace ZWave.Serial.Commands;

/// <summary>
/// Set the maximum interval between SmartStart inclusion requests.
/// </summary>
public readonly struct NetworkManagementSetMaxInclusionRequestIntervalsRequest : ICommand<NetworkManagementSetMaxInclusionRequestIntervalsRequest>
{
    public NetworkManagementSetMaxInclusionRequestIntervalsRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NetworkManagementSetMaxInclusionRequestIntervals;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the maximum inclusion request intervals.
    /// </summary>
    /// <param name="intervals">The maximum interval value.</param>
    public static NetworkManagementSetMaxInclusionRequestIntervalsRequest Create(byte intervals)
    {
        ReadOnlySpan<byte> commandParameters = [intervals];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NetworkManagementSetMaxInclusionRequestIntervalsRequest(frame);
    }

    public static NetworkManagementSetMaxInclusionRequestIntervalsRequest Create(DataFrame frame) => new NetworkManagementSetMaxInclusionRequestIntervalsRequest(frame);
}

public readonly struct NetworkManagementSetMaxInclusionRequestIntervalsResponse : ICommand<NetworkManagementSetMaxInclusionRequestIntervalsResponse>
{
    public NetworkManagementSetMaxInclusionRequestIntervalsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NetworkManagementSetMaxInclusionRequestIntervals;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the command was accepted.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[0] != 0;

    public static NetworkManagementSetMaxInclusionRequestIntervalsResponse Create(DataFrame frame) => new NetworkManagementSetMaxInclusionRequestIntervalsResponse(frame);
}
