using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the current tone play state of a Sound Switch device.
/// </summary>
public readonly record struct SoundSwitchTonePlayReport(
    /// <summary>
    /// The tone identifier currently being played, or 0 if no tone is playing.
    /// </summary>
    byte ToneIdentifier,

    /// <summary>
    /// The actual playing volume (0-100%). Added in version 2.
    /// This field is <see langword="null"/> if the sending node uses version 1.
    /// </summary>
    byte? Volume);

public sealed partial class SoundSwitchCommandClass
{
    /// <summary>
    /// Gets the last tone play report received from the device.
    /// </summary>
    public SoundSwitchTonePlayReport? LastTonePlayReport { get; private set; }

    /// <summary>
    /// Event raised when a Tone Play Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<SoundSwitchTonePlayReport>? OnTonePlayReportReceived;

    /// <summary>
    /// Request the current tone being played by the device.
    /// </summary>
    public async Task<SoundSwitchTonePlayReport> GetTonePlayAsync(CancellationToken cancellationToken)
    {
        SoundSwitchTonePlayGetCommand command = SoundSwitchTonePlayGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<SoundSwitchTonePlayReportCommand>(cancellationToken).ConfigureAwait(false);
        SoundSwitchTonePlayReport report = SoundSwitchTonePlayReportCommand.Parse(reportFrame, Logger);
        LastTonePlayReport = report;
        OnTonePlayReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Instruct the device to play or stop playing a tone.
    /// </summary>
    /// <param name="toneIdentifier">
    /// The tone to play: 0x00 = stop playing, 0x01-0xFE = play specified tone (unsupported values play default tone),
    /// 0xFF = play default tone.
    /// </param>
    /// <param name="volume">
    /// The volume for this play command (version 2 only): 0x00 = use configured volume, 1-100 = volume percentage,
    /// 0xFF = use most recent non-zero volume if muted, otherwise use configured volume.
    /// Pass <see langword="null"/> to omit (version 1 behavior).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task PlayAsync(byte toneIdentifier, byte? volume, CancellationToken cancellationToken)
    {
        var command = SoundSwitchTonePlaySetCommand.Create(EffectiveVersion, toneIdentifier, volume);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Instruct the device to stop playing the current tone.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var command = SoundSwitchTonePlaySetCommand.Create(EffectiveVersion, 0x00, null);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct SoundSwitchTonePlaySetCommand : ICommand
    {
        public SoundSwitchTonePlaySetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonePlaySet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchTonePlaySetCommand Create(byte version, byte toneIdentifier, byte? volume)
        {
            bool includeVolume = version >= 2;
            Span<byte> commandParameters = stackalloc byte[1 + (includeVolume ? 1 : 0)];
            commandParameters[0] = toneIdentifier;

            if (includeVolume)
            {
                // Per spec CC:0079.02.08.11.007: volume MUST be 0x00 if tone identifier is 0x00
                commandParameters[1] = toneIdentifier == 0x00
                    ? (byte)0x00
                    : volume.GetValueOrDefault(0x00);
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SoundSwitchTonePlaySetCommand(frame);
        }
    }

    internal readonly struct SoundSwitchTonePlayGetCommand : ICommand
    {
        public SoundSwitchTonePlayGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonePlayGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchTonePlayGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new SoundSwitchTonePlayGetCommand(frame);
        }
    }

    internal readonly struct SoundSwitchTonePlayReportCommand : ICommand
    {
        public SoundSwitchTonePlayReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.TonePlayReport;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchTonePlayReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Sound Switch Tone Play Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Sound Switch Tone Play Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            byte toneIdentifier = span[0];

            // V2 adds Play Command Tone Volume field - check payload length, not version
            byte? volume = span.Length > 1
                ? span[1]
                : null;

            return new SoundSwitchTonePlayReport(toneIdentifier, volume);
        }
    }
}
