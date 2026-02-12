namespace ZWave.Serial.Commands;

/// <summary>
/// Read a value from the NVR Flash memory area.
/// </summary>
public readonly struct NvrGetValueRequest : ICommand<NvrGetValueRequest>
{
    public NvrGetValueRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NvrGetValue;

    public DataFrame Frame { get; }

    public static NvrGetValueRequest Create(byte offset, byte length)
    {
        ReadOnlySpan<byte> commandParameters = [offset, length];
        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new NvrGetValueRequest(frame);
    }

    public static NvrGetValueRequest Create(DataFrame frame) => new NvrGetValueRequest(frame);
}

public readonly struct NvrGetValueResponse : ICommand<NvrGetValueResponse>
{
    public NvrGetValueResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NvrGetValue;

    public DataFrame Frame { get; }

    /// <summary>
    /// The data read from NVR.
    /// </summary>
    public ReadOnlyMemory<byte> Data => Frame.CommandParameters;

    public static NvrGetValueResponse Create(DataFrame frame) => new NvrGetValueResponse(frame);
}
