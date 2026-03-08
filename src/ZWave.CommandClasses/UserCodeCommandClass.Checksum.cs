using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class UserCodeCommandClass
{
    /// <summary>
    /// Gets the user code checksum from the device.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The CRC-CCITT checksum representing all user codes, or 0x0000 if no codes are set.</returns>
    public async Task<ushort> GetChecksumAsync(CancellationToken cancellationToken)
    {
        var command = ChecksumGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ChecksumReportCommand>(cancellationToken).ConfigureAwait(false);
        ushort checksum = ChecksumReportCommand.Parse(reportFrame, Logger);
        return checksum;
    }

    internal readonly struct ChecksumGetCommand : ICommand
    {
        public ChecksumGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.ChecksumGet;

        public CommandClassFrame Frame { get; }

        public static ChecksumGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ChecksumGetCommand(frame);
        }
    }

    internal readonly struct ChecksumReportCommand : ICommand
    {
        public ChecksumReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.ChecksumReport;

        public CommandClassFrame Frame { get; }

        public static ushort Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning(
                    "User Code Checksum Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Checksum Report frame is too short");
            }

            return frame.CommandParameters.Span[0..2].ToUInt16BE();
        }
    }
}
