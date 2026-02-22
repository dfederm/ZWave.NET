namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to set the maximum Long Range node ID.
    /// </summary>
    /// <param name="maxNodeId">The maximum LR node ID (16-bit).</param>
    public static SerialApiSetupRequest SetLongRangeMaxNodeId(ushort maxNodeId)
    {
        Span<byte> subcommandParameters = stackalloc byte[2];
        maxNodeId.WriteBytesBE(subcommandParameters);
        return Create(SerialApiSetupSubcommand.SetLongRangeMaxNodeId, subcommandParameters);
    }
}

/// <summary>
/// Response to a SetLongRangeMaxNodeId sub-command.
/// </summary>
public readonly struct SerialApiSetupSetLongRangeMaxNodeIdResponse : ICommand<SerialApiSetupSetLongRangeMaxNodeIdResponse>
{
    public SerialApiSetupSetLongRangeMaxNodeIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the sub-command was supported by the Z-Wave module.
    /// </summary>
    public bool WasSubcommandSupported => Frame.CommandParameters.Span[0] > 0;

    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Success => Frame.CommandParameters.Span[1] != 0;

    public static SerialApiSetupSetLongRangeMaxNodeIdResponse Create(DataFrame frame) => new SerialApiSetupSetLongRangeMaxNodeIdResponse(frame);
}
