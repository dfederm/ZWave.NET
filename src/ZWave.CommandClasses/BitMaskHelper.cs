using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ZWave.CommandClasses;

/// <summary>
/// Helpers for parsing Z-Wave bitmask fields into collections of enum values.
/// </summary>
internal static class BitMaskHelper
{
    /// <summary>
    /// Parse a variable-length bitmask into a set of enum values.
    /// </summary>
    /// <typeparam name="TEnum">A byte-backed enum type.</typeparam>
    /// <param name="bitMask">The bitmask bytes to parse.</param>
    /// <param name="offset">Value added to each bit position to produce the enum value. Default 0.</param>
    /// <param name="startBit">First bit position to consider; earlier bits are skipped. Default 0.</param>
    /// <returns>A set containing the enum values corresponding to set bits.</returns>
    internal static HashSet<TEnum> ParseBitMask<TEnum>(
        ReadOnlySpan<byte> bitMask,
        int offset = 0,
        int startBit = 0)
        where TEnum : struct, Enum
    {
        Debug.Assert(Unsafe.SizeOf<TEnum>() == sizeof(byte), "ParseBitMask only supports byte-backed enums.");

        HashSet<TEnum> result = [];
        for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
        {
            for (int bitNum = 0; bitNum < 8; bitNum++)
            {
                if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                {
                    int bitPosition = (byteNum << 3) + bitNum;
                    if (bitPosition >= startBit)
                    {
                        byte value = (byte)(bitPosition + offset);
                        result.Add(Unsafe.BitCast<byte, TEnum>(value));
                    }
                }
            }
        }

        return result;
    }
}
