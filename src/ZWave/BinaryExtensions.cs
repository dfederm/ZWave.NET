using System.Buffers.Binary;

namespace ZWave;

internal static class BinaryExtensions
{
    public static sbyte ToInt8(this byte b) => unchecked((sbyte)b);

    public static ushort ToUInt16BE(this ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt16BigEndian(bytes);

    public static void WriteBytesBE(this ushort value, Span<byte> destination) => BinaryPrimitives.WriteUInt16BigEndian(destination, value);

    public static uint ToUInt32BE(this ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt32BigEndian(bytes);

    public static void WriteBytesBE(this uint value, Span<byte> destination) => BinaryPrimitives.WriteUInt32BigEndian(destination, value);

    public static int ToInt32BE(this ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadInt32BigEndian(bytes);
}
