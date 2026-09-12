namespace ZWave.CommandClasses.Tests;

[TestClass]
public class S0CryptoTests
{
    private static byte[] Hex(string value) => Convert.FromHexString(value);
    private static string Hex(byte[] value) => Convert.ToHexString(value).ToLowerInvariant();

    [TestMethod]
    public void EcbEncrypt_Fips197_C1_MatchesKnownVector()
    {
        byte[] key = Hex("000102030405060708090a0b0c0d0e0f");
        byte[] plaintext = Hex("00112233445566778899aabbccddeeff");
        byte[] ciphertext = S0Crypto.Aes128EcbEncrypt(plaintext, key);

        Assert.AreEqual("69c4e0d86a7b0430d8cdb78070b4c55a", Hex(ciphertext));
    }

    [TestMethod]
    public void EcbEncrypt_WrongBlockSize_Throws()
    {
        byte[] key = Hex("000102030405060708090a0b0c0d0e0f");

        Assert.Throws<ZWaveException>(() => S0Crypto.Aes128EcbEncrypt(new byte[15], key));
        Assert.Throws<ZWaveException>(() => S0Crypto.Aes128EcbEncrypt(new byte[17], key));
    }

    [TestMethod]
    public void EncryptOfb_SingleBlock_MatchesKnownVector()
    {
        byte[] key = Hex("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] iv = Hex("000102030405060708090a0b0c0d0e0f");
        byte[] plaintext = Hex("6bc1bee22e409f96e93d7e117393172a");

        byte[] ciphertext = S0Crypto.EncryptOfb(plaintext, key, iv);

        Assert.AreEqual("3b3fd92eb72dad20333449f8e83cfb4a", Hex(ciphertext));
    }

    [TestMethod]
    public void EncryptOfb_MultiBlock_MatchesReferenceAndRoundTrips()
    {
        byte[] key = Hex("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] iv = Hex("000102030405060708090a0b0c0d0e0f");
        byte[] plaintext = Hex(
            "6bc1bee22e409f96e93d7e117393172a" +
            "ae2d8a571e03ac9c9eb76fac45af8e51" +
            "30c81c46a35ce411e5fbc1191a0a52ef" +
            "f69f2445df4f9b17ad2b417be66c3710");

        byte[] ciphertext = S0Crypto.EncryptOfb(plaintext, key, iv);

        Assert.AreEqual(
            "3b3fd92eb72dad20333449f8e83cfb4a" +
            "7789508d16918f03f53c52dac54ed825" +
            "9740051e9c5fecf64344f7a82260edcc" +
            "304c6528f659c77866a510d9c1d6ae5e",
            Hex(ciphertext));
        Assert.AreEqual(Hex(plaintext), Hex(S0Crypto.DecryptOfb(ciphertext, key, iv)));
    }

    [TestMethod]
    public void EncryptOfb_NonBlockAligned_LengthPreservedAndRoundTrips()
    {
        byte[] key = Hex("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] iv = Hex("000102030405060708090a0b0c0d0e0f");
        byte[] plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

        byte[] ciphertext = S0Crypto.EncryptOfb(plaintext, key, iv);

        Assert.HasCount(plaintext.Length, ciphertext);
        Assert.AreNotEqual(Hex(plaintext), Hex(ciphertext));
        Assert.AreEqual(Hex(plaintext), Hex(S0Crypto.DecryptOfb(ciphertext, key, iv)));
    }

    [TestMethod]
    public void CbcEncrypt_BlockAligned_MatchesReference()
    {
        byte[] key = Hex("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] iv = Hex("000102030405060708090a0b0c0d0e0f");
        byte[] plaintext = Hex(
            "6bc1bee22e409f96e93d7e117393172a" +
            "ae2d8a571e03ac9c9eb76fac45af8e51" +
            "30c81c46a35ce411e5fbc1191a0a52ef" +
            "f69f2445df4f9b17ad2b417be66c3710");

        byte[] ciphertext = S0Crypto.Aes128CbcEncrypt(plaintext, key, iv);

        Assert.AreEqual(
            "7649abac8119b246cee98e9b12e9197d" +
            "5086cb9b507219ee95db113a917678b2" +
            "73bed6b8e3c1743b7116e69e22229516" +
            "3ff1caa1681fac09120eca307586e1a7",
            Hex(ciphertext));
    }

    [TestMethod]
    public void CbcEncrypt_ZeroPadsToBlockBoundary()
    {
        byte[] key = Hex("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] iv = Hex("000102030405060708090a0b0c0d0e0f");
        byte[] plaintext = new byte[23];
        plaintext[0] = 0xAB;
        plaintext[22] = 0xCD;

        byte[] ciphertext = S0Crypto.Aes128CbcEncrypt(plaintext, key, iv);

        Assert.HasCount(32, ciphertext);
    }

    [TestMethod]
    public void ComputeMac_ZeroPaddedAuthData_MatchesReference()
    {
        byte[] authKey = Hex("01010101010101010101010101010101");
        byte[] authData = Hex("000102030405060700010203040506078101020daabbcc");

        ReadOnlySpan<byte> mac = S0Crypto.ComputeMac(authData, authKey);

        Assert.AreEqual("143f7d8906c5246b", Convert.ToHexString(mac).ToLowerInvariant());
    }

    [TestMethod]
    public void DeriveAuthKey_MatchesReference()
    {
        byte[] networkKey = Hex("2b7e151628aed2a6abf7158809cf4f3c");

        Assert.AreEqual("c985043655121fdf1f87fbce7c7ca451", Hex(S0Crypto.DeriveAuthKey(networkKey)));
    }

    [TestMethod]
    public void DeriveEncryptionKey_MatchesReference()
    {
        byte[] networkKey = Hex("2b7e151628aed2a6abf7158809cf4f3c");

        Assert.AreEqual("b53da9ee4283df0d733138f64594d676", Hex(S0Crypto.DeriveEncryptionKey(networkKey)));
    }

    [TestMethod]
    public void DerivedKeys_AreDifferent()
    {
        byte[] networkKey = Hex("2b7e151628aed2a6abf7158809cf4f3c");

        Assert.AreNotEqual(Hex(S0Crypto.DeriveAuthKey(networkKey)), Hex(S0Crypto.DeriveEncryptionKey(networkKey)));
    }
}
