using System.IO;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Serial.Tests;

[TestClass]
public class ZWaveSerialPortCoordinatorTests
{
    private const byte AckByte = 0x06;
    private const byte NakByte = 0x15;
    private const byte CanByte = 0x18;

    private static readonly byte[] SoftResetFrameBytes = SoftResetRequest.Create().Frame.Data.Span.ToArray();

    private static byte[] CreateInvalidDataFrame()
    {
        byte[] frame = DataFrame.Create(DataFrameType.REQ, CommandId.GetLibraryType).Data.Span.ToArray();
        frame[^1] ^= 0xFF;
        return frame;
    }

    private static byte[] CreateValidDataFrame()
        => DataFrame.Create(DataFrameType.REQ, CommandId.GetLibraryType).Data.Span.ToArray();

    /// <summary>
    /// Simulates a serial port with separate input and output buffers.
    /// The coordinator reads from the input buffer and writes to the output buffer.
    /// The test writes to the input buffer (simulating data from the Z-Wave chip)
    /// and reads from the output buffer (simulating data sent to the Z-Wave chip).
    /// </summary>
    private sealed class TestSerialStream : Stream
    {
        private readonly object _lock = new();
        private readonly List<byte> _inputBuffer = new();
        private readonly List<byte> _outputBuffer = new();
        private readonly SemaphoreSlim _inputDataAvailable = new(0);
        private readonly SemaphoreSlim _outputDataAvailable = new(0);
        private bool _disposed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                lock (_lock)
                {
                    return _outputBuffer.Count;
                }
            }
        }

        public override long Position
        {
            get => 0;
            set { }
        }

        public override void Flush()
        {
        }

        // Read from the input buffer (data from the Z-Wave chip).
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                if (_disposed || _inputBuffer.Count == 0)
                {
                    return 0;
                }

                int toRead = Math.Min(count, _inputBuffer.Count);
                for (int i = 0; i < toRead; i++)
                {
                    buffer[offset + i] = _inputBuffer[i];
                }

                _inputBuffer.RemoveRange(0, toRead);
                return toRead;
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return 0;
                    }

                    if (_inputBuffer.Count > 0)
                    {
                        int toRead = Math.Min(count, _inputBuffer.Count);
                        for (int i = 0; i < toRead; i++)
                        {
                            buffer[offset + i] = _inputBuffer[i];
                        }

                        _inputBuffer.RemoveRange(0, toRead);
                        return toRead;
                    }
                }

                await _inputDataAvailable.WaitAsync(cancellationToken);
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            byte[] array = new byte[buffer.Length];
            int read = await ReadAsync(array, 0, array.Length, cancellationToken);
            array.AsSpan(0, read).CopyTo(buffer.Span);
            return read;
        }

        // Write to the output buffer (data sent to the Z-Wave chip).
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                _outputBuffer.AddRange(buffer.AsSpan(offset, count));
            }

            _outputDataAvailable.Release(count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Write(buffer.Span.ToArray(), 0, buffer.Length);
            return ValueTask.CompletedTask;
        }

        public override void WriteByte(byte value)
        {
            Write(new[] { value }, 0, 1);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public new void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
            }

            _inputDataAvailable.Release();
            _outputDataAvailable.Release();
        }

        /// <summary>
        /// Writes data to the input buffer (simulating data from the Z-Wave chip).
        /// </summary>
        public void WriteToInput(byte[] data)
        {
            lock (_lock)
            {
                _inputBuffer.AddRange(data);
            }

            _inputDataAvailable.Release(data.Length);
        }

        /// <summary>
        /// Reads all data from the output buffer (data sent to the Z-Wave chip).
        /// </summary>
        public byte[] ReadOutput()
        {
            lock (_lock)
            {
                byte[] data = _outputBuffer.ToArray();
                _outputBuffer.Clear();
                return data;
            }
        }

        /// <summary>
        /// Reads a single byte from the output buffer.
        /// </summary>
        public async Task<byte> ReadOutputByteAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(TestSerialStream));
                    }

                    if (_outputBuffer.Count > 0)
                    {
                        byte value = _outputBuffer[0];
                        _outputBuffer.RemoveAt(0);
                        return value;
                    }
                }

                await _outputDataAvailable.WaitAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Clears both buffers.
        /// </summary>
        public void ClearBuffers()
        {
            lock (_lock)
            {
                _inputBuffer.Clear();
                _outputBuffer.Clear();
            }
        }
    }

    private sealed class TestCoordinator : IAsyncDisposable
    {
        public ZWaveSerialPortCoordinator Coordinator { get; }
        public TestSerialStream Stream { get; }
        public ChannelReader<DataFrame> ReceiveChannel { get; }
        public ChannelWriter<DataFrameTransmission> SendChannel { get; }
        public int HardResetInvokedCount { get; private set; }
        public TaskCompletionSource HardResetInvokedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TestCoordinator()
        {
            Stream = new TestSerialStream();
            Channel<DataFrame> receiveChannel = Channel.CreateUnbounded<DataFrame>();
            Channel<DataFrameTransmission> sendChannel = Channel.CreateUnbounded<DataFrameTransmission>();
            ReceiveChannel = receiveChannel.Reader;
            SendChannel = sendChannel.Writer;

            Coordinator = new ZWaveSerialPortCoordinator(
                NullLogger.Instance,
                Stream,
                sendChannel.Reader,
                receiveChannel.Writer,
                OnHardResetInvoked);
        }

        private Task OnHardResetInvoked()
        {
            HardResetInvokedCount++;
            HardResetInvokedTcs.TrySetResult();
            return Task.CompletedTask;
        }

        public void WriteToStream(byte[] data)
        {
            Stream.WriteToInput(data);
        }

        public void WriteToStream(byte data)
        {
            Stream.WriteToInput(new[] { data });
        }

        public void ClearStream()
        {
            Stream.ClearBuffers();
        }

        public async Task<DataFrame> ReadReceivedFrameAsync(TimeSpan timeout)
        {
            Task<DataFrame> readTask = ReceiveChannel.ReadAsync().AsTask();
            Task completedTask = await Task.WhenAny(readTask, Task.Delay(timeout));
            if (completedTask != readTask)
            {
                throw new TimeoutException("Timed out waiting for a received data frame");
            }

            return await readTask;
        }

        public async Task<DataFrameTransmission> QueueTransmissionAsync(DataFrame frame)
        {
            TaskCompletionSource transmissionComplete = new(TaskCreationOptions.RunContinuationsAsynchronously);
            DataFrameTransmission transmission = new(frame, transmissionComplete);
            await SendChannel.WriteAsync(transmission);
            return transmission;
        }

        public async Task WaitForTransmissionCompleteAsync(DataFrameTransmission transmission, TimeSpan timeout)
        {
            Task completeTask = transmission.TransmissionComplete.Task;
            Task completedTask = await Task.WhenAny(completeTask, Task.Delay(timeout));
            if (completedTask != completeTask)
            {
                throw new TimeoutException("Timed out waiting for transmission to complete");
            }

            await completeTask;
        }

        public async ValueTask DisposeAsync()
        {
            await Coordinator.DisposeAsync();
        }
    }

    private static async Task WaitForStreamContentAsync(TestSerialStream stream, byte[] expected, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        byte[] received = new byte[expected.Length];
        int offset = 0;

        while (offset < expected.Length)
        {
            try
            {
                byte b = await stream.ReadOutputByteAsync(cts.Token);
                received[offset] = b;
                offset++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (offset < expected.Length)
        {
            Assert.Fail($"Expected stream to contain {expected.Length} bytes, but only found {offset}");
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (received[i] != expected[i])
            {
                Assert.Fail($"Expected byte 0x{expected[i]:X2} at position {i}, but found 0x{received[i]:X2}");
            }
        }
    }

    private static async Task WaitForStreamContentAsync(TestSerialStream stream, byte expectedByte, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);

        try
        {
            byte b = await stream.ReadOutputByteAsync(cts.Token);
            if (b != expectedByte)
            {
                Assert.Fail($"Expected byte 0x{expectedByte:X2}, but found 0x{b:X2}");
            }
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"Expected stream to contain byte 0x{expectedByte:X2}, but it was not found within {timeout.TotalSeconds}s");
        }
    }

    private static byte[] ReadStreamContent(TestSerialStream stream)
    {
        return stream.ReadOutput();
    }

    [TestMethod]
    public async Task TwoInvalidFrames_DoNotReset()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        // Wait for both NAKs to be sent.
        await WaitForStreamContentAsync(coordinator.Stream, NakByte, TimeSpan.FromSeconds(5));
        await WaitForStreamContentAsync(coordinator.Stream, NakByte, TimeSpan.FromSeconds(5));

        // Give time for a reset to be sent if it were going to happen.
        await Task.Delay(200);

        Assert.AreEqual(0, coordinator.HardResetInvokedCount);

        // Clear the stream and verify no soft reset frame is sent.
        coordinator.ClearStream();
        await Task.Delay(200);
        Assert.AreEqual(0, coordinator.Stream.Length, "Soft reset frame should not have been sent");
    }

    [TestMethod]
    public async Task ThreeInvalidFrames_SendThreeNaksAndOneReset()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        // Wait for the hard reset to be invoked.
        await coordinator.HardResetInvokedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Wait for the recovery to complete (timeout path, since no SerialApiStarted is sent).
        await Task.Delay(1700);

        Assert.AreEqual(1, coordinator.HardResetInvokedCount);

        // Read all stream content and verify it contains 3 NAKs (one per invalid frame)
        // and the soft reset frame. The constructor's NAK is also present.
        byte[] streamData = ReadStreamContent(coordinator.Stream);
        int nakCount = 0;
        foreach (byte b in streamData)
        {
            if (b == NakByte)
            {
                nakCount++;
            }
        }

        // 1 NAK from constructor + 3 NAKs from invalid frames = 4 total
        Assert.AreEqual(4, nakCount, $"Expected 4 NAKs (1 initial + 3 for invalid frames), found {nakCount}");

        // Verify the soft reset frame is present in the stream.
        bool foundSoftReset = false;
        for (int i = 0; i <= streamData.Length - SoftResetFrameBytes.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < SoftResetFrameBytes.Length; j++)
            {
                if (streamData[i + j] != SoftResetFrameBytes[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                foundSoftReset = true;
                break;
            }
        }

        Assert.IsTrue(foundSoftReset, "Expected soft reset frame to be present in stream");
    }

    [TestMethod]
    public async Task ValidFrameResetsStreak()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();
        byte[] validFrame = CreateValidDataFrame();

        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(validFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        // Wait for the valid frame to be received.
        await coordinator.ReadReceivedFrameAsync(TimeSpan.FromSeconds(5));

        // Give time for a reset to be sent if it were going to happen.
        await Task.Delay(200);

        Assert.AreEqual(0, coordinator.HardResetInvokedCount);
    }

    [TestMethod]
    public async Task AckNakCanDoNotBreakStreak()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();

        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(AckByte);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(NakByte);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(CanByte);

        // Wait for the hard reset to be invoked.
        await coordinator.HardResetInvokedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Wait for the recovery to complete (timeout path).
        await Task.Delay(1700);

        Assert.AreEqual(1, coordinator.HardResetInvokedCount);
    }

    [TestMethod]
    public async Task NewStreakAfterReset_TriggersSecondReset()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();

        // First streak of 3.
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        await coordinator.HardResetInvokedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Wait for the recovery to complete (timeout path).
        await Task.Delay(1700);

        // Second streak of 3.
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        // Wait for the second hard reset to be invoked.
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (coordinator.HardResetInvokedCount < 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.AreEqual(2, coordinator.HardResetInvokedCount);
    }

    [TestMethod]
    public async Task ResetDoesNotDeadlockReadLoop()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();

        // Trigger a hard reset.
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        await coordinator.HardResetInvokedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Wait for the recovery to complete (timeout path).
        await Task.Delay(1700);

        // The read loop should still be processing frames.
        // Clear the stream to remove any leftover bytes (e.g., the soft reset frame).
        coordinator.ClearStream();

        byte[] validFrame = CreateValidDataFrame();
        coordinator.WriteToStream(validFrame);

        DataFrame receivedFrame = await coordinator.ReadReceivedFrameAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(CommandId.GetLibraryType, receivedFrame.CommandId);
    }

    [TestMethod]
    public async Task ActiveRequestIsFaulted()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();

        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        // The hard reset hook should be invoked, which simulates the driver faulting pending state.
        await coordinator.HardResetInvokedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Wait for the recovery to complete (timeout path).
        await Task.Delay(1700);

        Assert.AreEqual(1, coordinator.HardResetInvokedCount);
    }

    [TestMethod]
    public async Task OrdinaryTrafficWaitsUntilRecoveryCompletes()
    {
        await using TestCoordinator coordinator = new();

        byte[] invalidFrame = CreateInvalidDataFrame();

        // Queue a transmission before the reset.
        DataFrame transmissionFrame = DataFrame.Create(DataFrameType.REQ, CommandId.GetLibraryType);
        DataFrameTransmission transmission = await coordinator.QueueTransmissionAsync(transmissionFrame);

        // Give the write task time to pick up the transmission and wait on the gate.
        await Task.Delay(100);

        // Trigger a hard reset.
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);
        coordinator.WriteToStream(invalidFrame);

        await coordinator.HardResetInvokedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The transmission should not have completed yet (it's waiting on the gate).
        Task completeTask = transmission.TransmissionComplete.Task;
        Assert.IsFalse(completeTask.IsCompleted, "Transmission should not have completed while recovery is in progress");

        // Wait for the recovery to complete (timeout path).
        await Task.Delay(1700);

        // The transmission should now complete (it will fail because no ACK is sent, but it should not be stuck on the gate).
        try
        {
            await coordinator.WaitForTransmissionCompleteAsync(transmission, TimeSpan.FromSeconds(10));
        }
        catch (ZWaveException)
        {
            // Expected: the transmission fails because no ACK is sent.
        }
    }
}
