using System.Buffers.Binary;

namespace ZWave;

internal static class BinaryExtensions
{
    public static sbyte ToInt8(this byte b) => unchecked((sbyte)b);

    public static short ToInt16BE(this ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadInt16BigEndian(bytes);

    public static ushort ToUInt16BE(this ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt16BigEndian(bytes);

    public static void WriteBytesBE(this short value, Span<byte> destination) => BinaryPrimitives.WriteInt16BigEndian(destination, value);

    public static void WriteBytesBE(this ushort value, Span<byte> destination) => BinaryPrimitives.WriteUInt16BigEndian(destination, value);

    public static uint ToUInt32BE(this ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt32BigEndian(bytes);

    public static void WriteBytesBE(this uint value, Span<byte> destination) => BinaryPrimitives.WriteUInt32BigEndian(destination, value);

    public static int ToInt32BE(this ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadInt32BigEndian(bytes);

    public static void WriteBytesBE(this int value, Span<byte> destination) => BinaryPrimitives.WriteInt32BigEndian(destination, value);

    /// <summary>
    /// Lookup table for 10^n where n is a Z-Wave precision value (0–7).
    /// </summary>
    public static ReadOnlySpan<double> PowersOfTen => [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000];

    /// <summary>
    /// Read a signed big-endian integer from a span of 1, 2, or 4 bytes.
    /// </summary>
    public static int ReadSignedVariableSizeBE(this ReadOnlySpan<byte> bytes)
    {
        switch (bytes.Length)
        {
            case 1:
            {
                return unchecked((sbyte)bytes[0]);
            }
            case 2:
            {
                return BinaryPrimitives.ReadInt16BigEndian(bytes);
            }
            case 4:
            {
                return BinaryPrimitives.ReadInt32BigEndian(bytes);
            }
            default:
            {
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    $"Invalid value size {bytes.Length}. Expected 1, 2, or 4.");
                return default;
            }
        }
    }

    /// <summary>
    /// Get the minimum number of bytes (1, 2, or 4) needed to represent a signed integer.
    /// </summary>
    public static int GetSignedVariableSize(this int value)
        => value switch
        {
            >= sbyte.MinValue and <= sbyte.MaxValue => 1,
            >= short.MinValue and <= short.MaxValue => 2,
            _ => 4,
        };

    /// <summary>
    /// Write a signed big-endian integer using 1, 2, or 4 bytes based on the destination length.
    /// </summary>
    public static void WriteSignedVariableSizeBE(this int value, Span<byte> destination)
    {
        switch (destination.Length)
        {
            case 1:
            {
                destination[0] = unchecked((byte)(sbyte)value);
                break;
            }
            case 2:
            {
                BinaryPrimitives.WriteInt16BigEndian(destination, (short)value);
                break;
            }
            case 4:
            {
                BinaryPrimitives.WriteInt32BigEndian(destination, value);
                break;
            }
            default:
            {
                throw new ArgumentException($"Invalid destination size {destination.Length}. Expected 1, 2, or 4.", nameof(destination));
            }
        }
    }
}
