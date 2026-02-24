namespace ZWave.Serial.Commands;

/// <summary>
/// Used to set node information for all Virtual Nodes in the embedded module.
/// </summary>
public readonly struct SetVirtualNodesApplicationNodeInformationRequest : ICommand<SetVirtualNodesApplicationNodeInformationRequest>
{
    public SetVirtualNodesApplicationNodeInformationRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetVirtualNodesApplicationNodeInformation;

    public DataFrame Frame { get; }

    /// <remarks>
    /// Per Z-Wave Host API Specification, the Virtual NodeID field MUST be encoded using 8 bits
    /// regardless of the configured NodeID base Type.
    /// </remarks>
    public static SetVirtualNodesApplicationNodeInformationRequest Create(
        ushort nodeId,
        byte deviceOptionMask,
        byte genericType,
        byte specificType,
        ReadOnlySpan<byte> commandClasses)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nodeId, (ushort)0xFF);
        Span<byte> commandParameters = stackalloc byte[5 + commandClasses.Length];
        commandParameters[0] = (byte)nodeId;
        commandParameters[1] = deviceOptionMask;
        commandParameters[2] = genericType;
        commandParameters[3] = specificType;
        commandParameters[4] = (byte)commandClasses.Length;
        commandClasses.CopyTo(commandParameters.Slice(5, commandClasses.Length));

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetVirtualNodesApplicationNodeInformationRequest(frame);
    }

    public static SetVirtualNodesApplicationNodeInformationRequest Create(DataFrame frame, CommandParsingContext context) => new SetVirtualNodesApplicationNodeInformationRequest(frame);
}
