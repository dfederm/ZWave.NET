using System.Buffers;
using Microsoft.Extensions.Logging.Abstractions;
using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial;

[TestClass]
public class FrameParserTests
{
    [TestMethod]
    public void ParseEmptySequence()
    {
        var sequence = ReadOnlySequence<byte>.Empty;

        var success = FrameParser.TryParseData(NullLogger.Instance, ref sequence, out Frame frame);
        Assert.IsFalse(success);
        Assert.IsTrue(sequence.IsEmpty);
        Assert.AreEqual(default, frame);
    }

    [TestMethod]
    public void ParseInvalidDataOnly()
    {
        var bytes = new byte[]
        {
            0xDE,
            0xAD,
            0xBE,
            0xEF,
        };
        var sequence = new ReadOnlySequence<byte>(bytes);

        var success = FrameParser.TryParseData(NullLogger.Instance, ref sequence, out Frame frame);
        Assert.IsFalse(success);
        Assert.IsTrue(sequence.IsEmpty);
        Assert.AreEqual(default, frame);
    }

    [TestMethod]
    [DataRow(FrameHeader.ACK)]
    [DataRow(FrameHeader.NAK)]
    [DataRow(FrameHeader.CAN)]
    public void ParseSingleByteFrames(byte frameHeader)
    {
        var bytes = new[] { frameHeader };
        var sequence = new ReadOnlySequence<byte>(bytes);

        var success = FrameParser.TryParseData(NullLogger.Instance, ref sequence, out Frame frame);
        Assert.IsTrue(success);
        Assert.IsTrue(sequence.IsEmpty);
        Assert.AreEqual(new Frame(bytes), frame);
    }

    [TestMethod]
    public void ParseSingleByteFrameWithLeadingInvalidData()
    {
        var bytes = new byte[]
        {
            0xDE,
            0xAD,
            0xBE,
            0xEF,
            FrameHeader.ACK,
        };
        var sequence = new ReadOnlySequence<byte>(bytes);

        var success = FrameParser.TryParseData(NullLogger.Instance, ref sequence, out Frame frame);
        Assert.IsTrue(success);
        Assert.IsTrue(sequence.IsEmpty);
        Assert.AreEqual(Frame.ACK, frame);
    }

    [TestMethod]
    public void ParseSingleByteFrameWithTrailingData()
    {
        var bytes = new[]
        {
            FrameHeader.ACK,
            FrameHeader.ACK,
            FrameHeader.ACK,
        };
        var sequence = new ReadOnlySequence<byte>(bytes);

        var success = FrameParser.TryParseData(NullLogger.Instance, ref sequence, out Frame frame);
        Assert.IsTrue(success);
        Assert.AreEqual(bytes.Length - 1, sequence.Length);
        Assert.AreEqual(Frame.ACK, frame);
    }

    [TestMethod]
    public void ParseDataFrame()
    {
        var bytes = new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
            0xF6 // Checksum
        };
        var sequence = new ReadOnlySequence<byte>(bytes);

        var success = FrameParser.TryParseData(NullLogger.Instance, ref sequence, out Frame frame);
        Assert.IsTrue(success);
        Assert.IsTrue(sequence.IsEmpty);
        Assert.AreEqual(FrameType.Data, frame.Type);
    }

    [TestMethod]
    [DataRow(
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
            (byte)CommandId.SerialApiStarted,
        })]
    [DataRow(
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            (byte)DataFrameType.REQ,
        })]
    [DataRow(
        new byte[]
        {
            FrameHeader.SOF,
            3, // Length
        })]
    [DataRow(
        new byte[]
        {
            FrameHeader.SOF,
        })]
    public void ParseIncompleteDataFrame(byte[] bytes)
    {
        var sequence = new ReadOnlySequence<byte>(bytes);

        var success = FrameParser.TryParseData(NullLogger.Instance, ref sequence, out Frame frame);
        Assert.IsFalse(success);
        Assert.AreEqual(bytes.Length, sequence.Length);
        Assert.AreEqual(default, frame);
    }
}
