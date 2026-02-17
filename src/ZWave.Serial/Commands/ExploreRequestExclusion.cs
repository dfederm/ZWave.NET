namespace ZWave.Serial.Commands;

/// <summary>
/// Initiate a Network-Wide Exclusion process.
/// </summary>
public readonly struct ExploreRequestExclusionRequest : IRequestWithCallback<ExploreRequestExclusionRequest>
{
    public ExploreRequestExclusionRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ExploreRequestExclusion;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public static ExploreRequestExclusionRequest Create(byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ExploreRequestExclusionRequest(frame);
    }

    public static ExploreRequestExclusionRequest Create(DataFrame frame) => new ExploreRequestExclusionRequest(frame);
}

/// <summary>
/// Callback for the <see cref="ExploreRequestExclusionRequest"/> command.
/// </summary>
public readonly struct ExploreRequestExclusionCallback : ICommand<ExploreRequestExclusionCallback>
{
    public ExploreRequestExclusionCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ExploreRequestExclusion;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static ExploreRequestExclusionCallback Create(DataFrame frame) => new ExploreRequestExclusionCallback(frame);
}
