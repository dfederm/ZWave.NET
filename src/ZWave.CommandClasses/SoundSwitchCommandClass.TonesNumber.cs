using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class SoundSwitchCommandClass
{
    /// <summary>
    /// Gets the number of tones supported by the device.
    /// </summary>
    public byte? SupportedTonesCount { get; private set; }

    /// <summary>
    /// Request the number of tones supported by the device.
    /// </summary>
    public async Task<byte> GetTonesNumberAsync(CancellationToken cancellationToken)
    {
        SoundSwitchTonesNumberGetCommand command = SoundSwitchTonesNumberGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<SoundSwitchTonesNumberReportCommand>(cancellationToken).ConfigureAwait(false);
        byte tonesCount = SoundSwitchTonesNumberReportCommand.Parse(reportFrame, Logger);
        SupportedTonesCount = tonesCount;
        return tonesCount;
    }

    internal readonly struct SoundSwitchTonesNumberGetCommand : ICommand
    {
        public SoundSwitchTonesNumberGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonesNumberGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchTonesNumberGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new SoundSwitchTonesNumberGetCommand(frame);
        }
    }

    internal readonly struct SoundSwitchTonesNumberReportCommand : ICommand
    {
        public SoundSwitchTonesNumberReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonesNumberReport;

        public CommandClassFrame Frame { get; }

        public static byte Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Sound Switch Tones Number Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Sound Switch Tones Number Report frame is too short");
            }

            return frame.CommandParameters.Span[0];
        }
    }
}
