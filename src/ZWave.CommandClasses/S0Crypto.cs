using System.Security.Cryptography;

namespace ZWave.CommandClasses;

/// <summary>
/// AES-128 primitives for the Security 0 Command Class (spec SDS13783 §3.5): key derivation (ECB),
/// payload encryption (OFB), and the message authentication code (CBC).
/// </summary>
/// <remarks>
/// The S0 cipher suite is defined in the Z-Wave spec and in Silicon Labs' "Z-Wave AES-128"
/// primitives. Unlike PKCS#7 padding, S0 uses zero-byte padding: input is padded with 0x00
/// (not with a length count) to reach a 16-byte boundary, and for OFB the output length equals
/// the input length. The ECB and CBC primitives use the span-based one-shot Aes methods; OFB has
/// no one-shot equivalent on this runtime (CipherMode.OFB is rejected), so it is built on ECB.
/// </remarks>
internal static class S0Crypto
{
    private const int BlockSize = 16;
    private const byte AuthKeyFill = 0x55;
    private const byte EncryptionKeyFill = 0xAA;

    private static readonly byte[] ZeroIv = new byte[BlockSize];
    private static readonly byte[] AuthKeyBlock = Fill16(AuthKeyFill);
    private static readonly byte[] EncryptionKeyBlock = Fill16(EncryptionKeyFill);

    /// <summary>
    /// Derives the S0 authentication key: AES-128-ECB of a 16-byte 0x55 block under the network key.
    /// </summary>
    public static byte[] DeriveAuthKey(byte[] networkKey)
        => Aes128EcbEncrypt(AuthKeyBlock, networkKey);

    /// <summary>
    /// Derives the S0 encryption key: AES-128-ECB of a 16-byte 0xAA block under the network key.
    /// </summary>
    public static byte[] DeriveEncryptionKey(byte[] networkKey)
        => Aes128EcbEncrypt(EncryptionKeyBlock, networkKey);

    /// <summary>
    /// Encrypts a single 16-byte block with AES-128-ECB.
    /// </summary>
    /// <param name="plaintext">Exactly one 16-byte block.</param>
    /// <param name="key">A 16-byte key.</param>
    public static byte[] Aes128EcbEncrypt(ReadOnlySpan<byte> plaintext, byte[] key)
    {
        if (plaintext.Length != BlockSize)
        {
            ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, "AES-128-ECB input must be exactly one 16-byte block");
        }

        using Aes aes = Aes.Create();
        aes.Key = key;
        return aes.EncryptEcb(plaintext, PaddingMode.None);
    }

    /// <summary>
    /// Encrypts arbitrary-length data with AES-128-OFB. The output length equals the input length.
    /// </summary>
    public static byte[] EncryptOfb(ReadOnlySpan<byte> plaintext, byte[] key, byte[] iv)
        => Ofb(plaintext, key, iv);

    /// <summary>
    /// Decrypts arbitrary-length data with AES-128-OFB. The output length equals the input length.
    /// </summary>
    public static byte[] DecryptOfb(ReadOnlySpan<byte> ciphertext, byte[] key, byte[] iv)
        => Ofb(ciphertext, key, iv);

    private static byte[] Ofb(ReadOnlySpan<byte> data, byte[] key, byte[] iv)
    {
        if (data.IsEmpty)
        {
            return [];
        }

        // OFB is self-inverse (encryption and decryption are the same operation), and this runtime
        // rejects CipherMode.OFB with no one-shot alternative, so it is built on the ECB primitive.
        byte[] output = new byte[data.Length];

        using Aes aes = Aes.Create();
        aes.Key = key;

        // A single 32-byte buffer holds the two OFB feedback/keystream blocks, ping-ponged between
        // the ECB input and output, so the loop allocates no per-block arrays. The IV seeds the first
        // block; it is copied in because the feedback block is overwritten as the chain advances.
        byte[] scratch = new byte[BlockSize * 2];
        iv.CopyTo(scratch);
        Span<byte> feedback = scratch.AsSpan(0, BlockSize);
        Span<byte> keystream = scratch.AsSpan(BlockSize, BlockSize);
        int offset = 0;
        while (offset < data.Length)
        {
            _ = aes.EncryptEcb(feedback, keystream, PaddingMode.None);
            int blockLength = Math.Min(BlockSize, data.Length - offset);
            for (int i = 0; i < blockLength; i++)
            {
                output[offset + i] = (byte)(data[offset + i] ^ keystream[i]);
            }

            Span<byte> temp = feedback;
            feedback = keystream;
            keystream = temp;
            offset += blockLength;
        }

        return output;
    }

    /// <summary>
    /// Encrypts data with AES-128-CBC. The input is zero-padded to a 16-byte multiple and the full
    /// (untrimmed) output is returned, matching the S0 MAC construction.
    /// </summary>
    public static byte[] Aes128CbcEncrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] iv)
    {
        byte[] padded = ZeroPadToBlock(plaintext);
        using Aes aes = Aes.Create();
        aes.Key = key;
        return aes.EncryptCbc(padded, iv, PaddingMode.None);
    }

    /// <summary>
    /// Computes the S0 MAC: AES-128-CBC of the auth data under the auth key (zero IV), taking the
    /// first 8 bytes of the final 16-byte block.
    /// </summary>
    public static ReadOnlySpan<byte> ComputeMac(ReadOnlySpan<byte> authData, byte[] authKey)
    {
        byte[] cbc = Aes128CbcEncrypt(authData, authKey, ZeroIv);
        return cbc.AsSpan()[^BlockSize..^(BlockSize / 2)];
    }

    private static byte[] Fill16(byte value)
    {
        byte[] block = new byte[BlockSize];
        block.AsSpan().Fill(value);
        return block;
    }

    private static byte[] ZeroPadToBlock(ReadOnlySpan<byte> data)
    {
        int remainder = data.Length % BlockSize;
        int paddedLength = remainder == 0 ? data.Length : data.Length + (BlockSize - remainder);
        byte[] padded = new byte[paddedLength];
        data.CopyTo(padded);
        return padded;
    }
}
