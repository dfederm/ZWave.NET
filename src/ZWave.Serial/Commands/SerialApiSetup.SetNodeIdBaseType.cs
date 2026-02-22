namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to set the NodeID base type for the Serial API.
    /// </summary>
    /// <param name="nodeIdType">The NodeID encoding type to set.</param>
    public static SerialApiSetupRequest SetNodeIdBaseType(NodeIdType nodeIdType)
    {
        ReadOnlySpan<byte> subcommandParameters = [(byte)nodeIdType];
        return Create(SerialApiSetupSubcommand.SetNodeIdBaseType, subcommandParameters);
    }
}

/// <summary>
/// Response to a SetNodeIdBaseType sub-command.
/// </summary>
public readonly struct SerialApiSetupSetNodeIdBaseTypeResponse : ICommand<SerialApiSetupSetNodeIdBaseTypeResponse>
{
    public SerialApiSetupSetNodeIdBaseTypeResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the sub-command was supported by the Z-Wave module.
    /// The value 0 MUST indicate that the received Z-Wave API setup sub command in the Initial data frame is not supported.
    /// </summary>
    public bool WasSubcommandSupported => Frame.CommandParameters.Span[0] > 0;

    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[1] != 0;

    public static SerialApiSetupSetNodeIdBaseTypeResponse Create(DataFrame frame) => new SerialApiSetupSetNodeIdBaseTypeResponse(frame);
}
