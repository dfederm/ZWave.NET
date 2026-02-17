namespace ZWave.Serial.Commands;

public enum SetSucNodeIdRequestCapabilities : byte
{
    /// <summary>
    /// Enable the NodeID server functionality to become a SIS.
    /// </summary>
    SucFuncNodeIdServer = 0x01,
}

/// <summary>
/// Indicate the status regarding the configuration of a static/bridge controller to be SUC/SIS node
/// </summary>
public enum SetSucNodeIdStatus : byte
{
    /// <summary>
    /// The process of configuring the static/bridge controller is ended successfully
    /// </summary>
    Succeeded = 0x05,

    /// <summary>
    /// The process of configuring the static/bridge controller is failed.
    /// </summary>
    Failed = 0x06,
}

public readonly struct SetSucNodeIdRequest : IRequestWithCallback<SetSucNodeIdRequest>
{
    public SetSucNodeIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSucNodeId;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[4];

    public static SetSucNodeIdRequest Create(
        byte nodeId,
        bool enableSuc,
        SetSucNodeIdRequestCapabilities capabilities,
        TransmissionOptions transmissionOptions,
        byte sessionId)
    {
        ReadOnlySpan<byte> commandParameters =
        [
            nodeId, // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
            (byte)(enableSuc ? 1 : 0),
            (byte)transmissionOptions,
            (byte)capabilities,
            sessionId,
        ];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetSucNodeIdRequest(frame);
    }

    public static SetSucNodeIdRequest Create(DataFrame frame) => new SetSucNodeIdRequest(frame);
}

public readonly struct SetSucNodeIdCallback : ICommand<SetSucNodeIdCallback>
{
    public SetSucNodeIdCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSucNodeId;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// Indicate the status regarding the configuration of a static/bridge controller to be SUC/SIS node
    /// </summary>
    public SetSucNodeIdStatus SetSucNodeIdStatus => (SetSucNodeIdStatus)Frame.CommandParameters.Span[1];

    public static SetSucNodeIdCallback Create(DataFrame frame) => new SetSucNodeIdCallback(frame);
}
