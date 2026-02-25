namespace ZWave.Serial.Commands;

/// <summary>
/// The mode for the add primary controller operation.
/// </summary>
public enum AddPrimaryControllerMode : byte
{
    /// <summary>
    /// Start the add primary controller process.
    /// </summary>
    Start = 0x01,

    /// <summary>
    /// Stop the add primary controller process.
    /// </summary>
    Stop = 0x05,

    /// <summary>
    /// Stop the add primary controller process due to a failure.
    /// </summary>
    StopDueToFailure = 0x06,
}

/// <summary>
/// The status of the add primary controller operation.
/// </summary>
public enum AddPrimaryControllerStatus : byte
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
/// Add a controller to the Z-Wave network as the new primary controller.
/// </summary>
public readonly struct AddPrimaryControllerRequest : ICommand<AddPrimaryControllerRequest>
{
    public AddPrimaryControllerRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AddPrimaryController;

    public DataFrame Frame { get; }

    public static AddPrimaryControllerRequest Create(
        AddPrimaryControllerMode mode,
        bool isHighPower,
        bool isNetworkWide,
        byte sessionId)
    {
        byte flags = (byte)mode;
        if (isHighPower)
        {
            flags |= 0x80;
        }

        if (isNetworkWide)
        {
            flags |= 0x40;
        }

        ReadOnlySpan<byte> commandParameters = [flags, sessionId];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new AddPrimaryControllerRequest(frame);
    }

    public static AddPrimaryControllerRequest Create(DataFrame frame, CommandParsingContext context) => new AddPrimaryControllerRequest(frame);
}

/// <summary>
/// Callback for the <see cref="AddPrimaryControllerRequest"/> command.
/// </summary>
public readonly struct AddPrimaryControllerCallback : ICommand<AddPrimaryControllerCallback>
{
    public AddPrimaryControllerCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.AddPrimaryController;

    public DataFrame Frame { get; }

    /// <summary>
    /// The session ID for correlating the callback with the request.
    /// </summary>
    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The status of the add primary controller operation.
    /// </summary>
    public AddPrimaryControllerStatus Status => (AddPrimaryControllerStatus)Frame.CommandParameters.Span[1];

    /// <summary>
    /// The node ID assigned during the add primary controller operation.
    /// </summary>
    public ushort AssignedNodeId => Frame.CommandParameters.Span[2];

    /// <summary>
    /// The Basic Device Class of the included node.
    /// </summary>
    public byte BasicDeviceClass => Frame.CommandParameters.Span[4];

    /// <summary>
    /// The Generic Device Class of the included node.
    /// </summary>
    public byte GenericDeviceClass => Frame.CommandParameters.Span[5];

    /// <summary>
    /// The Specific Device Class of the included node.
    /// </summary>
    public byte SpecificDeviceClass => Frame.CommandParameters.Span[6];

    /// <summary>
    /// The list of non-secure implemented Command Classes by the included node.
    /// </summary>
    public IReadOnlyList<CommandClassInfo> CommandClasses
    {
        get
        {
            byte length = Frame.CommandParameters.Span[3];
            ReadOnlySpan<byte> allCommandClasses = Frame.CommandParameters.Span.Slice(7, length);
            return CommandClassInfo.ParseList(allCommandClasses);
        }
    }

    public static AddPrimaryControllerCallback Create(DataFrame frame, CommandParsingContext context) => new AddPrimaryControllerCallback(frame);
}
