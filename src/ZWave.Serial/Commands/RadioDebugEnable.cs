namespace ZWave.Serial.Commands;

/// <summary>
/// Enable or disable the radio debug interface.
/// </summary>
public readonly struct RadioDebugEnableRequest : ICommand<RadioDebugEnableRequest>
{
    public RadioDebugEnableRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.RadioDebugEnable;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a V2 request to enable or disable radio debug for a given protocol.
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <param name="debugInterfaceProtocol">The debug interface protocol.</param>
    /// <param name="configuration">Optional protocol-specific configuration data.</param>
    public static RadioDebugEnableRequest Create(bool enable, DebugInterfaceProtocol debugInterfaceProtocol, ReadOnlySpan<byte> configuration = default)
    {
        int length = 3 + (configuration.Length > 0 ? 1 + configuration.Length : 0);
        Span<byte> commandParameters = stackalloc byte[length];
        commandParameters[0] = (byte)(enable ? 0x01 : 0x00);
        ((ushort)debugInterfaceProtocol).WriteBytesBE(commandParameters[1..3]);

        if (configuration.Length > 0)
        {
            commandParameters[3] = (byte)configuration.Length;
            configuration.CopyTo(commandParameters[4..]);
        }

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new RadioDebugEnableRequest(frame);
    }

    public static RadioDebugEnableRequest Create(DataFrame frame, CommandParsingContext context) => new RadioDebugEnableRequest(frame);
}

public readonly struct RadioDebugEnableResponse : ICommand<RadioDebugEnableResponse>
{
    public RadioDebugEnableResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.RadioDebugEnable;

    public DataFrame Frame { get; }

    /// <summary>
    /// The command status (0x01 = success, 0x00 = failure).
    /// </summary>
    public byte CommandStatus => Frame.CommandParameters.Span[0];

    public static RadioDebugEnableResponse Create(DataFrame frame, CommandParsingContext context) => new RadioDebugEnableResponse(frame);
}
