namespace ZWave.Serial.Commands;

/// <summary>
/// Used to set node information for all Virtual Slave Nodes in the embedded module.
/// </summary>
public readonly struct SerialApiApplicationSlaveNodeInformationRequest : ICommand<SerialApiApplicationSlaveNodeInformationRequest>
{
    public SerialApiApplicationSlaveNodeInformationRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiApplicationSlaveNodeInformation;

    public DataFrame Frame { get; }

    public static SerialApiApplicationSlaveNodeInformationRequest Create(
        byte nodeId,
        byte deviceOptionMask,
        byte genericType,
        byte specificType,
        ReadOnlySpan<byte> commandClasses)
    {
        Span<byte> commandParameters = stackalloc byte[5 + commandClasses.Length];
        commandParameters[0] = nodeId;
        commandParameters[1] = deviceOptionMask;
        commandParameters[2] = genericType;
        commandParameters[3] = specificType;
        commandParameters[4] = (byte)commandClasses.Length;
        commandClasses.CopyTo(commandParameters.Slice(5, commandClasses.Length));

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SerialApiApplicationSlaveNodeInformationRequest(frame);
    }

    public static SerialApiApplicationSlaveNodeInformationRequest Create(DataFrame frame) => new SerialApiApplicationSlaveNodeInformationRequest(frame);
}
