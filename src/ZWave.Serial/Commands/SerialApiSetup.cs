namespace ZWave.Serial.Commands;

public enum SerialApiSetupSubcommand : byte
{
    /// <summary>
    /// Request the list of Z-Wave API Setup Sub Commands that are supported by the Z-Wave API Module
    /// </summary>
    GetSupportedCommands = 0x01,

    /// <summary>
    /// Configure the Z-Wave API Module to return detailed Tx Status Report after sending a frame to a destination
    /// </summary>
    SetTxStatusReport = 0x02,

    /// <summary>
    /// Configure the Tx Powerlevel setting of the Z-Wave API.
    /// </summary>
    SetPowerlevel = 0x04,

    /// <summary>
    /// Request the Powerlevel setting of the Z-Wave API.
    /// </summary>
    GetPowerlevel = 0x08,

    /// <summary>
    /// Request the maximum payload that the Z-Wave API Module can accept for transmitting Z-Wave frames.
    /// </summary>
    GetMaxPayloadSize = 0x10,

    /// <summary>
    /// Request the maximum payload that the Z-Wave API Module can accept for transmitting Z-Wave Long Range frames.
    /// </summary>
    GetLongRangeMaxPayloadSize = 0x11,

    /// <summary>
    /// Request the current RF region configured at the Z-Wave API Module.
    /// </summary>
    GetRFRegion = 0x20,

    /// <summary>
    /// Configure the RF region at the Z-Wave API Module.
    /// </summary>
    SetRFRegion = 0x40,

    /// <summary>
    /// Configure the NodeID base type for the Z-Wave API.
    /// </summary>
    SetNodeIdBaseType = 0x80,
}

public readonly partial struct SerialApiSetupRequest : ICommand<SerialApiSetupRequest>
{
    public SerialApiSetupRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    private static SerialApiSetupRequest Create(SerialApiSetupSubcommand subcommand, ReadOnlySpan<byte> subcommandParameters)
    {
        Span<byte> commandParameters = stackalloc byte[subcommandParameters.Length + 1];
        commandParameters[0] = (byte)subcommand;
        subcommandParameters.CopyTo(commandParameters[1..]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SerialApiSetupRequest(frame);
    }

    public static SerialApiSetupRequest Create(DataFrame frame) => new SerialApiSetupRequest(frame);
}
