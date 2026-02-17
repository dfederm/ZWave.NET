namespace ZWave.Serial;

internal readonly struct Frame : IEquatable<Frame>
{
    public Frame(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new ArgumentException("Frame data must not be empty", nameof(data));
        }

        // Reuse the singletons for single-byte frames to allow the provided byte array to be GC'd.
        switch (data.Span[0])
        {
            case FrameHeader.ACK:
            {
                this = ACK;
                break;
            }
            case FrameHeader.NAK:
            {
                this = NAK;
                break;
            }
            case FrameHeader.CAN:
            {
                this = CAN;
                break;
            }
            case FrameHeader.SOF:
            {
                Data = data;
                Type = FrameType.Data;
                break;
            }
            default:
            {
                throw new ArgumentException($"Frame data had unknown frame header: {data.Span[0]}", nameof(data));
            }
        }

        // Sanity checks for a complete frame. Note that the frame way still be invalid, eg. data frame with bad checksum.
        if (Type == FrameType.Data)
        {
            // The length doesn't include the SOF or checksum, so add 2
            if (data.Span[1] + 2 != data.Length)
            {
                throw new ArgumentException($"The data frame's length field had invalid value {data.Span[1]} for frame data length {data.Length}", nameof(data));
            }
        }
        else if (data.Length > 1)
        {
            throw new ArgumentException($"Frame data must be exactly 1 byte for {Type} frame", nameof(data));
        }
    }

    // Used for well-known single-byte frame types
    private Frame(byte frameHeader, FrameType frameType)
    {
        Data = new[] { frameHeader };
        Type = frameType;
    }

    public static Frame ACK { get; } = new Frame(FrameHeader.ACK, FrameType.ACK);

    public static Frame NAK { get; } = new Frame(FrameHeader.NAK, FrameType.NAK);

    public static Frame CAN { get; } = new Frame(FrameHeader.CAN, FrameType.CAN);

    public FrameType Type { get; }

    public ReadOnlyMemory<byte> Data { get; }

    public DataFrame ToDataFrame()
        => Type == FrameType.Data
            ? new DataFrame(Data)
            : throw new InvalidOperationException($"{Type} frames are not data frames");

    public override bool Equals(object? obj) => obj is Frame other && this.Equals(other);

    public bool Equals(Frame other)
        => Data.Span.SequenceEqual(other.Data.Span);

    public static bool operator ==(Frame lhs, Frame rhs) => lhs.Equals(rhs);

    public static bool operator !=(Frame lhs, Frame rhs) => !(lhs == rhs);

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.AddBytes(Data.Span);
        return hash.ToHashCode();
    }

    public override string ToString() => Type switch
    {
        FrameType.ACK => nameof(FrameType.ACK),
        FrameType.NAK => nameof(FrameType.NAK),
        FrameType.CAN => nameof(FrameType.CAN),
        FrameType.Data => $"{nameof(FrameType.Data)} [Size={Data.Length}]",
        _ => throw new NotImplementedException(),
    };
}
