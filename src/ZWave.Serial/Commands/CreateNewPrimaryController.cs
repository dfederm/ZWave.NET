namespace ZWave.Serial.Commands;

/// <summary>
/// The mode for the create new primary controller operation.
/// </summary>
public enum CreateNewPrimaryControllerMode : byte
{
    /// <summary>
    /// Start the create new primary controller process.
    /// </summary>
    Start = 0x02,

    /// <summary>
    /// Stop the create new primary controller process.
    /// </summary>
    Stop = 0x05,

    /// <summary>
    /// Start the create new primary controller process using network-wide inclusion.
    /// </summary>
    StartNetworkWide = 0x42,
}

/// <summary>
/// The status of the create new primary controller operation.
/// </summary>
public enum CreateNewPrimaryControllerStatus : byte
{
    /// <summary>
    /// A node requesting inclusion has been found.
    /// </summary>
    NodeFound = 0x02,

    /// <summary>
    /// The inclusion is ongoing.
    /// </summary>
    InclusionOngoing = 0x03,

    /// <summary>
    /// The inclusion has completed successfully.
    /// </summary>
    InclusionCompleted = 0x05,

    /// <summary>
    /// The inclusion has failed.
    /// </summary>
    InclusionFailed = 0x07,
}

/// <summary>
/// Add a controller to the Z-Wave network as a replacement for the old primary controller.
/// </summary>
public readonly struct CreateNewPrimaryControllerRequest : ICommand<CreateNewPrimaryControllerRequest>
{
    public CreateNewPrimaryControllerRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.CreateNewPrimaryController;

    public DataFrame Frame { get; }

    public static CreateNewPrimaryControllerRequest Create(
        CreateNewPrimaryControllerMode mode,
        bool isHighPower,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)((byte)mode | (isHighPower ? 0x80 : 0)), sessionId];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new CreateNewPrimaryControllerRequest(frame);
    }

    public static CreateNewPrimaryControllerRequest Create(DataFrame frame) => new CreateNewPrimaryControllerRequest(frame);
}

/// <summary>
/// Callback for the <see cref="CreateNewPrimaryControllerRequest"/> command.
/// </summary>
public readonly struct CreateNewPrimaryControllerCallback : ICommand<CreateNewPrimaryControllerCallback>
{
    public CreateNewPrimaryControllerCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.CreateNewPrimaryController;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the create new primary controller operation.
    /// </summary>
    public CreateNewPrimaryControllerStatus Status => (CreateNewPrimaryControllerStatus)Frame.CommandParameters.Span[1];

    public static CreateNewPrimaryControllerCallback Create(DataFrame frame) => new CreateNewPrimaryControllerCallback(frame);
}
