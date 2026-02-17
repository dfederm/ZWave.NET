using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests;

[TestClass]
public class DataFrameTests
{
    [TestMethod]
    public void EmptyCommandParameters()
    {
        var frameData = new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        };

        var dataFrame = new DataFrame(frameData);
        Assert.AreEqual(DataFrameType.REQ, dataFrame.Type);
        Assert.AreEqual(CommandId.SerialApiStarted, dataFrame.CommandId);
        Assert.IsTrue(dataFrame.CommandParameters.IsEmpty);
        Assert.IsTrue(dataFrame.IsChecksumValid());
    }

    [TestMethod]
    public void InvalidChecksum()
    {
        var frameData = new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0x00 // Checksum
        };

        var dataFrame = new DataFrame(frameData);
        Assert.AreEqual(DataFrameType.REQ, dataFrame.Type);
        Assert.AreEqual(CommandId.SerialApiStarted, dataFrame.CommandId);
        Assert.IsTrue(dataFrame.CommandParameters.IsEmpty);
        Assert.IsFalse(dataFrame.IsChecksumValid());
    }

    [TestMethod]
    public void CommandParameters()
    {
        var frameData = new byte[]
        {
            FrameHeader.SOF,
            6, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0x01, // Command parameter 1
            0x02, // Command parameter 2
            0x03, // Command parameter 3
            0xF3 // Checksum
        };

        var dataFrame = new DataFrame(frameData);
        Assert.AreEqual(DataFrameType.REQ, dataFrame.Type);
        Assert.AreEqual(CommandId.SerialApiStarted, dataFrame.CommandId);
        Assert.AreEqual(3, dataFrame.CommandParameters.Length);
        Assert.IsTrue(dataFrame.CommandParameters.Span.SequenceEqual(new byte[] { 0x01, 0x02, 0x03 }));
        Assert.IsTrue(dataFrame.IsChecksumValid());
    }
}
