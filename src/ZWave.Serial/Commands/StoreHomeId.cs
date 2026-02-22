namespace ZWave.Serial.Commands;

/// <summary>
/// Restore HomeID and NodeID information from a backup.
/// </summary>
public readonly struct StoreHomeIdRequest : ICommand<StoreHomeIdRequest>
{
    public StoreHomeIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.StoreHomeId;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to store the Home ID and Node ID.
    /// </summary>
    /// <param name="homeId">The Home ID to store (big-endian).</param>
    /// <param name="nodeId">The Node ID to store.</param>
    public static StoreHomeIdRequest Create(uint homeId, ushort nodeId)
    {
        Span<byte> commandParameters = stackalloc byte[5];
        homeId.WriteBytesBE(commandParameters);
        commandParameters[4] = (byte)nodeId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new StoreHomeIdRequest(frame);
    }

    public static StoreHomeIdRequest Create(DataFrame frame) => new StoreHomeIdRequest(frame);
}

public readonly struct StoreHomeIdResponse : ICommand<StoreHomeIdResponse>
{
    public StoreHomeIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.StoreHomeId;

    public DataFrame Frame { get; }

    /// <summary>
    /// The status of the store operation.
    /// </summary>
    public byte Status => Frame.CommandParameters.Span[0];

    public static StoreHomeIdResponse Create(DataFrame frame) => new StoreHomeIdResponse(frame);
}
