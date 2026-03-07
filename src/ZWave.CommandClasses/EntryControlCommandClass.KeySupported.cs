using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class EntryControlCommandClass
{
    /// <summary>
    /// Gets the supported ASCII keys reported by the device.
    /// </summary>
    public IReadOnlySet<char>? SupportedKeys { get; private set; }

    /// <summary>
    /// Request the supported keys for credential entry from the device.
    /// </summary>
    public async Task<IReadOnlySet<char>> GetSupportedKeysAsync(CancellationToken cancellationToken)
    {
        EntryControlKeySupportedGetCommand command = EntryControlKeySupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<EntryControlKeySupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<char> supportedKeys = EntryControlKeySupportedReportCommand.Parse(reportFrame, Logger);
        SupportedKeys = supportedKeys;
        return supportedKeys;
    }

    internal readonly struct EntryControlKeySupportedGetCommand : ICommand
    {
        public EntryControlKeySupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.KeySupportedGet;

        public CommandClassFrame Frame { get; }

        public static EntryControlKeySupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new EntryControlKeySupportedGetCommand(frame);
        }
    }

    internal readonly struct EntryControlKeySupportedReportCommand : ICommand
    {
        public EntryControlKeySupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.KeySupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<char> Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: BitMaskLength(1)
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Entry Control Key Supported Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Key Supported Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte bitMaskLength = span[0];

            if (frame.CommandParameters.Length < 1 + bitMaskLength)
            {
                logger.LogWarning(
                    "Entry Control Key Supported Report frame too short for declared bitmask ({Length} bytes, need {Expected})",
                    frame.CommandParameters.Length,
                    1 + bitMaskLength);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Key Supported Report frame is too short for bitmask");
            }

            HashSet<char> supportedKeys = [];
            ReadOnlySpan<byte> bitMask = span.Slice(1, bitMaskLength);
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        char asciiChar = (char)((byteNum << 3) + bitNum);
                        supportedKeys.Add(asciiChar);
                    }
                }
            }

            return supportedKeys;
        }
    }
}
