using System.Buffers;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ZWave.Serial.Commands;

namespace ZWave.Serial;

/// <summary>
/// Represents a data frame transmission request with its completion source.
/// </summary>
public record struct DataFrameTransmission(DataFrame Frame, TaskCompletionSource TransmissionComplete);

/// <summary>
/// Coordinates communication with the Z-Wave Serial port
/// </summary>
public sealed class ZWaveSerialPortCoordinator : IAsyncDisposable
{
    // INS12350 6.3 specifies that the host should use 3 retransmissions, meaning 4 total attempts
    private const int MaxTransmissionAttempts = 4;

    // INS12350 6.2.2
    private static readonly TimeSpan FrameDeliveryTimeout = TimeSpan.FromMilliseconds(1600);

    // INS12350 6.4.1 defines a Z-Wave module as unresponsive after 4 seconds, so retry 4 times with a 1 second delay between each.
    private const int MaxConnectionAttempts = 4;
    private const int ConnectionDelay = 1000;

    // INS12350 6.4.2: A hard reset SHOULD be invoked after three consecutive invalid checksums.
    private const int MaxConsecutiveInvalidChecksums = 3;

    // Lock to manage a the current unsolicited or request/response frame flow. If one flow is in progress, a new one may not start.
    private readonly SemaphoreSlim _commLock = new (1, 1);

    private readonly ILogger _logger;

    private readonly SerialPort? _serialPort;

    private readonly Stream _stream;

    private readonly ChannelReader<DataFrameTransmission> _dataFrameSendChannelReader;

    private readonly ChannelWriter<DataFrame> _dataFrameReceiveChannelWriter;

    // Called when a hard reset is invoked to fault the driver's pending request/response and callback state.
    private readonly Func<Task> _onHardResetInvoked;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly Task _readTask;

    private readonly Task _writeTask;

    private TaskCompletionSource<bool>? _frameDeliveryResultTaskSource;

    // Tracks whether a CAN was received during the current frame delivery attempt.
    // Per spec 3.4.3, on CAN the chip drops our frame but has its own data frame to send.
    // We defer signaling delivery failure until the chip's data frame is processed, giving
    // the chip retransmission priority. The 1600ms ACK timeout acts as a fallback.
    private bool _pendingCanDeliveryFailure;

    // INS12350 6.4.2: Tracks consecutive data frames received with an invalid checksum.
    // Only the read task touches this field.
    private int _consecutiveInvalidChecksumCount;

    // Set while a hard reset is in progress. The write task awaits this gate before processing
    // new transmissions, so ordinary traffic doesn't run while the controller is restarting.
    private TaskCompletionSource _recoveryGate = new TaskCompletionSource();

    // Set while waiting for SerialApiStarted after a hard reset. Only the read task touches this field.
    private TaskCompletionSource<SerialApiStartedRequest>? _serialApiStartedTcs;

    public ZWaveSerialPortCoordinator(
        ILogger logger,
        string portName,
        ChannelReader<DataFrameTransmission> dataFrameSendChannelReader,
        ChannelWriter<DataFrame> dataFrameReceiveChannelWriter,
        Func<Task> onHardResetInvoked)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dataFrameSendChannelReader);
        ArgumentNullException.ThrowIfNull(dataFrameReceiveChannelWriter);
        ArgumentNullException.ThrowIfNull(onHardResetInvoked);

        _logger = logger;
        _serialPort = CreateSerialPort(portName);
        _stream = _serialPort.BaseStream;
        _dataFrameSendChannelReader = dataFrameSendChannelReader;
        _dataFrameReceiveChannelWriter = dataFrameReceiveChannelWriter;
        _onHardResetInvoked = onHardResetInvoked;

        _serialPort.Open();
        _logger.LogSerialApiPortOpened(_serialPort.PortName);

        _cancellationTokenSource = new CancellationTokenSource();

        // Note: Since we're starting our own tasks, we don't need to ConfigureAwait anywhere downstream.
        _readTask = Task.Run(ReadAsync);
        _writeTask = Task.Run(WriteAsync);

        // Send a NAK as part of the initialization sequence (INS12350 6.1)
        SendFrame(Frame.NAK);
    }

    // Test-only constructor that uses an in-memory stream instead of a real serial port.
    internal ZWaveSerialPortCoordinator(
        ILogger logger,
        Stream stream,
        ChannelReader<DataFrameTransmission> dataFrameSendChannelReader,
        ChannelWriter<DataFrame> dataFrameReceiveChannelWriter,
        Func<Task> onHardResetInvoked)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(dataFrameSendChannelReader);
        ArgumentNullException.ThrowIfNull(dataFrameReceiveChannelWriter);
        ArgumentNullException.ThrowIfNull(onHardResetInvoked);

        _logger = logger;
        _serialPort = null;
        _stream = stream;
        _dataFrameSendChannelReader = dataFrameSendChannelReader;
        _dataFrameReceiveChannelWriter = dataFrameReceiveChannelWriter;
        _onHardResetInvoked = onHardResetInvoked;

        _cancellationTokenSource = new CancellationTokenSource();

        // Note: Since we're starting our own tasks, we don't need to ConfigureAwait anywhere downstream.
        _readTask = Task.Run(ReadAsync);
        _writeTask = Task.Run(WriteAsync);

        // Send a NAK as part of the initialization sequence (INS12350 6.1)
        SendFrame(Frame.NAK);
    }

    private static SerialPort CreateSerialPort(string portName)
    {
        ArgumentException.ThrowIfNullOrEmpty(portName);

        // INS12350 4.2.1 defines the serial port settings
        var serialPort = new SerialPort(
            portName,
            baudRate: 115200,
            parity: Parity.None,
            dataBits: 8,
            stopBits: StopBits.One);

        // Avoid throwing TimeoutExceptions.
        serialPort.ReadTimeout = SerialPort.InfiniteTimeout;
        serialPort.WriteTimeout = SerialPort.InfiniteTimeout;

        return serialPort;
    }

    public async ValueTask DisposeAsync()
    {
        _dataFrameReceiveChannelWriter.Complete();
        _cancellationTokenSource.Cancel();

        try
        {
            await _readTask;
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation.
        }

        try
        {
            await _writeTask;
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation.
        }

        if (_serialPort != null)
        {
            _serialPort.Close();
            _logger.LogSerialApiPortClosed(_serialPort.PortName);
        }
        else
        {
            _stream.Dispose();
        }
    }

    private async Task ReadAsync()
    {
        CancellationToken cancellationToken = _cancellationTokenSource.Token;

        PipeReader serialPortReader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // This won't return until there is available data, so the actual read is outside the lock.
                // There is a race condition where we write a frame before we're notified of this read, however
                // the Z-Wave protocol will handle this conflict (by the chip sending us a CAN).
                ReadResult readResult = await serialPortReader.ReadAsync(cancellationToken);

                // While processing frames, lock to ensure no frames are written.
                await _commLock.WaitAsync();
                try
                {
                    ReadOnlySequence<byte> buffer = readResult.Buffer;

                    if (readResult.IsCanceled)
                    {
                        break;
                    }

                    while (FrameParser.TryParseData(_logger, ref buffer, out Frame frame))
                    {
                        switch (frame.Type)
                        {
                            case FrameType.ACK:
                            case FrameType.NAK:
                            {
                                _logger.LogSerialApiFrameReceived(frame);

                                if (_frameDeliveryResultTaskSource != null)
                                {
                                    _frameDeliveryResultTaskSource.SetResult(frame.Type == FrameType.ACK);
                                }
                                else
                                {
                                    // We received a frame delivery notification unexpectedly. Just ignore.
                                    _logger.LogSerialApiUnexpectedFrame(frame);
                                }

                                break;
                            }
                            case FrameType.CAN:
                            {
                                _logger.LogSerialApiFrameReceived(frame);

                                if (_frameDeliveryResultTaskSource != null)
                                {
                                    // Spec 3.4.3 (Figure 3.9): CAN means the chip detected a collision
                                    // and dropped our frame. The chip has priority to retransmit its own
                                    // data frame. Defer signaling delivery failure until we process and
                                    // ACK the chip's data frame, then the host can retransmit.
                                    _pendingCanDeliveryFailure = true;
                                    _logger.LogSerialApiCanDuringFrameDelivery();
                                }
                                else
                                {
                                    // We received a frame delivery notification unexpectedly. Just ignore.
                                    _logger.LogSerialApiUnexpectedFrame(frame);
                                }

                                break;
                            }
                            case FrameType.Data:
                            {
                                DataFrame dataFrame = frame.ToDataFrame();

                                if (dataFrame.IsChecksumValid())
                                {
                                    _consecutiveInvalidChecksumCount = 0;
                                    _logger.LogSerialApiDataFrameReceived(dataFrame);

                                    // Acknowledge any valid request immediately.
                                    SendFrame(Frame.ACK);

                                    // If we're waiting for SerialApiStarted after a hard reset, intercept it.
                                    if (_serialApiStartedTcs != null && dataFrame.CommandId == CommandId.SerialApiStarted)
                                    {
                                        _serialApiStartedTcs.SetResult(SerialApiStartedRequest.Create(dataFrame, new CommandParsingContext(NodeIdType.Short)));
                                        _serialApiStartedTcs = null;
                                    }

                                    await _dataFrameReceiveChannelWriter.WriteAsync(dataFrame, cancellationToken);

                                    // Spec 3.4.3 (Figure 3.9): After a CAN, the chip retransmits its
                                    // data frame. Now that we've processed and ACKed it, signal delivery
                                    // failure so the host can retransmit with backoff.
                                    if (_pendingCanDeliveryFailure && _frameDeliveryResultTaskSource != null)
                                    {
                                        _frameDeliveryResultTaskSource.SetResult(false);
                                        _pendingCanDeliveryFailure = false;
                                    }
                                }
                                else
                                {
                                    _logger.LogSerialApiInvalidDataFrameReceived(dataFrame);

                                    // INS12350 5.4.6:
                                    //   Data frame MUST be considered invalid if it is received with an invalid checksum.
                                    //   A host or Z-Wave chip MUST return a NAK frame in response to an invalid Data frame.
                                    SendFrame(Frame.NAK);

                                    // INS12350 6.4.2:
                                    //   If a host application detects an invalid checksum three times in a row when receiving data frames, the
                                    //   host application SHOULD invoke a hard reset of the device. If a hard reset line is not available, a soft
                                    //   reset indication SHOULD be issued for the device.
                                    _consecutiveInvalidChecksumCount++;
                                    if (_consecutiveInvalidChecksumCount >= MaxConsecutiveInvalidChecksums)
                                    {
                                        _consecutiveInvalidChecksumCount = 0;
                                        await InvokeHardResetAsync(cancellationToken);
                                    }
                                }

                                break;
                            }
                            default:
                            {
                                // Ignore anything we don't recognize.
                                _logger.LogSerialApiFrameUnknownType(frame.Type);
                                break;
                            }
                        }
                    }

                    // Tell the PipeReader how much of the buffer has been consumed.
                    serialPortReader.AdvanceTo(buffer.Start, buffer.End);
                }
                finally
                {
                    _commLock.Release();
                }

                // Stop reading if there's no more data coming.
                if (readResult.IsCompleted)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                // Swallow. If a specific read is cancelled, just keep retrying.
                _logger.LogSerialApiReadCancellation();
            }
            catch (Exception ex)
            {
                _logger.LogSerialApiReadException(ex);

                await _commLock.WaitAsync();
                try
                {
                    EnsurePortOpened();
                }
                finally
                {
                    _commLock.Release();
                }

                // When re-opening the port the stream gets recreated too, so we need to re-create the reader
                serialPortReader.CancelPendingRead();
                serialPortReader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
            }
        }
    }

    private async Task WriteAsync()
    {
        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        await foreach (DataFrameTransmission transmission in _dataFrameSendChannelReader.ReadAllAsync(cancellationToken))
        {
            // Wait for any in-progress hard reset recovery to complete before processing new traffic.
            await _recoveryGate.Task;

            bool transmissionSuccess = false;
            for (int transmissionAttempt = 0; transmissionAttempt < MaxTransmissionAttempts; transmissionAttempt++)
            {
                // INS12350 6.3 specifies a wait time for retransmissions
                if (transmissionAttempt > 0)
                {
                    int waitTimeMillis = 100 + ((transmissionAttempt - 1) * 1000);
                    await Task.Delay(waitTimeMillis, cancellationToken);
                }

                _pendingCanDeliveryFailure = false;
                _frameDeliveryResultTaskSource = new TaskCompletionSource<bool>();

                // While writing frames, lock to ensure no frames are read and processed.
                await _commLock.WaitAsync();
                try
                {
                    // Send the command
                    await _stream.WriteAsync(transmission.Frame.Data, cancellationToken);
                    _logger.LogSerialApiDataFrameSent(transmission.Frame);
                }
                catch (Exception ex)
                {
                    _logger.LogSerialApiWriteException(ex);
                    EnsurePortOpened();
                    continue;
                }
                finally
                {
                    _commLock.Release();
                }

                // Wait for delivery confirmation. This cannot be in the lock as the reading thread is separate from this one, and
                // we cannot just directly read from the stream here as the other thread may get the read first, as it's also not inside
                // the lock so we cannot ensure it does not consume those bytes.
                bool frameDeliveryResult;
                try
                {
                    frameDeliveryResult = await _frameDeliveryResultTaskSource.Task.WaitAsync(FrameDeliveryTimeout, cancellationToken);
                }
                catch (TimeoutException)
                {
                    // INS12350 6.2.2 specifies that timeouts are treated as a NAK, which is not a success.
                    _logger.LogSerialApiFrameDeliveryAckTimeout();
                    frameDeliveryResult = false;
                }
                finally
                {
                    _frameDeliveryResultTaskSource = null;
                }

                if (frameDeliveryResult)
                {
                    // A data frame went through successfully.
                    transmissionSuccess = true;
                    break;
                }

                // In the case of a NAK or timeout, retransmit our data frame.
                _logger.LogSerialApiFrameTransmissionRetry(transmissionAttempt + 1);
            }

            if (transmissionSuccess)
            {
                transmission.TransmissionComplete.SetResult();
            }
            else
            {
                // INS12350 6.3: Flush/reopen the serial port after the three retransmissions.
                await _commLock.WaitAsync();
                try
                {
                    if (_serialPort != null)
                    {
                        _serialPort.DiscardInBuffer();
                        _serialPort.DiscardOutBuffer();
                        _serialPort.Close();
                        EnsurePortOpened();
                    }
                }
                finally
                {
                    _commLock.Release();
                }

                transmission.TransmissionComplete.SetException(ZWaveException.Create(ZWaveErrorCode.CommandSendFailed, "Command failed to send"));
            }
        }
    }

    /// <summary>
    /// Invokes a hard reset of the Z-Wave module per INS12350 6.4.2.
    /// Since a hard reset line is not available, a soft reset indication is issued instead.
    /// Gates the write task until the module signals SerialApiStarted (or a fallback timeout elapses),
    /// so ordinary traffic doesn't run while the controller is restarting.
    /// </summary>
    private async Task InvokeHardResetAsync(CancellationToken cancellationToken)
    {
        _logger.LogSerialApiHardResetInvoked(MaxConsecutiveInvalidChecksums);

        // Gate the write task so ordinary traffic waits until recovery completes.
        _recoveryGate = new TaskCompletionSource();

        // Fault the driver's pending request/response and callback state. The third invalid frame
        // may have been a corrupted response, so any in-flight SendCommandAsync would otherwise wait
        // permanently.
        await _onHardResetInvoked();

        // The chip is in an unknown state, so discard any pending data before and after the reset.
        if (_serialPort != null)
        {
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
        }

        // Send the soft reset directly, bypassing the write task (which is gated).
        var softResetRequest = SoftResetRequest.Create();
        SendFrame(new Frame(softResetRequest.Frame.Data));

        // Wait for the module to signal SerialApiStarted, or fall back to a timeout.
        // The chip may not respond if it is in a bad state, so the timeout is the safety net.
        _serialApiStartedTcs = new TaskCompletionSource<SerialApiStartedRequest>();
        TimeSpan serialApiStartedWaitTime = TimeSpan.FromMilliseconds(1500);

        try
        {
            await _serialApiStartedTcs.Task.WaitAsync(serialApiStartedWaitTime, cancellationToken);
        }
        catch (TimeoutException)
        {
            // If we don't get the signal, assume the soft reset was successful after the wait time.
        }
        finally
        {
            _serialApiStartedTcs = null;
        }

        // Unblock the write task so ordinary traffic can resume.
        _recoveryGate.SetResult();
    }

    private void EnsurePortOpened()
    {
        if (_commLock.CurrentCount != 0)
        {
            throw new InvalidOperationException("The lock must be held before calling this method");
        }

        if (_serialPort == null)
        {
            // No real serial port in test mode.
            return;
        }

        if (!_serialPort.IsOpen)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    _serialPort.Open();
                    _logger.LogSerialApiPortReopened(_serialPort.PortName);
                    break;
                }
                catch (FileNotFoundException)
                {
                    // If the port goes away momentarily, for example during a soft reset, retry opening the port a few times
                    if (retryCount <= MaxConnectionAttempts)
                    {
                        retryCount++;
                        Thread.Sleep(ConnectionDelay);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }

    private void SendFrame(Frame frame)
    {
        _stream.Write(frame.Data.Span);
        _logger.LogSerialApiFrameSent(frame);
    }
}
