namespace ZWave.Serial.Commands;

public enum NvmStatus : byte
{
    /// <summary>
    /// The NVM operation completed successfully.
    /// </summary>
    Success = 0x00,

    /// <summary>
    /// The NVM operation encountered an error.
    /// </summary>
    Error = 0x01,

    /// <summary>
    /// The NVM address is out of range.
    /// </summary>
    AddressOutOfRange = 0x02,

    /// <summary>
    /// End of file has been reached.
    /// </summary>
    EndOfFile = 0xFF,
}

/// <summary>
/// Get NVM ID from external NVM.
/// </summary>
public readonly struct NvmGetIdRequest : ICommand<NvmGetIdRequest>
{
    public NvmGetIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.NvmGetId;

    public DataFrame Frame { get; }

    public static NvmGetIdRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new NvmGetIdRequest(frame);
    }

    public static NvmGetIdRequest Create(DataFrame frame) => new NvmGetIdRequest(frame);
}

public readonly struct NvmGetIdResponse : ICommand<NvmGetIdResponse>
{
    public NvmGetIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.NvmGetId;

    public DataFrame Frame { get; }

    /// <summary>
    /// The NVM manufacturer ID.
    /// </summary>
    public byte NvmManufacturerId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The memory type.
    /// </summary>
    public byte MemoryType => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The memory capacity.
    /// </summary>
    public byte MemoryCapacity => Frame.CommandParameters.Span[2];

    public static NvmGetIdResponse Create(DataFrame frame) => new NvmGetIdResponse(frame);
}
