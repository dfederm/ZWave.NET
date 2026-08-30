namespace ZWave.CommandClasses;

/// <summary>
/// CRC-16 (CCIT-FALSE) computation per the Z-Wave CRC-16 Encapsulation Command Class
/// (spec §3.1.2): initial value 0x1D0F, polynomial 0x1021, non-reflected (MSB-first), no final XOR.
/// </summary>
internal static class Crc16
{
    private const ushort Polynomial = 0x1021;
    private const ushort InitialValue = 0x1D0F;

    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = InitialValue;
        foreach (byte value in data)
        {
            crc ^= (ushort)(value << 8);
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ Polynomial)
                    : (ushort)(crc << 1);
            }
        }

        return crc;
    }
}
