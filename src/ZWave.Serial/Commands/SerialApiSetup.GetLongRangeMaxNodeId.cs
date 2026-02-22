namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the maximum Long Range node ID.
    /// </summary>
    public static SerialApiSetupRequest GetLongRangeMaxNodeId()
        => Create(SerialApiSetupSubcommand.GetLongRangeMaxNodeId, []);
}

/// <summary>
/// Response to a GetLongRangeMaxNodeId sub-command.
/// </summary>
public readonly struct SerialApiSetupGetLongRangeMaxNodeIdResponse : ICommand<SerialApiSetupGetLongRangeMaxNodeIdResponse>
{
    public SerialApiSetupGetLongRangeMaxNodeIdResponse(DataFrame frame)
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
    /// Gets the maximum Long Range node ID currently configured for the Z-Wave API (16-bit, big-endian).
    /// </summary>
    public ushort CurrentMaxNodeId => Frame.CommandParameters.Span[1..3].ToUInt16BE();

    /// <summary>
    /// Gets the maximum Long Range node ID supported by the Z-Wave API (16-bit, big-endian).
    /// </summary>
    public ushort MaxSupportedNodeId => Frame.CommandParameters.Span[3..5].ToUInt16BE();

    public static SerialApiSetupGetLongRangeMaxNodeIdResponse Create(DataFrame frame) => new SerialApiSetupGetLongRangeMaxNodeIdResponse(frame);
}
