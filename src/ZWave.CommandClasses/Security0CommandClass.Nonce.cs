using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Security 0 nonce (8 random bytes).
/// </summary>
public readonly record struct Security0Nonce(ReadOnlyMemory<byte> Nonce)
{
    /// <summary>
    /// Gets the nonce ID, which is the first byte of the nonce.
    /// </summary>
    public byte NonceId => Nonce.Span[0];
}

public sealed partial class Security0CommandClass
{
    /// <summary>
    /// Event raised when a Nonce Report is received (solicited or unsolicited).
    /// </summary>
    public event Action<Security0Nonce>? OnNonceReportReceived;

    /// <summary>
    /// Requests a nonce from this node.
    /// </summary>
    public async Task<Security0Nonce> GetNonceAsync(CancellationToken cancellationToken)
    {
        NonceGetCommand command = NonceGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<NonceReportCommand>(cancellationToken).ConfigureAwait(false);
        Security0Nonce nonce = NonceReportCommand.Parse(reportFrame, Logger);
        OnNonceReportReceived?.Invoke(nonce);
        return nonce;
    }

    /// <summary>
    /// Nonce Get command (spec §3.5.2.1). Requests a fresh nonce; no parameters.
    /// </summary>
    internal readonly struct NonceGetCommand : ICommand
    {
        public NonceGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.NonceGet;

        public CommandClassFrame Frame { get; }

        public static NonceGetCommand Create()
            => new(CommandClassFrame.Create(CommandClassId, CommandId));
    }

    /// <summary>
    /// Nonce Report command (spec §3.5.2.2). Carries the 8-byte nonce.
    /// </summary>
    internal readonly struct NonceReportCommand : ICommand
    {
        public NonceReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.NonceReport;

        public CommandClassFrame Frame { get; }

        public static NonceReportCommand Create(ReadOnlySpan<byte> nonce)
        {
            if (nonce.Length != 8)
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, "The S0 nonce must be 8 bytes long");
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, nonce);
            return new NonceReportCommand(frame);
        }

        public static Security0Nonce Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length != 8)
            {
                logger.LogWarning("Nonce Report frame is malformed ({Length} bytes, expected 8)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Nonce Report frame is malformed");
            }

            // No copy: the frame is backed by a dedicated per-frame array (see FrameParser), so it is
            // safe to hold a slice of it for the nonce's lifetime.
            return new Security0Nonce(frame.CommandParameters);
        }
    }
}
