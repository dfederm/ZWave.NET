namespace ZWave.CommandClasses.Tests;

public partial class Crc16EncapsulationCommandClassTests
{
    [TestMethod]
    public void Crc16_Compute_Empty_ReturnsInitialValue()
    {
        Assert.AreEqual((ushort)0x1D0F, Crc16.Compute(ReadOnlySpan<byte>.Empty));
    }

    [TestMethod]
    public void Crc16_Compute_SingleByteA()
    {
        Assert.AreEqual((ushort)0x9479, Crc16.Compute([0x41]));
    }

    [TestMethod]
    public void Crc16_Compute_CheckSequence()
    {
        // "123456789"
        byte[] data = [0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39];
        Assert.AreEqual((ushort)0xE5CC, Crc16.Compute(data));
    }

    [TestMethod]
    public void Crc16_Compute_SpecTable1Vector()
    {
        // Spec SDS13783 §3.1.2 Table 1: CRC-16 over the bytes
        // [0x56, 0x01, 0x20, 0x02] (CC id + cmd id + Basic CC + Basic Get)
        // equals 0x4D26 (MSB 0x4D, LSB 0x26).
        Assert.AreEqual((ushort)0x4D26, Crc16.Compute([0x56, 0x01, 0x20, 0x02]));
    }
}
