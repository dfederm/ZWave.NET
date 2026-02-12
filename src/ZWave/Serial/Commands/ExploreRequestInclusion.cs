namespace ZWave.Serial.Commands;

/// <summary>
/// Initiate a Network-Wide Inclusion process.
/// </summary>
public readonly struct ExploreRequestInclusionRequest : IRequestWithCallback<ExploreRequestInclusionRequest>
{
    public ExploreRequestInclusionRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ExploreRequestInclusion;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    public static ExploreRequestInclusionRequest Create(byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ExploreRequestInclusionRequest(frame);
    }

    public static ExploreRequestInclusionRequest Create(DataFrame frame) => new ExploreRequestInclusionRequest(frame);
}

/// <summary>
/// Callback for the <see cref="ExploreRequestInclusionRequest"/> command.
/// </summary>
public readonly struct ExploreRequestInclusionCallback : ICommand<ExploreRequestInclusionCallback>
{
    public ExploreRequestInclusionCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ExploreRequestInclusion;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the transmission.
    /// </summary>
    public TransmissionStatus Status => (TransmissionStatus)Frame.CommandParameters.Span[1];

    public static ExploreRequestInclusionCallback Create(DataFrame frame) => new ExploreRequestInclusionCallback(frame);
}
