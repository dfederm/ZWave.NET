using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public partial class NodeNamingAndLocationCommandClassTests
{
    [TestMethod]
    public void DecodeText_Ascii()
    {
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F]; // "Hello"
        string result = NodeNamingAndLocationCommandClass.DecodeText(CharPresentation.Ascii, data);
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void DecodeText_OemExtendedAscii_DecodesAsAscii()
    {
        // OEM Extended ASCII is decoded as ASCII; bytes > 127 become '?'
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x81];
        string result = NodeNamingAndLocationCommandClass.DecodeText(CharPresentation.OemExtendedAscii, data);
        Assert.AreEqual("Hell?", result);
    }

    [TestMethod]
    public void DecodeText_Utf16()
    {
        byte[] data = [0x00, 0x48, 0x00, 0x69]; // "Hi" in UTF-16 BE
        string result = NodeNamingAndLocationCommandClass.DecodeText(CharPresentation.Utf16, data);
        Assert.AreEqual("Hi", result);
    }

    [TestMethod]
    public void DecodeText_EmptyData()
    {
        string result = NodeNamingAndLocationCommandClass.DecodeText(CharPresentation.Ascii, ReadOnlySpan<byte>.Empty);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void DecodeText_UnknownPresentation_FallsBackToAscii()
    {
        byte[] data = [0x41, 0x42]; // "AB"
        string result = NodeNamingAndLocationCommandClass.DecodeText((CharPresentation)0x05, data);
        Assert.AreEqual("AB", result);
    }

    [TestMethod]
    public void GetCharPresentation_AsciiString_ReturnsAscii()
    {
        CharPresentation result = NodeNamingAndLocationCommandClass.GetCharPresentation("Hello");
        Assert.AreEqual(CharPresentation.Ascii, result);
    }

    [TestMethod]
    public void GetCharPresentation_NonAsciiString_ReturnsUtf16()
    {
        CharPresentation result = NodeNamingAndLocationCommandClass.GetCharPresentation("Caf\u00E9");
        Assert.AreEqual(CharPresentation.Utf16, result);
    }

    [TestMethod]
    public void GetCharPresentation_EmptyString_ReturnsAscii()
    {
        CharPresentation result = NodeNamingAndLocationCommandClass.GetCharPresentation(string.Empty);
        Assert.AreEqual(CharPresentation.Ascii, result);
    }

    [TestMethod]
    public void EncodeText_Ascii()
    {
        Span<byte> buffer = stackalloc byte[16];
        int written = NodeNamingAndLocationCommandClass.EncodeText("Hello", CharPresentation.Ascii, buffer);
        Assert.AreEqual(5, written);
        Assert.AreEqual((byte)'H', buffer[0]);
        Assert.AreEqual((byte)'e', buffer[1]);
        Assert.AreEqual((byte)'l', buffer[2]);
        Assert.AreEqual((byte)'l', buffer[3]);
        Assert.AreEqual((byte)'o', buffer[4]);
    }

    [TestMethod]
    public void EncodeText_Utf16()
    {
        Span<byte> buffer = stackalloc byte[16];
        int written = NodeNamingAndLocationCommandClass.EncodeText("Hi", CharPresentation.Utf16, buffer);
        Assert.AreEqual(4, written);
        // "Hi" in UTF-16 BE: 0x00 0x48 0x00 0x69
        Assert.AreEqual(0x00, buffer[0]);
        Assert.AreEqual(0x48, buffer[1]);
        Assert.AreEqual(0x00, buffer[2]);
        Assert.AreEqual(0x69, buffer[3]);
    }

    [TestMethod]
    public void EncodeText_Ascii_TooLong_Throws()
    {
        byte[] buffer = new byte[16];
        Assert.ThrowsExactly<ArgumentException>(
            () => NodeNamingAndLocationCommandClass.EncodeText(
                "This is a very long name exceeding 16 bytes", CharPresentation.Ascii, buffer));
    }

    [TestMethod]
    public void EncodeText_Utf16_TooLong_Throws()
    {
        // 9 non-ASCII characters = 18 bytes in UTF-16, exceeds 16 bytes
        byte[] buffer = new byte[16];
        Assert.ThrowsExactly<ArgumentException>(
            () => NodeNamingAndLocationCommandClass.EncodeText(
                "\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8", CharPresentation.Utf16, buffer));
    }

    [TestMethod]
    public void EncodeText_EmptyString()
    {
        Span<byte> buffer = stackalloc byte[16];
        int written = NodeNamingAndLocationCommandClass.EncodeText(string.Empty, CharPresentation.Ascii, buffer);
        Assert.AreEqual(0, written);
    }

    [TestMethod]
    public void EncodeText_Ascii_Exactly16Bytes()
    {
        Span<byte> buffer = stackalloc byte[16];
        int written = NodeNamingAndLocationCommandClass.EncodeText("1234567890ABCDEF", CharPresentation.Ascii, buffer);
        Assert.AreEqual(16, written);
    }

    [TestMethod]
    public void EncodeText_Utf16_Exactly8Characters()
    {
        Span<byte> buffer = stackalloc byte[16];
        int written = NodeNamingAndLocationCommandClass.EncodeText(
            "\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7", CharPresentation.Utf16, buffer);
        Assert.AreEqual(16, written);
    }
}
