using System.Buffers.Binary;

namespace ZWave.Serial.Commands;

/// <summary>
/// The protocol type running on the Z-Wave Module.
/// </summary>
public enum ProtocolType : byte
{
    /// <summary>
    /// Z-Wave Protocol.
    /// </summary>
    ZWave = 0x00,

    /// <summary>
    /// Z-Wave AV Protocol. This value SHOULD NOT be used by any Z-Wave API Module.
    /// </summary>
    ZWaveAV = 0x01,

    /// <summary>
    /// Z-Wave for IP Protocol. This value SHOULD NOT be used by any Z-Wave API Module.
    /// </summary>
    ZWaveForIP = 0x02,
}

/// <summary>
/// Request the Z-Wave Protocol version data.
/// </summary>
public readonly struct GetProtocolVersionRequest : ICommand<GetProtocolVersionRequest>
{
    public GetProtocolVersionRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetProtocolVersion;

    public DataFrame Frame { get; }

    public static GetProtocolVersionRequest Create()
    {
        DataFrame frame = DataFrame.Create(Type, CommandId);
        return new GetProtocolVersionRequest(frame);
    }

    public static GetProtocolVersionRequest Create(DataFrame frame) => new GetProtocolVersionRequest(frame);
}

/// <summary>
/// Response containing the Z-Wave Protocol version data.
/// </summary>
public readonly struct GetProtocolVersionResponse : ICommand<GetProtocolVersionResponse>
{
    public GetProtocolVersionResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetProtocolVersion;

    public DataFrame Frame { get; }

    /// <summary>
    /// The protocol type running on the Z-Wave Module.
    /// </summary>
    public ProtocolType ProtocolType => (ProtocolType)Frame.CommandParameters.Span[0];

    /// <summary>
    /// The major version number for the Z-Wave Protocol.
    /// </summary>
    public byte MajorVersion => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The minor version number for the Z-Wave Protocol.
    /// </summary>
    public byte MinorVersion => Frame.CommandParameters.Span[2];

    /// <summary>
    /// The revision version number for the Z-Wave Protocol.
    /// </summary>
    public byte RevisionVersion => Frame.CommandParameters.Span[3];

    /// <summary>
    /// The application framework build number. The value 0 indicates this value is not available.
    /// </summary>
    public ushort ApplicationFrameworkBuildNumber
        => BinaryPrimitives.ReadUInt16BigEndian(Frame.CommandParameters.Span.Slice(4, 2));

    /// <summary>
    /// The git commit hash for the Z-Wave Protocol running in the Z-Wave API Module.
    /// May be all zeros if this information is not available.
    /// </summary>
    public ReadOnlySpan<byte> GitCommitHash
        // The git commit hash may be omitted
        => Frame.CommandParameters.Span.Length < 22 ? [] : Frame.CommandParameters.Span.Slice(6, 16);

    public static GetProtocolVersionResponse Create(DataFrame frame) => new GetProtocolVersionResponse(frame);
}
