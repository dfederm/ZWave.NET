using System.Runtime.InteropServices;
using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests;

[TestClass]
public class FrameTests
{
    private static readonly ReadOnlyMemory<byte> ValidDataFrameData = new byte[]
    {
        FrameHeader.SOF,
        3, // Length
        (byte)DataFrameType.REQ,
        (byte)CommandId.SerialApiStarted,
        0xF6 // Checksum
    };

    [TestMethod]
    public void ConstructorEmptyData()
    {
        Assert.Throws<ArgumentException>(() => new Frame(Array.Empty<byte>()));
    }

    [TestMethod]
    [DataRow(FrameHeader.ACK, FrameType.ACK)]
    [DataRow(FrameHeader.NAK, FrameType.NAK)]
    [DataRow(FrameHeader.CAN, FrameType.CAN)]
    public void ConstructorSingleByteFrames(byte frameHeader, FrameType expectedFrameType)
    {
        var frame = new Frame(new[] { frameHeader });
        Assert.AreEqual(expectedFrameType, frame.Type);
    }

    [TestMethod]
    [DataRow(FrameHeader.ACK)]
    [DataRow(FrameHeader.NAK)]
    [DataRow(FrameHeader.CAN)]
    public void ConstructorSingleByteFramesWithExtraData(byte frameHeader)
    {
        Assert.Throws<ArgumentException>(() => new Frame(new byte[] { frameHeader, 0x01 }));
    }

    // Single byte frames use a singleton array to avoid holding onto many small arrays
    [TestMethod]
    [DataRow(true, new[] { FrameHeader.ACK })]
    [DataRow(true, new[] { FrameHeader.NAK })]
    [DataRow(true, new[] { FrameHeader.CAN })]
    [DataRow(
        false,
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        })]
    public void ConstructorDataSingletons(bool expectSingleton, byte[] inputFrameData)
    {
        var frame = new Frame(inputFrameData);

        // Get the actual underlying array
        Assert.IsTrue(MemoryMarshal.TryGetArray(frame.Data, out ArraySegment<byte> arraySegment));
        byte[]? actualFrameData = arraySegment.Array;

        Assert.AreEqual(!expectSingleton, ReferenceEquals(inputFrameData, actualFrameData));
    }

    [TestMethod]
    public void ConstructorDataFrame()
    {
        var frame = new Frame(ValidDataFrameData);
        Assert.AreEqual(FrameType.Data, frame.Type);
    }

    [TestMethod]
    public void ConstructorDataFrameWithInvalidLength()
    {
        byte[] frameData = ValidDataFrameData.ToArray();
        frameData[1]--; // Length

        Assert.Throws<ArgumentException>(() => new Frame(frameData));
    }

    [TestMethod]
    public void ConstructorUnknownHeader()
    {
        byte[] frameData = new byte[]
        {
            0xDE,
            0xAD,
            0xBE,
            0xEF,
        };

        Assert.Throws<ArgumentException>(() => new Frame(frameData));
    }

    [TestMethod]
    public void ToDataFrameForDataFrame()
    {
        byte[] frameData = new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        };
        var frame = new Frame(frameData);

        var dataFrame = frame.ToDataFrame();
        Assert.AreNotEqual(default, dataFrame);
    }

    [TestMethod]
    public void ToDataFrameForNonDataFrame()
    {
        Assert.Throws<InvalidOperationException>(() => Frame.ACK.ToDataFrame());
    }

    [TestMethod]
    [DataRow(true, new[] { FrameHeader.ACK }, new[] { FrameHeader.ACK })]
    [DataRow(true, new[] { FrameHeader.NAK }, new[] { FrameHeader.NAK })]
    [DataRow(true, new[] { FrameHeader.CAN }, new[] { FrameHeader.CAN })]
    [DataRow(false, new[] { FrameHeader.ACK }, new[] { FrameHeader.NAK })]
    [DataRow(false, new[] { FrameHeader.ACK }, new[] { FrameHeader.CAN })]
    [DataRow(false, new[] { FrameHeader.NAK }, new[] { FrameHeader.CAN })]
    [DataRow(
        true,
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        },
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        })]
    [DataRow(
        false,
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        },
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SoftReset,
            0xF6 // Checksum
        })]
    public void Equality(bool expectedAreEqual, byte[] frameData1, byte[] frameData2)
    {
        var frame1 = new Frame(frameData1);
        var frame2 = new Frame(frameData2);
        Assert.AreEqual(expectedAreEqual, frame1 == frame2);
    }

    [TestMethod]
    [DataRow(new[] { FrameHeader.ACK })]
    [DataRow(new[] { FrameHeader.NAK })]
    [DataRow(new[] { FrameHeader.CAN })]
    [DataRow(
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        })]
    public void GetHashCodeConsistency(byte[] frameData)
    {
        var frame1 = new Frame(frameData);

        var frameDataCopy = (byte[])frameData.Clone();
        var frame2 = new Frame(frameDataCopy);

        Assert.AreEqual(frame1.GetHashCode(), frame2.GetHashCode());
    }

    [TestMethod]
    public void GetHashCodeUniqueness()
    {
        var hashCodes = new HashSet<int>();
        int hashCodesAdded = 0;

        void AddHashCode(Frame frame)
        {
            hashCodes.Add(frame.GetHashCode());
            hashCodesAdded++;
        }

        AddHashCode(Frame.ACK);
        AddHashCode(Frame.NAK);
        AddHashCode(Frame.CAN);
        AddHashCode(new Frame(new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        }));
        AddHashCode(new Frame(new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SoftReset,
            0xF6 // Checksum
        }));

        Assert.HasCount(hashCodesAdded, hashCodes);
    }
}